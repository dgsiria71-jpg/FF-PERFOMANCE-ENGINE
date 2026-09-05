using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class PerformancePage : UserControl
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private bool _captureInProgress;

    public PerformancePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Refresh();
            _timer.Start();
        };
        Unloaded += (_, _) => _timer.Stop();
        _timer.Tick += (_, _) => Refresh();
    }

    private void Refresh()
    {
        var system = App.Services.Telemetry.CaptureSystemSample();
        CpuText.Text = system.CpuPercent is double cpu ? $"{cpu:0}%" : "—";
        RamText.Text = system.MemoryUsedGb is double used && system.MemoryTotalGb is double total
            ? $"{used:0.0}/{total:0.0} GB"
            : "—";

        var presentMonAvailable = App.Services.PresentMon.FindExecutable() is not null;
        ProviderText.Text = presentMonAvailable ? "PresentMon pronto" : "PresentMon indisponível";

        var target = PerformanceCaptureTargetPolicy.FromGuardianStatus(App.Services.GuardianHost.CurrentStatus);
        if (target.CanCapture)
        {
            TargetText.Text = $"Instância {target.InstanceName} · PID {target.ProcessId}";
            TargetDetailText.Text = "O mesmo processo vinculado pelo Guardian será medido; não há seleção aproximada de outro HD-Player.";
        }
        else
        {
            TargetText.Text = "Aguardando vínculo exato do Guardian";
            TargetDetailText.Text = "A captura permanece bloqueada até existir uma instância e um PID BlueStacks inequívocos.";
        }

        MeasureButton.IsEnabled = !_captureInProgress && presentMonAvailable && target.CanCapture;
    }

    private async void Measure_Click(object sender, RoutedEventArgs e)
    {
        if (_captureInProgress) return;

        _captureInProgress = true;
        MeasureButton.IsEnabled = false;
        MeasureButton.Content = "Medindo...";
        CaptureDetailText.Text = "Capturando 10 segundos do PID vinculado pelo Guardian.";

        try
        {
            var result = await App.Services.PerformanceCapture.CaptureAsync(
                App.Services.GuardianHost.CurrentStatus,
                TimeSpan.FromSeconds(10));
            ApplyPresentation(PerformancePresentation.FromCapture(result));
        }
        catch (OperationCanceledException)
        {
            CaptureDetailText.Text = "Captura cancelada. Nenhum valor foi inventado ou preservado como medição nova.";
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            CaptureDetailText.Text = $"Captura indisponível: {ex.Message}";
        }
        finally
        {
            _captureInProgress = false;
            MeasureButton.Content = "Medir 10 segundos";
            Refresh();
        }
    }

    private void ApplyPresentation(PerformanceCapturePresentation presentation)
    {
        FpsText.Text = presentation.Fps;
        OneLowText.Text = presentation.OnePercentLow;
        PointOneLowText.Text = presentation.PointOnePercentLow;
        FrameTimeText.Text = presentation.FrameTime;
        P95Text.Text = presentation.P95FrameTime;
        P99Text.Text = presentation.P99FrameTime;
        StutterText.Text = presentation.Stutter;
        LatencyText.Text = presentation.Latency;
        DataQualityText.Text = presentation.DataQuality;
        CaptureDetailText.Text = presentation.Detail;

        if (presentation.ProcessId != "—" && presentation.Instance != "—")
            TargetText.Text = $"Instância {presentation.Instance} · PID {presentation.ProcessId}";
    }
}
