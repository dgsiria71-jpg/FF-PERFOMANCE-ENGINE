using System.Windows;
using System.Windows.Controls;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.App.Pages;

public partial class OptimizePage : UserControl
{
    private AutoTunerMode _mode;
    private GameKind _game;
    private IReadOnlyList<TuningCandidate> _candidates = Array.Empty<TuningCandidate>();

    public OptimizePage()
    {
        InitializeComponent();
        _mode = App.Services.Settings.KeepDeepAsDefault ? AutoTunerMode.Deep : App.Services.Settings.DefaultTunerMode;
        _game = App.Services.Settings.PreferredGame == GameKind.None ? GameKind.FreeFireMax : App.Services.Settings.PreferredGame;
        Loaded += (_, _) => Refresh();
    }

    private void Adaptive_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _mode = AutoTunerMode.Adaptive;
        Refresh();
    }

    private void Deep_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _mode = AutoTunerMode.Deep;
        Refresh();
    }

    private void FreeFire_Click(object sender, RoutedEventArgs e)
    {
        _game = GameKind.FreeFire;
        Refresh();
    }

    private void FreeFireMax_Click(object sender, RoutedEventArgs e)
    {
        _game = GameKind.FreeFireMax;
        Refresh();
    }

    private void Refresh()
    {
        var environment = App.Services.Environment.Capture();
        if (App.Services.Settings.PreferredGame == GameKind.None && environment.ActiveGame != GameKind.None)
            _game = environment.ActiveGame;

        var instance = environment.Instances.FirstOrDefault();
        ModeText.Text = $"Modo: {_mode}";
        GameText.Text = $"Jogo: {DisplayGame(_game)}";
        EnvironmentText.Text = environment.BlueStacksDetected
            ? $"BlueStacks detectado · {instance?.Name ?? "instância não identificada"}"
            : "BlueStacks não detectado";
        _candidates = App.Services.AutoTuner.GenerateCandidates(environment, instance, _mode);
        CandidateText.Text = $"{_candidates.Count} candidatos seguros gerados para exploração.";
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        Refresh();
        ResultText.Text = $"Gerados {_candidates.Count} candidatos. Nenhum vencedor será escolhido antes de benchmark real.";
        Progress.Value = 15;
    }

    private async void Prepare_Click(object sender, RoutedEventArgs e)
    {
        var environment = App.Services.Environment.Capture();
        var instance = environment.Instances.FirstOrDefault();
        if (instance is null)
        {
            ResultText.Text = "Nenhuma instância BlueStacks foi detectada. Abra o Multi-instance Manager ou configure uma instância e tente novamente.";
            Progress.Value = 0;
            return;
        }

        if (instance.AdbPort is null)
        {
            ResultText.Text = $"A instância {instance.Name} não expõe uma porta ADB no bluestacks.conf. O modo assistido será necessário para abrir {DisplayGame(_game)}.";
            Progress.Value = 0;
            return;
        }

        Progress.IsIndeterminate = true;
        ResultText.Text = $"Preparando {instance.Name} e abrindo {DisplayGame(_game)}...";
        try
        {
            var result = await App.Services.BlueStacksAutomation.PrepareGameAsync(
                instance,
                _game,
                foregroundTimeout: TimeSpan.FromSeconds(45),
                startupDelay: TimeSpan.FromSeconds(8),
                pollInterval: TimeSpan.FromMilliseconds(750));

            ResultText.Text = result.Success
                ? $"{DisplayGame(_game)} está em primeiro plano. Ambiente pronto para estabilização e benchmark real."
                : $"Automação não concluída: {result.Message} Você pode continuar em modo assistido sem perder o estado atual.";
            Progress.Value = result.Success ? 35 : 0;
        }
        catch (OperationCanceledException)
        {
            ResultText.Text = "Preparação cancelada. Nenhuma configuração de benchmark foi aplicada.";
            Progress.Value = 0;
        }
        finally
        {
            Progress.IsIndeterminate = false;
        }
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        Progress.IsIndeterminate = true;
        ResultText.Text = "Capturando 12 segundos de frame telemetry via PresentMon...";
        var sample = await App.Services.PresentMon.CaptureAsync(TimeSpan.FromSeconds(12));
        Progress.IsIndeterminate = false;
        if (sample is null)
        {
            ResultText.Text = App.Services.PresentMon.FindExecutable() is null
                ? "PresentMon não está instalado. Execute scripts/Get-PresentMon.ps1 e tente novamente."
                : "Não foi possível medir frames. Inicie uma instância BlueStacks ativa e tente novamente.";
            Progress.Value = 0;
            return;
        }

        ResultText.Text = $"FPS {sample.Fps:0.0} · 1% Low {sample.OnePercentLow:0.0} · Frame Time {sample.FrameTimeMs:0.00} ms · Latência {(sample.LatencyMs?.ToString("0.0") ?? "—")} ms. Evidência registrada; execute candidatos adicionais para comparar.";
        Progress.Value = 100;
        await App.Services.History.AppendAsync(new HistoryEvent
        {
            Kind = HistoryEventKind.Benchmark,
            Title = "Benchmark real",
            Summary = ResultText.Text
        });
    }

    private static string DisplayGame(GameKind game) => game switch
    {
        GameKind.FreeFire => "Free Fire",
        GameKind.FreeFireMax => "Free Fire MAX",
        _ => "Free Fire MAX"
    };
}
