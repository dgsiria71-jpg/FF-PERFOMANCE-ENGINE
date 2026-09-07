using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

namespace FFPerformanceEngine.App.Pages;

public partial class OptimizePage : UserControl
{
    private AutoTunerMode _mode;
    private GameKind _game;
    private CancellationTokenSource? _tuningCts;
    private bool _isRunning;
    private bool _pcOperationRunning;
    private bool _initializing = true;
    private PersistentPcOptimizationPreview? _pcPreview;
    private Guid? _pcRestorePointId;

    private bool IsBusy => _isRunning || _pcOperationRunning;

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
        PcAnalyzeButton.Click += PcAnalyze_Click;
        PcApplyButton.Click += PcApply_Click;
        PcRestoreButton.Click += PcRestore_Click;
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

    private async void PcAnalyze_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;

        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Analisando estado real do Windows";
        PcOptimizationDetailText.Text = "Atualizando capabilities e construindo o preview a partir do estado atual comprovado.";

        try
        {
            var presentation = await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = presentation.CanApply
                ? "Preview pronto para revisão"
                : "Nenhuma alteração persistente elegível";
            PcOptimizationDetailText.Text = presentation.CanApply
                ? "Revise cada capability abaixo. Aplicar usa exatamente este preview e revalida tudo antes do primeiro snapshot/mutation."
                : "O Core manteve todas as capabilities como SKIP. Nenhuma alteração será aplicada até existir recomendação elegível para este PC.";
        }
        catch (Exception ex)
        {
            ResetPersistentPcPreview();
            PcOptimizationStatusText.Text = "Não foi possível analisar este PC";
            PcOptimizationDetailText.Text = ex.Message;
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async void PcApply_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _pcPreview?.CanApply != true) return;

        var preview = _pcPreview;
        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Aplicando preview com proteção transacional";
        PcOptimizationDetailText.Text = "Aguardando a lease global, revalidando fingerprint/estado e criando restore point antes de qualquer mutation.";

        try
        {
            var result = await App.Services.PersistentPcOptimization.ApplyAsync(preview);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            _pcRestorePointId = result.RestorePointId;
            PcRestorePointText.Text = $"Restore point: {ShortId(result.RestorePointId)} · disponível para desfazer esta aplicação";
            PcRestoreButton.Visibility = Visibility.Visible;

            var refreshed = await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = "Otimização persistente aplicada";
            PcOptimizationDetailText.Text = refreshed.CanApply
                ? "A aplicação foi verificada e registrada. O estado mudou novamente durante a atualização do preview; revise antes de qualquer nova aplicação."
                : "A aplicação foi verificada e registrada no History. O restore point preserva o estado exato anterior.";
        }
        catch (PersistentPcOptimizationDriftException ex)
        {
            ResetPersistentPcPreview();
            PcOptimizationStatusText.Text = "Preview ficou desatualizado";
            PcOptimizationDetailText.Text = $"{ex.Message} Analise este PC novamente antes de aplicar.";
        }
        catch (Exception ex)
        {
            PcOptimizationStatusText.Text = "Aplicação não concluída";
            PcOptimizationDetailText.Text = $"{ex.Message} O backend transacional mantém rollback/History como autoridade de recuperação.";
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async void PcRestore_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy || _pcRestorePointId is not Guid restorePointId) return;

        SetPcOperationState(true);
        PcOptimizationStatusText.Text = "Restaurando estado anterior";
        PcOptimizationDetailText.Text = "A restauração usa a mesma lease global e o restore point durável criado antes da aplicação.";

        try
        {
            var result = await App.Services.PersistentPcOptimization.RestoreAsync(restorePointId);
            if (!result.Success)
                throw new InvalidOperationException(result.Message);

            _pcRestorePointId = null;
            PcRestorePointText.Text = string.Empty;
            PcRestoreButton.Visibility = Visibility.Collapsed;
            await RefreshPersistentPcPreviewCoreAsync();
            PcOptimizationStatusText.Text = "Estado anterior restaurado";
            PcOptimizationDetailText.Text = "As capabilities do restore point foram verificadas após o rollback. Um novo preview pode ser aplicado somente depois da revalidação atual.";
        }
        catch (Exception ex)
        {
            PcOptimizationStatusText.Text = "Restauração não concluída";
            PcOptimizationDetailText.Text = $"{ex.Message} Consulte History para o estado auditado da transação.";
        }
        finally
        {
            SetPcOperationState(false);
            RefreshReadiness();
        }
    }

    private async Task<PersistentPcOptimizationPresentation> RefreshPersistentPcPreviewCoreAsync()
    {
        var preview = await App.Services.PersistentPcOptimization.AnalyzeAsync();
        _pcPreview = preview;

        var presentation = PersistentPcOptimizationPresentation.Create(preview);
        PcOptimizationEntries.ItemsSource = presentation.Entries;
        PcOptimizationEntries.Visibility = presentation.TotalCount > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        PcOptimizationCountsText.Text = $"{presentation.ReadyCount} pronta(s) · {presentation.SkippedCount} ignorada(s) · {presentation.TotalCount} total";
        PcApplyButton.IsEnabled = presentation.CanApply && !IsBusy;
        return presentation;
    }

    private void ResetPersistentPcPreview()
    {
        _pcPreview = null;
        PcOptimizationEntries.ItemsSource = null;
        PcOptimizationEntries.Visibility = Visibility.Collapsed;
        PcOptimizationCountsText.Text = "Preview indisponível";
        PcApplyButton.IsEnabled = false;
    }

    private void SetPcOperationState(bool running)
    {
        _pcOperationRunning = running;

        PcAnalyzeButton.IsEnabled = !running && !_isRunning;
        PcApplyButton.IsEnabled = !running && !_isRunning && _pcPreview?.CanApply == true;
        PcRestoreButton.IsEnabled = !running && !_isRunning && _pcRestorePointId.HasValue;

        AdaptiveModeButton.IsEnabled = !running && !_isRunning;
        DeepModeButton.IsEnabled = !running && !_isRunning;
        FreeFireButton.IsEnabled = !running && !_isRunning;
        FreeFireMaxButton.IsEnabled = !running && !_isRunning;
        InstanceCombo.IsEnabled = !running && !_isRunning;
        KeepDeepCheck.IsEnabled = !running && !_isRunning;
        StartButton.IsEnabled = !running && !_isRunning;
    }

    private void AdaptiveMode_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _mode = AutoTunerMode.Adaptive;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void DeepMode_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _mode = AutoTunerMode.Deep;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFire_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _game = GameKind.FreeFire;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void FreeFireMax_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        _game = GameKind.FreeFireMax;
        ApplyChoiceVisuals();
        RefreshReadiness();
    }

    private void InstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || IsBusy) return;
        RefreshReadiness();
    }

    private async void KeepDeepCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing || IsBusy) return;

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
        if (_initializing || IsBusy) return;

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
        StartButton.IsEnabled = readiness.CanStart && !IsBusy;
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;

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
        AdaptiveModeButton.IsEnabled = !running && !_pcOperationRunning;
        DeepModeButton.IsEnabled = !running && !_pcOperationRunning;
        FreeFireButton.IsEnabled = !running && !_pcOperationRunning;
        FreeFireMaxButton.IsEnabled = !running && !_pcOperationRunning;
        InstanceCombo.IsEnabled = !running && !_pcOperationRunning;
        KeepDeepCheck.IsEnabled = !running && !_pcOperationRunning;
        StartButton.IsEnabled = !running && !_pcOperationRunning;
        CancelButton.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.IsEnabled = running;

        PcAnalyzeButton.IsEnabled = !running && !_pcOperationRunning;
        PcApplyButton.IsEnabled = !running && !_pcOperationRunning && _pcPreview?.CanApply == true;
        PcRestoreButton.IsEnabled = !running && !_pcOperationRunning && _pcRestorePointId.HasValue;
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

    private static string ShortId(Guid value)
        => value.ToString("N")[..8].ToUpperInvariant();

    private static string FormatNumber(double? value, string format)
        => value is double number ? number.ToString(format, System.Globalization.CultureInfo.InvariantCulture) : "—";

    private static string FormatMetric(double? value, string format, string unit)
        => value is double number
            ? $"{number.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {unit}"
            : "—";
}
