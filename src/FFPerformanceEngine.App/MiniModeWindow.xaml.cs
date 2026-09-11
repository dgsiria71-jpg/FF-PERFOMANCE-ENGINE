using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FFPerformanceEngine.Core.Models;

namespace FFPerformanceEngine.App;

public partial class MiniModeWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _collapsed;

    public MiniModeWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (App.Services.Settings.ArgbEnabled && Resources["ArgbStoryboard"] is Storyboard storyboard) storyboard.Begin(this, true);
            _timer.Start();
            RefreshTelemetry();
        };
        Closed += (_, _) => _timer.Stop();
        _timer.Tick += (_, _) => RefreshTelemetry();
    }

    private void RefreshTelemetry()
    {
        var sample = App.Services.Telemetry.CaptureSystemSample();
        CpuText.Text = sample.CpuPercent is double cpu ? $"{cpu:0}%" : "—";
        GuardianText.Text = App.Services.Settings.GuardianEnabled ? $"● Guardian {App.Services.Guardian.Mode}" : "○ Guardian Off";
        var environment = App.Services.Environment.Capture();
        StateText.Text = environment.ActiveGame switch
        {
            GameKind.FreeFireMax => "● Free Fire MAX",
            GameKind.FreeFire => "● Free Fire",
            _ => environment.BlueStacksDetected ? "● BlueStacks" : "○ Aguardando jogo"
        };
    }

    private void UpdateFrameMetrics(TelemetrySample? sample)
    {
        if (sample is null) return;
        FpsText.Text = sample.Fps is double fps ? $"{fps:0}" : "—";
        LatencyText.Text = sample.LatencyMs is double latency ? $"{latency:0.0} ms" : "—";
    }

    private void Drag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        _collapsed = !_collapsed;
        Height = _collapsed ? 92 : 300;
        Width = _collapsed ? 410 : 620;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    public async Task RunQuickBoostAsync()
    {
        GuardianText.Text = "◉ Quick Boost...";
        GuardianText.Text = await App.Services.GuardianCanary.QuickBoostAsync();
    }

    private async void Boost_Click(object sender, RoutedEventArgs e) => await RunQuickBoostAsync();

    private void Profile_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Abra Profiles para escolher e aplicar perfis validados. Mudanças que exigem reinício nunca são forçadas no meio da partida.", "Quick Profiles");

    private async void Deep_Click(object sender, RoutedEventArgs e)
    {
        GuardianText.Text = "◉ Deep Scan: análise ampliada";
        var sample = await App.Services.PresentMon.CaptureAsync(TimeSpan.FromSeconds(8));
        UpdateFrameMetrics(sample);
        GuardianText.Text = sample is null ? "● Deep Scan sem frame evidence" : $"● Deep Scan: {sample.Fps:0} FPS · P95 {sample.FrameTimeP95Ms:0.0} ms";
    }

    public async Task RunMidGameOptimizeAsync()
    {
        App.Services.Guardian.SetState(GameState.Match);
        var profiles = await App.Services.Profiles.LoadAsync();
        var baseline = profiles
            .Where(x => x.Evidence == EvidenceLevel.Validated && x.AverageFps is > 0)
            .OrderByDescending(x => x.Kind == ProfileKind.Recommended)
            .ThenByDescending(x => x.Confidence)
            .FirstOrDefault();
        if (baseline?.AverageFps is not double expectedFps)
        {
            GuardianText.Text = "● Mid-Game requer um perfil validado como baseline";
            return;
        }

        GuardianText.Text = "◉ Mid-Game: medindo canary...";
        var result = await App.Services.GuardianCanary.TryAboveNormalPriorityAsync(
            expectedFps,
            App.Services.PresentMon.CaptureAsync,
            TimeSpan.FromSeconds(4));
        UpdateFrameMetrics(result.After ?? result.Before);
        GuardianText.Text = result.Message;
    }

    private async void MidGame_Click(object sender, RoutedEventArgs e) => await RunMidGameOptimizeAsync();
}
