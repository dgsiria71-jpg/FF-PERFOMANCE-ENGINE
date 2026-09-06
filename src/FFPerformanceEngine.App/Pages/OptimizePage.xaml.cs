using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class OptimizePage : UserControl
{
    private AutoTunerMode _mode;
    private GameKind _game;
    private CancellationTokenSource? _tuningCts;
    private bool _isRunning;
    private bool _initializing = true;

    public OptimizePage()
    {
        InitializeComponent();

        _mode = App.Services.Settings.KeepDeepAsDefault
            ? AutoTunerMode.Deep
            : App.Services.Settings.DefaultTunerMode;
        _game = App.Services.Settings.PreferredGame is GameKind.FreeFire or GameKind.FreeFireMax
            ? App.Services.Settings.PreferredGame
            : GameKind.FreeFireMax;

        KeepDeepCheck.IsChecked = App.Services.Settings.KeepDeepAsDefault;
        Loaded += OptimizePage_Loaded;
        ApplyChoiceVisuals();
        _initializing = false;
    }

    private void OptimizePage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadEnvironmentSelection();
        RefreshReadiness();
    }

    private void LoadEnvironmentSelection()
    {
        _initializing = true;
        try
        {
            var environment = App.Services.Environment.Capture();
            if (App.Services.Settings.PreferredGame == GameKind.None &&
                environment.ActiveGame is GameKind.FreeFire or GameKind.FreeFireMax)
            {
                _game = environment.ActiveGame;
            }

            var previousInstance = InstanceCombo.SelectedItem as string;
            var names = environment.Instances.Select(instance => instance.Name).ToList();
            InstanceCombo.ItemsSource = names;

            if (!string.IsNullOrWhiteSpace(previousInstance) && names.Contains(previousInstance, StringComparer.OrdinalIgnoreCase))
                InstanceCombo.SelectedItem = names.First(name => string.Equals(name, previousInstance, StringComparison.OrdinalIgnoreCase));
            else if (names.Count > 0)
                InstanceCombo.SelectedIndex = 0;

            ApplyChoiceVisuals();
        }
        finally
        {
            _initializing = false;
        }
    }

    private void AdaptiveMode_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _mode = AutoTunerMode.Adaptive;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void DeepMode_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _mode = AutoTunerMode.Deep;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFire_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _game = GameKind.FreeFire;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFireMax_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;
        _game = GameKind.FreeFireMax;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || _isRunning) return;
        RefreshReadiness();
    }

    private async void KeepDeepCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing || _isRunning) return;

        var keepDeep = KeepDeepCheck.IsChecked == true;
        if (keepDeep)
            _mode = AutoTunerMode.Deep;

        ApplyChoiceVisuals();
        RefreshReadiness();

        var updated = App.Services.Settings with
        {
            KeepDeepAsDefault = keepDeep,
            DefaultTunerMode = keepDeep ? AutoTunerMode.Deep : _mode
        };

        try
        {
            await App.Services.SaveSettingsAsync(updated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReadinessStatusText.Text = "Não foi possível salvar a preferência";
            ReadinessDetailText.Text = ex.Message;
        }
    }

    private void RefreshReadiness()
    {
        if (_initializing || _isRunning) return;

        var selectedInstance = InstanceCombo.SelectedItem as string;
        OptimizeReadiness readiness;
        try
        {
            readiness = App.Services.OptimizeWorkflow.Analyze(_game, _mode, selectedInstance);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReadinessStatusText.Text = "Não foi possível analisar o ambiente";
            ReadinessDetailText.Text = ex.Message;
            InstanceStatusText.Text = "Instância: —";
            TelemetryStatusText.Text = "PresentMon: —";
            CandidateCountText.Text = "0 candidatos";
            StartButton.IsEnabled = false;
            return;
        }

        ReadinessStatusText.Text = readiness.CanStart ? "Pronto para otimizar" : "Atenção necessária";
        ReadinessDetailText.Text = readiness.Message;
        InstanceStatusText.Text = readiness.Instance is null
            ? "Instância: —"
            : $"Instância: {readiness.Instance.Name}";
        TelemetryStatusText.Text = App.Services.PresentMon.FindExecutable() is null
            ? "PresentMon: ausente"
            : "PresentMon: pronto";
        CandidateCountText.Text = $"{readiness.Candidates.Count} candidato(s)";
        StartButton.IsEnabled = readiness.CanStart;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_isRunning) return;

        var selectedInstance = InstanceCombo.SelectedItem as string;
        var readiness = App.Services.OptimizeWorkflow.Analyze(_game, _mode, selectedInstance);
        if (!readiness.CanStart || readiness.Instance is null)
        {
            ReadinessStatusText.Text = "Atenção necessária";
            ReadinessDetailText.Text = readiness.Message;
            StartButton.IsEnabled = false;
            return;
        }

        _tuningCts = new CancellationTokenSource();
        _isRunning = true;
        SetRunningState(true);
        ResultCard.Visibility = Visibility.Collapsed;
        RunCard.Visibility = Visibility.Visible;
        ApplyProgress(new AutoTunerProgressPresentation(0, "Preparando", "Otimizando seu sistema", "Criando snapshot e preparando a primeira medição segura."));

        try
        {
            var result = await App.Services.OptimizeWorkflow.RunAsync(
                _game,
                _mode,
                readiness.Instance.Name,
                visual => Dispatcher.BeginInvoke(new Action(() => ApplyProgress(visual))),
                _tuningCts.Token);

            ShowResult(result);
        }
        catch (OperationCanceledException)
        {
            StageLabelText.Text = "CANCELADO";
            RunTitleText.Text = "Otimização cancelada com segurança";
            RunDetailText.Text = "A solicitação foi concluída somente após a restauração obrigatória da configuração de referência.";
        }
        catch (Exception ex)
        {
            StageLabelText.Text = "INTERROMPIDO";
            RunTitleText.Text = "Sessão encerrada";
            RunDetailText.Text = $"{ex.Message} Verifique History antes de iniciar outra sessão.";
        }
        finally
        {
            _tuningCts.Dispose();
            _tuningCts = null;
            _isRunning = false;
            SetRunningState(false);
            LoadEnvironmentSelection();
            RefreshReadiness();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRunning || _tuningCts is null) return;

        CancelButton.IsEnabled = false;
        StageLabelText.Text = "CANCELANDO";
        RunTitleText.Text = "Cancelando com segurança";
        RunDetailText.Text = "A solicitação foi recebida. O rollback obrigatório terminará antes de a sessão ser encerrada.";
        _tuningCts.Cancel();
    }

    private void SetRunningState(bool running)
    {
        AdaptiveModeButton.IsEnabled = !running;
        DeepModeButton.IsEnabled = !running;
        FreeFireButton.IsEnabled = !running;
        FreeFireMaxButton.IsEnabled = !running;
        InstanceCombo.IsEnabled = !running;
        KeepDeepCheck.IsEnabled = !running;
        StartButton.IsEnabled = !running;
        CancelButton.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = running;
    }

    private void ApplyProgress(AutoTunerProgressPresentation visual)
    {
        RunCard.Visibility = Visibility.Visible;
        StageLabelText.Text = visual.StageLabel.ToUpperInvariant();
        RunTitleText.Text = visual.Title;
        RunDetailText.Text = visual.Detail;
        RunProgress.Value = visual.Percent;
        ProgressPercentText.Text = $"{visual.Percent}%";
        RunPercentHero.Text = $"{visual.Percent}%";
    }

    private void ShowResult(OptimizeWorkflowResult result)
    {
        ApplyProgress(new AutoTunerProgressPresentation(
            100,
            result.Session.ProfilesPersisted ? "Concluído e salvo" : "Concluído",
            result.Session.ProfilesPersisted ? "Otimização validada" : "Otimização concluída sem novo vencedor",
            result.Session.ProfilesPersisted
                ? "O benchmark terminou, o baseline foi restaurado e os perfis vencedores validados foram persistidos."
                : "Nenhum resultado inconclusivo substituiu os perfis conhecidos."));

        ResultCard.Visibility = Visibility.Visible;
        ResultSummaryText.Text = result.Summary;
        PersistenceStatusText.Text = result.Session.ProfilesPersisted
            ? "VALIDADO E SALVO"
            : "PERFIS PRESERVADOS";

        var recommended = result.Recommended;
        if (recommended is null)
        {
            ResultFpsText.Text = "—";
            ResultLowText.Text = "—";
            ResultFrameTimeText.Text = "—";
            ResultLatencyText.Text = "—";
            ResultConfidenceText.Text = "—";
            RecommendedConfigText.Text = "Nenhum perfil novo atingiu evidência suficiente para substituir a configuração conhecida.";
        }
        else
        {
            ResultFpsText.Text = FormatNumber(recommended.AverageFps, "0.0");
            ResultLowText.Text = FormatNumber(recommended.OnePercentLow, "0.0");
            ResultFrameTimeText.Text = FormatMetric(recommended.FrameTimeMs, "0.00", "ms");
            ResultLatencyText.Text = FormatMetric(recommended.LatencyMs, "0.0", "ms");
            ResultConfidenceText.Text = $"{Math.Round(Math.Clamp(recommended.Confidence, 0, 1) * 100, MidpointRounding.AwayFromZero):0}%";
            RecommendedConfigText.Text = $"{recommended.CpuCores} cores · {recommended.RamMb / 1024d:0.#} GB RAM · {recommended.Renderer} · alvo {recommended.FpsTarget} FPS · {recommended.Resolution}";
        }

        var winnerNames = result.Session.Tuning.Winners
            .Where(profile => profile.Evidence == EvidenceLevel.Validated)
            .Select(profile => profile.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        WinnersText.Text = winnerNames.Count == 0
            ? "Nenhum vencedor novo foi persistido; o último conjunto confiável permanece intacto."
            : $"Vencedores validados: {string.Join(" · ", winnerNames)}";
    }

    private void ApplyChoiceVisuals()
    {
        SetChoice(AdaptiveModeButton, _mode == AutoTunerMode.Adaptive);
        SetChoice(DeepModeButton, _mode == AutoTunerMode.Deep);
        SetSmallChoice(FreeFireButton, _game == GameKind.FreeFire);
        SetSmallChoice(FreeFireMaxButton, _game == GameKind.FreeFireMax);
    }

    private void SetChoice(Button button, bool selected)
    {
        button.Background = (Brush)FindResource(selected ? "GlassStrongBrush" : "GlassBrush");
        button.BorderBrush = (Brush)FindResource(selected ? "AccentBrush" : "GlassBorderBrush");
        button.BorderThickness = selected ? new Thickness(2) : new Thickness(1);
    }

    private void SetSmallChoice(Button button, bool selected)
    {
        button.Background = (Brush)FindResource(selected ? "AccentSoftBrush" : "GlassBrush");
        button.Foreground = (Brush)FindResource("TextBrush");
        button.BorderBrush = (Brush)FindResource(selected ? "AccentBrush" : "GlassBorderBrush");
        button.BorderThickness = new Thickness(1);
    }

    private static string FormatNumber(double? value, string format)
        => value is double number ? number.ToString(format, System.Globalization.CultureInfo.InvariantCulture) : "—";

    private static string FormatMetric(double? value, string format, string unit)
        => value is double number
            ? $"{number.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {unit}"
            : "—";
}
