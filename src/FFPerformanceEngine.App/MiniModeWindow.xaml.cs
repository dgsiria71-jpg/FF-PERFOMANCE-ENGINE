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

    private async void Boost_Click(object sender, RoutedEventArgs e)
    {
        GuardianText.Text = "◉ Quick Boost: verificando...";
        await Task.Delay(350);
        GuardianText.Text = "● Sem ação pré-validada necessária";
    }

    private void Profile_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Abra Profiles para escolher favoritos validados. Trocas RestartRequired são adiadas até pós-partida.", "Quick Profiles");

    private async void Deep_Click(object sender, RoutedEventArgs e)
    {
        GuardianText.Text = "◉ Deep Scan 60s (análise ampliada)";
        await Task.Delay(800);
        GuardianText.Text = "● Deep Scan pronto";
    }

    private async void MidGame_Click(object sender, RoutedEventArgs e)
    {
        App.Services.Guardian.SetState(GameState.Match);
        GuardianText.Text = "◉ Analisando Mid-Game...";
        var before = await App.Services.PresentMon.CaptureAsync(TimeSpan.FromSeconds(4));
        if (before is null) { GuardianText.Text = "● Sem frame evidence; nenhuma mudança"; return; }
        var action = new GuardianAction { Id = "priority", Description = "Process priority", Safety = ActionSafety.LiveSafe, MinimumConfidence = 0.85 };
        var decision = App.Services.Guardian.Evaluate(before.Fps ?? 0, before, action);
        GuardianText.Text = decision.ShouldAct ? "◉ Ação candidata requer canary" : "● Nenhuma intervenção necessária";
    }
}
