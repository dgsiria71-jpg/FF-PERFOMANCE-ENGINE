using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FFPerformanceEngine.Core.Services;

namespace FFPerformanceEngine.App.Pages;

public partial class PerformancePage : UserControl
{
    private static readonly TimeSpan GraphContinuityWindow = TimeSpan.FromSeconds(12);

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private IReadOnlyList<PerformanceGraphSegment> _fpsGraphSegments = Array.Empty<PerformanceGraphSegment>();
    private DateTimeOffset? _graphStart;
    private DateTimeOffset? _graphEnd;
    private PerformanceIntervalWindowMode _windowMode = PerformanceIntervalWindowMode.Session;
    private PerformanceIntervalSummary? _currentInterval;
    private PerformanceIntervalSummary? _baselineInterval;
    private bool _captureInProgress;

    public PerformancePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            UpdateWindowButtons();
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
        RefreshTimeline();
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

    private void MarkMoment_Click(object sender, RoutedEventArgs e)
    {
        App.Services.PerformanceTimelineEvents.RecordUserMarker(
            DateTimeOffset.UtcNow,
            "Momento marcado manualmente pelo usuário.");
        RefreshTimeline();
    }

    private void SessionWindow_Click(object sender, RoutedEventArgs e)
    {
        _windowMode = PerformanceIntervalWindowMode.Session;
        UpdateWindowButtons();
        RefreshTimeline();
    }

    private void RecentWindow_Click(object sender, RoutedEventArgs e)
    {
        _windowMode = PerformanceIntervalWindowMode.Recent60Seconds;
        UpdateWindowButtons();
        RefreshTimeline();
    }

    private void PinBaseline_Click(object sender, RoutedEventArgs e)
    {
        if (_currentInterval is null || !HasFrameEvidence(_currentInterval))
        {
            BaselineStatusText.Text = "Baseline não fixado: o intervalo atual não possui evidência de frame suficiente.";
            return;
        }

        _baselineInterval = _currentInterval;
        ClearBaselineButton.IsEnabled = true;
        RefreshBaselineComparison();
    }

    private void ClearBaseline_Click(object sender, RoutedEventArgs e)
    {
        _baselineInterval = null;
        ClearBaselineButton.IsEnabled = false;
        RefreshBaselineComparison();
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

    private void RefreshTimeline()
    {
        var snapshot = App.Services.PerformanceTimeline.Snapshot();
        TimelineCountText.Text = snapshot.Count == 1
            ? "1 evento nesta sessão"
            : $"{snapshot.Count} eventos nesta sessão";

        if (snapshot.Count == 0)
        {
            TimelineText.Text = "Nenhum evento sincronizado ainda.";
            _currentInterval = null;
            ClearGraphPresentation();
            RefreshBaselineComparison();
            return;
        }

        var window = PerformanceIntervalWindowResolver.Resolve(snapshot, _windowMode);
        if (window is null)
        {
            _currentInterval = null;
            ClearGraphPresentation();
            RefreshBaselineComparison();
            return;
        }

        var summary = PerformanceIntervalAnalysis.Analyze(snapshot, window.Start, window.End);
        _currentInterval = summary;
        _fpsGraphSegments = PerformanceGraphSeriesBuilder.Build(
            summary.Points,
            PerformanceGraphMetric.Fps,
            GraphContinuityWindow);
        _graphStart = summary.Start;
        _graphEnd = summary.End;
        ApplyIntervalSummary(summary);
        RefreshBaselineComparison();
        RenderFpsGraph();

        var rows = PerformanceTimelinePresentation.Recent(snapshot, maxRows: 8);
        TimelineText.Text = string.Join(
            Environment.NewLine,
            rows.Select(row =>
            {
                var detail = string.IsNullOrWhiteSpace(row.Detail) ? string.Empty : $" · {row.Detail}";
                var metrics = row.Metrics == "—" ? string.Empty : $" · {row.Metrics}";
                return $"{row.Timestamp.ToLocalTime():HH:mm:ss} · {row.Title}{detail}{metrics}";
            }));
    }

    private void ApplyIntervalSummary(PerformanceIntervalSummary summary)
    {
        var presentation = PerformanceIntervalPresentation.FromSummary(summary);
        IntervalAverageFpsText.Text = presentation.AverageFps;
        IntervalFrameTimeText.Text = presentation.AverageFrameTime;
        IntervalEvidenceText.Text = presentation.Evidence;
        IntervalEventsText.Text = presentation.Events;
        var modeLabel = _windowMode == PerformanceIntervalWindowMode.Session ? "Sessão" : "Últimos 60 s";
        GraphRangeText.Text = $"{modeLabel} · {summary.Start.ToLocalTime():HH:mm:ss} — {summary.End.ToLocalTime():HH:mm:ss}";

        var measuredPoints = _fpsGraphSegments.Sum(segment => segment.Points.Count);
        GraphStatusText.Text = measuredPoints switch
        {
            0 => "Nenhuma evidência de FPS disponível para desenhar neste intervalo.",
            1 => "1 ponto de FPS medido. O ponto é exibido sem inventar uma linha contínua.",
            _ => $"{measuredPoints} pontos de FPS medidos · {_fpsGraphSegments.Count} trecho(s) contínuo(s). Lacunas acima de {GraphContinuityWindow.TotalSeconds:0} s ou FPS indisponível não são conectados."
        };

        PinBaselineButton.IsEnabled = HasFrameEvidence(summary);
    }

    private void RefreshBaselineComparison()
    {
        if (_baselineInterval is null)
        {
            BaselineStatusText.Text = "Nenhum baseline fixado.";
            BaselineFpsDeltaText.Text = "—";
            BaselineFrameTimeDeltaText.Text = "—";
            return;
        }

        var baseline = _baselineInterval;
        BaselineStatusText.Text = $"Baseline fixado · {baseline.Start.ToLocalTime():HH:mm:ss} — {baseline.End.ToLocalTime():HH:mm:ss}. Deltas = intervalo atual − baseline.";

        if (_currentInterval is null)
        {
            BaselineFpsDeltaText.Text = "—";
            BaselineFrameTimeDeltaText.Text = "—";
            return;
        }

        var presentation = PerformanceIntervalPresentation.FromComparison(
            PerformanceIntervalAnalysis.Compare(baseline, _currentInterval));
        BaselineFpsDeltaText.Text = presentation.AverageFpsDelta;
        BaselineFrameTimeDeltaText.Text = presentation.AverageFrameTimeDelta;
    }

    private void UpdateWindowButtons()
    {
        SessionWindowButton.IsEnabled = _windowMode != PerformanceIntervalWindowMode.Session;
        RecentWindowButton.IsEnabled = _windowMode != PerformanceIntervalWindowMode.Recent60Seconds;
    }

    private static bool HasFrameEvidence(PerformanceIntervalSummary summary)
        => summary.FpsEvidenceSamples > 0 || summary.AverageFrameTimeMs is not null;

    private void ClearGraphPresentation()
    {
        _fpsGraphSegments = Array.Empty<PerformanceGraphSegment>();
        _graphStart = null;
        _graphEnd = null;
        FpsGraphCanvas.Children.Clear();
        GraphRangeText.Text = "Sem intervalo";
        GraphStatusText.Text = "Nenhuma evidência de FPS neste intervalo.";
        IntervalAverageFpsText.Text = "—";
        IntervalFrameTimeText.Text = "—";
        IntervalEvidenceText.Text = "0/0 amostras com FPS";
        IntervalEventsText.Text = "Guardian 0 · Marcadores 0";
        PinBaselineButton.IsEnabled = false;
    }

    private void FpsGraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => RenderFpsGraph();

    private void RenderFpsGraph()
    {
        FpsGraphCanvas.Children.Clear();

        var points = _fpsGraphSegments.SelectMany(segment => segment.Points).ToArray();
        if (points.Length == 0 || _graphStart is not DateTimeOffset start || _graphEnd is not DateTimeOffset end)
            return;

        var width = FpsGraphCanvas.ActualWidth;
        var height = FpsGraphCanvas.ActualHeight;
        if (width <= 2 || height <= 2) return;

        const double padding = 14;
        var plotWidth = Math.Max(1, width - padding * 2);
        var plotHeight = Math.Max(1, height - padding * 2);
        var minValue = points.Min(point => point.Value);
        var maxValue = points.Max(point => point.Value);
        if (Math.Abs(maxValue - minValue) < 0.0001)
        {
            var pad = Math.Max(1, Math.Abs(maxValue) * 0.05);
            minValue -= pad;
            maxValue += pad;
        }

        for (var i = 0; i <= 3; i++)
        {
            var y = padding + plotHeight * i / 3d;
            var gridLine = new Line
            {
                X1 = padding,
                X2 = padding + plotWidth,
                Y1 = y,
                Y2 = y,
                StrokeThickness = 1,
                Opacity = 0.28
            };
            gridLine.SetResourceReference(Shape.StrokeProperty, "AccentSoftBrush");
            FpsGraphCanvas.Children.Add(gridLine);
        }

        foreach (var segment in _fpsGraphSegments)
        {
            if (segment.Points.Count >= 2)
            {
                var line = new Polyline
                {
                    StrokeThickness = 2.2,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    SnapsToDevicePixels = true
                };
                line.SetResourceReference(Shape.StrokeProperty, "AccentBrush");

                foreach (var point in segment.Points)
                    line.Points.Add(ToCanvasPoint(point, start, end, minValue, maxValue, padding, plotWidth, plotHeight));

                FpsGraphCanvas.Children.Add(line);
            }

            foreach (var point in segment.Points)
            {
                var canvasPoint = ToCanvasPoint(point, start, end, minValue, maxValue, padding, plotWidth, plotHeight);
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    ToolTip = $"{point.Timestamp.ToLocalTime():HH:mm:ss} · {point.Value:0.0} FPS · {point.DataQuality}"
                };
                dot.SetResourceReference(Shape.FillProperty, "AccentBrush");
                Canvas.SetLeft(dot, canvasPoint.X - 3);
                Canvas.SetTop(dot, canvasPoint.Y - 3);
                FpsGraphCanvas.Children.Add(dot);
            }
        }
    }

    private static Point ToCanvasPoint(
        PerformanceGraphPoint point,
        DateTimeOffset start,
        DateTimeOffset end,
        double minValue,
        double maxValue,
        double padding,
        double plotWidth,
        double plotHeight)
    {
        var timeRangeMs = Math.Max(0, (end - start).TotalMilliseconds);
        var xRatio = timeRangeMs <= 0
            ? 0.5
            : Math.Clamp((point.Timestamp - start).TotalMilliseconds / timeRangeMs, 0, 1);
        var valueRange = Math.Max(double.Epsilon, maxValue - minValue);
        var yRatio = Math.Clamp((point.Value - minValue) / valueRange, 0, 1);

        return new Point(
            padding + plotWidth * xRatio,
            padding + plotHeight * (1 - yRatio));
    }
}
