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

    private void CaptureBaseline_Click(object sender, RoutedEventArgs e)
    {
        if (_currentInterval is null || !HasFrameEvidence(_currentInterval))
        {
            ComparisonStatusText.Text = "A não foi capturado: o intervalo atual não possui evidência de frame suficiente.";
            return;
        }

        App.Services.PerformanceComparison.SetBaseline(
            $"A · Baseline · {CurrentWindowLabel()}",
            _currentInterval);
        RefreshComparisonUi();
        RenderFpsGraph();
    }

    private void CaptureCandidate_Click(object sender, RoutedEventArgs e)
    {
        if (_currentInterval is null || !HasFrameEvidence(_currentInterval))
        {
            ComparisonStatusText.Text = "B não foi capturado: o intervalo atual não possui evidência de frame suficiente.";
            return;
        }

        App.Services.PerformanceComparison.SetCandidate(
            $"B · Candidato · {CurrentWindowLabel()}",
            _currentInterval);
        RefreshComparisonUi();
        RenderFpsGraph();
    }

    private void ClearComparison_Click(object sender, RoutedEventArgs e)
    {
        App.Services.PerformanceComparison.Clear();
        RefreshComparisonUi();
        RenderFpsGraph();
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
        var sessionSnapshot = App.Services.PerformanceTimeline.Snapshot();
        if (sessionSnapshot.Count == 0)
        {
            TimelineCountText.Text = "0 eventos neste intervalo";
            TimelineText.Text = "Nenhum evento sincronizado ainda.";
            _currentInterval = null;
            ClearGraphPresentation();
            RefreshComparisonUi();
            RenderFpsGraph();
            return;
        }

        var window = PerformanceIntervalWindowResolver.Resolve(sessionSnapshot, _windowMode);
        if (window is null)
        {
            TimelineCountText.Text = "0 eventos neste intervalo";
            TimelineText.Text = "Nenhum evento sincronizado neste intervalo.";
            _currentInterval = null;
            ClearGraphPresentation();
            RefreshComparisonUi();
            RenderFpsGraph();
            return;
        }

        var intervalEntries = App.Services.PerformanceTimeline.Snapshot(window.Start, window.End);
        TimelineCountText.Text = intervalEntries.Count == 1
            ? "1 evento neste intervalo"
            : $"{intervalEntries.Count} eventos neste intervalo";

        var summary = PerformanceIntervalAnalysis.Analyze(intervalEntries, window.Start, window.End);
        _currentInterval = summary;
        _fpsGraphSegments = PerformanceGraphSeriesBuilder.Build(
            summary.Points,
            PerformanceGraphMetric.Fps,
            GraphContinuityWindow);
        _graphStart = summary.Start;
        _graphEnd = summary.End;
        ApplyIntervalSummary(summary);
        RefreshComparisonUi();
        RenderFpsGraph();

        var rows = PerformanceTimelinePresentation.Recent(intervalEntries, maxRows: 8);
        TimelineText.Text = rows.Count == 0
            ? "Nenhum evento sincronizado neste intervalo."
            : string.Join(
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
        GraphRangeText.Text = $"{CurrentWindowLabel()} · {summary.Start.ToLocalTime():HH:mm:ss} — {summary.End.ToLocalTime():HH:mm:ss}";

        var hasEvidence = HasFrameEvidence(summary);
        CaptureBaselineButton.IsEnabled = hasEvidence;
        CaptureCandidateButton.IsEnabled = hasEvidence;
    }

    private void RefreshComparisonUi()
    {
        var baseline = App.Services.PerformanceComparison.Baseline;
        var candidate = App.Services.PerformanceComparison.Candidate;

        ApplySnapshotCard(baseline, baselineCard: true);
        ApplySnapshotCard(candidate, baselineCard: false);
        ClearComparisonButton.IsEnabled = baseline is not null || candidate is not null;

        var comparison = App.Services.PerformanceComparison.CurrentComparison;
        if (comparison is null)
        {
            ComparisonFpsDeltaText.Text = "—";
            ComparisonFrameTimeDeltaText.Text = "—";
            ComparisonStatusText.Text = baseline is null && candidate is null
                ? "Capture A e B para comparar."
                : baseline is null
                    ? "B está congelado. Capture A para concluir a comparação."
                    : "A está congelado. Capture B para concluir a comparação.";
            ComparisonTimelineText.Text = "Os intervalos capturados permanecem congelados enquanto a telemetria ao vivo continua.";
            return;
        }

        var presentation = PerformanceIntervalPresentation.FromComparison(comparison.Metrics);
        ComparisonFpsDeltaText.Text = presentation.AverageFpsDelta;
        ComparisonFrameTimeDeltaText.Text = presentation.AverageFrameTimeDelta;
        ComparisonStatusText.Text = $"{comparison.Baseline.Name}  →  {comparison.Candidate.Name}. Deltas são B − A e usam apenas métricas disponíveis em ambos os lados.";
        ComparisonTimelineText.Text =
            $"A {FormatRange(comparison.Baseline.Interval)} · Guardian {comparison.Baseline.Interval.GuardianEvents} · Marcadores {comparison.Baseline.Interval.UserMarkers}  |  " +
            $"B {FormatRange(comparison.Candidate.Interval)} · Guardian {comparison.Candidate.Interval.GuardianEvents} · Marcadores {comparison.Candidate.Interval.UserMarkers}.";
    }

    private void ApplySnapshotCard(PerformanceEvidenceSnapshot? snapshot, bool baselineCard)
    {
        var name = baselineCard ? BaselineNameText : CandidateNameText;
        var range = baselineCard ? BaselineRangeText : CandidateRangeText;
        var quality = baselineCard ? BaselineQualityText : CandidateQualityText;
        var metrics = baselineCard ? BaselineMetricsText : CandidateMetricsText;
        var evidence = baselineCard ? BaselineEvidenceText : CandidateEvidenceText;

        if (snapshot is null)
        {
            name.Text = "Não capturado";
            range.Text = "—";
            quality.Text = "Qualidade —";
            metrics.Text = "FPS — · Frame Time — · Latência —";
            evidence.Text = "FPS 0/0 · Frame Time 0/0 · Latência 0/0";
            return;
        }

        name.Text = snapshot.Name;
        range.Text = $"{FormatRange(snapshot.Interval)} · capturado {snapshot.CapturedAt.ToLocalTime():HH:mm:ss}";
        quality.Text = $"Qualidade {QualityLabel(snapshot.Quality)}";
        metrics.Text =
            $"FPS {FormatMetric(snapshot.AverageFps, "0.0", string.Empty)} · " +
            $"Frame Time {FormatMetric(snapshot.AverageFrameTimeMs, "0.00", " ms")} · " +
            $"Latência {FormatMetric(snapshot.AverageLatencyMs, "0.00", " ms")}";
        evidence.Text =
            $"FPS {snapshot.FpsEvidenceSamples}/{snapshot.TelemetrySamples} · " +
            $"Frame Time {snapshot.FrameTimeEvidenceSamples}/{snapshot.TelemetrySamples} · " +
            $"Latência {snapshot.LatencyEvidenceSamples}/{snapshot.TelemetrySamples}";
    }

    private void UpdateWindowButtons()
    {
        SessionWindowButton.IsEnabled = _windowMode != PerformanceIntervalWindowMode.Session;
        RecentWindowButton.IsEnabled = _windowMode != PerformanceIntervalWindowMode.Recent60Seconds;
    }

    private string CurrentWindowLabel()
        => _windowMode == PerformanceIntervalWindowMode.Session ? "Sessão" : "Últimos 60 s";

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
        CaptureBaselineButton.IsEnabled = false;
        CaptureCandidateButton.IsEnabled = false;
    }

    private void FpsGraphCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        => RenderFpsGraph();

    private void RenderFpsGraph()
    {
        FpsGraphCanvas.Children.Clear();

        var comparison = App.Services.PerformanceComparison.CurrentComparison;
        if (comparison is not null)
        {
            RenderComparisonGraph(comparison);
            return;
        }

        RenderLiveGraph();
    }

    private void RenderLiveGraph()
    {
        var points = _fpsGraphSegments.SelectMany(segment => segment.Points).ToArray();
        if (points.Length == 0 || _graphStart is not DateTimeOffset start || _graphEnd is not DateTimeOffset end)
        {
            GraphStatusText.Text = "Nenhuma evidência de FPS disponível para desenhar neste intervalo.";
            return;
        }

        if (!TryGetPlot(out var padding, out var plotWidth, out var plotHeight)) return;
        var (minValue, maxValue) = ValueRange(points);
        DrawGrid(padding, plotWidth, plotHeight);
        DrawSegments(
            _fpsGraphSegments,
            start,
            end,
            minValue,
            maxValue,
            padding,
            plotWidth,
            plotHeight,
            "AccentBrush",
            dashed: false,
            seriesLabel: "Atual");

        var measuredPoints = points.Length;
        GraphStatusText.Text = measuredPoints switch
        {
            1 => "1 ponto de FPS medido. O ponto é exibido sem inventar uma linha contínua.",
            _ => $"{measuredPoints} pontos de FPS medidos · {_fpsGraphSegments.Count} trecho(s) contínuo(s). Lacunas acima de {GraphContinuityWindow.TotalSeconds:0} s ou FPS indisponível não são conectados."
        };
    }

    private void RenderComparisonGraph(PerformanceABComparison comparison)
    {
        var baselineSegments = PerformanceGraphSeriesBuilder.Build(
            comparison.Baseline.Interval.Points,
            PerformanceGraphMetric.Fps,
            GraphContinuityWindow);
        var candidateSegments = PerformanceGraphSeriesBuilder.Build(
            comparison.Candidate.Interval.Points,
            PerformanceGraphMetric.Fps,
            GraphContinuityWindow);
        var allPoints = baselineSegments.SelectMany(segment => segment.Points)
            .Concat(candidateSegments.SelectMany(segment => segment.Points))
            .ToArray();

        GraphRangeText.Text = "A/B · progresso relativo 0–100%";
        if (allPoints.Length == 0)
        {
            GraphStatusText.Text = "A e B não possuem evidência de FPS suficiente para desenhar o overlay.";
            return;
        }

        if (!TryGetPlot(out var padding, out var plotWidth, out var plotHeight)) return;
        var (minValue, maxValue) = ValueRange(allPoints);
        DrawGrid(padding, plotWidth, plotHeight);

        DrawSegments(
            baselineSegments,
            comparison.Baseline.Interval.Start,
            comparison.Baseline.Interval.End,
            minValue,
            maxValue,
            padding,
            plotWidth,
            plotHeight,
            "MutedTextBrush",
            dashed: true,
            seriesLabel: "A");
        DrawSegments(
            candidateSegments,
            comparison.Candidate.Interval.Start,
            comparison.Candidate.Interval.End,
            minValue,
            maxValue,
            padding,
            plotWidth,
            plotHeight,
            "AccentBrush",
            dashed: false,
            seriesLabel: "B");

        var baselinePoints = baselineSegments.Sum(segment => segment.Points.Count);
        var candidatePoints = candidateSegments.Sum(segment => segment.Points.Count);
        GraphStatusText.Text =
            $"A {baselinePoints} ponto(s) / {baselineSegments.Count} trecho(s) · B {candidatePoints} ponto(s) / {candidateSegments.Count} trecho(s). " +
            "Os dois intervalos são alinhados somente por progresso relativo para comparação visual; cada tooltip preserva o timestamp real e nenhuma lacuna é conectada.";
    }

    private bool TryGetPlot(out double padding, out double plotWidth, out double plotHeight)
    {
        padding = 14;
        var width = FpsGraphCanvas.ActualWidth;
        var height = FpsGraphCanvas.ActualHeight;
        if (width <= 2 || height <= 2)
        {
            plotWidth = 0;
            plotHeight = 0;
            return false;
        }

        plotWidth = Math.Max(1, width - padding * 2);
        plotHeight = Math.Max(1, height - padding * 2);
        return true;
    }

    private void DrawGrid(double padding, double plotWidth, double plotHeight)
    {
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
    }

    private void DrawSegments(
        IReadOnlyList<PerformanceGraphSegment> segments,
        DateTimeOffset start,
        DateTimeOffset end,
        double minValue,
        double maxValue,
        double padding,
        double plotWidth,
        double plotHeight,
        string brushResource,
        bool dashed,
        string seriesLabel)
    {
        foreach (var segment in segments)
        {
            if (segment.Points.Count >= 2)
            {
                var line = new Polyline
                {
                    StrokeThickness = dashed ? 1.8 : 2.4,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    SnapsToDevicePixels = true,
                    Opacity = dashed ? 0.82 : 1.0
                };
                if (dashed) line.StrokeDashArray = new DoubleCollection { 5, 4 };
                line.SetResourceReference(Shape.StrokeProperty, brushResource);

                foreach (var point in segment.Points)
                    line.Points.Add(ToCanvasPoint(point, start, end, minValue, maxValue, padding, plotWidth, plotHeight));

                FpsGraphCanvas.Children.Add(line);
            }

            foreach (var point in segment.Points)
            {
                var canvasPoint = ToCanvasPoint(point, start, end, minValue, maxValue, padding, plotWidth, plotHeight);
                var dot = new Ellipse
                {
                    Width = dashed ? 5 : 6,
                    Height = dashed ? 5 : 6,
                    Opacity = dashed ? 0.82 : 1.0,
                    ToolTip = $"{seriesLabel} · {point.Timestamp.ToLocalTime():HH:mm:ss} · {point.Value:0.0} FPS · {point.DataQuality}"
                };
                dot.SetResourceReference(Shape.FillProperty, brushResource);
                Canvas.SetLeft(dot, canvasPoint.X - dot.Width / 2);
                Canvas.SetTop(dot, canvasPoint.Y - dot.Height / 2);
                FpsGraphCanvas.Children.Add(dot);
            }
        }
    }

    private static (double Min, double Max) ValueRange(IReadOnlyList<PerformanceGraphPoint> points)
    {
        var minValue = points.Min(point => point.Value);
        var maxValue = points.Max(point => point.Value);
        if (Math.Abs(maxValue - minValue) < 0.0001)
        {
            var pad = Math.Max(1, Math.Abs(maxValue) * 0.05);
            minValue -= pad;
            maxValue += pad;
        }
        return (minValue, maxValue);
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

    private static string FormatRange(PerformanceIntervalSummary interval)
        => $"{interval.Start.ToLocalTime():HH:mm:ss} — {interval.End.ToLocalTime():HH:mm:ss}";

    private static string QualityLabel(PerformanceEvidenceQuality quality)
        => quality switch
        {
            PerformanceEvidenceQuality.Measured => "Medida",
            PerformanceEvidenceQuality.Partial => "Parcial",
            _ => "Indisponível"
        };

    private static string FormatMetric(double? value, string format, string suffix)
        => value is double number && double.IsFinite(number)
            ? number.ToString(format) + suffix
            : "—";
}
