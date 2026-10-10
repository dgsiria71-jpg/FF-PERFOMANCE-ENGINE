namespace FFPerformanceEngine.Core.Telemetry;

public sealed record ProcessorPowerObservation(
    uint ProcessorNumber,
    uint MaxMhz,
    uint CurrentMhz,
    uint MhzLimit);

public sealed record ProcessorPowerSnapshot
{
    public DateTimeOffset Timestamp { get; }
    public int ExpectedProcessorCount { get; }
    public IReadOnlyList<ProcessorPowerObservation> Observations { get; }

    public ProcessorPowerSnapshot(
        DateTimeOffset timestamp,
        int expectedProcessorCount,
        IEnumerable<ProcessorPowerObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (expectedProcessorCount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(expectedProcessorCount),
                "Expected processor count cannot be negative.");

        var copied = observations.ToArray();
        if (copied.Any(observation => observation is null))
            throw new ArgumentException(
                "Processor power observations cannot contain null values.",
                nameof(observations));

        var distinctProcessorCount = copied
            .Select(observation => observation.ProcessorNumber)
            .Distinct()
            .Count();
        if (distinctProcessorCount > expectedProcessorCount)
            throw new ArgumentException(
                "Processor power observations cannot describe more distinct processors than the expected processor count.",
                nameof(observations));

        Timestamp = timestamp;
        ExpectedProcessorCount = expectedProcessorCount;
        Observations = Array.AsReadOnly(copied);
    }
}

public interface IProcessorPowerInfoProvider
{
    ProcessorPowerSnapshot? Capture();
}

public sealed class ProcessorPowerTelemetrySource
{
    private const string SourceId = "windows-processor-power";
    private readonly IProcessorPowerInfoProvider _provider;

    public ProcessorPowerTelemetrySource(IProcessorPowerInfoProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    public TelemetryFrame Capture()
    {
        ProcessorPowerSnapshot? snapshot;
        try
        {
            snapshot = _provider.Capture();
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                   or System.Runtime.InteropServices.ExternalException
                                   or DllNotFoundException
                                   or EntryPointNotFoundException)
        {
            snapshot = null;
        }

        if (snapshot is null)
            return new TelemetryFrame(DateTimeOffset.UtcNow, Array.Empty<TelemetryMetricObservation>());

        var accepted = snapshot.Observations
            .GroupBy(observation => observation.ProcessorNumber)
            .Select(group => group.Distinct().ToArray())
            .Where(group => group.Length == 1)
            .Select(group => group[0])
            .Where(observation => observation.MaxMhz > 0
                                  && observation.CurrentMhz > 0
                                  && observation.MhzLimit > 0)
            .OrderBy(observation => observation.ProcessorNumber)
            .ToArray();

        if (snapshot.ExpectedProcessorCount <= 0 || accepted.Length == 0)
            return new TelemetryFrame(snapshot.Timestamp, Array.Empty<TelemetryMetricObservation>());

        var coverage = accepted.Length / (double)snapshot.ExpectedProcessorCount;
        var metrics = new TelemetryMetricObservation[]
        {
            Summary(
                TelemetryStandardMetrics.CpuClockCurrentAverageMhz,
                accepted.Average(observation => observation.CurrentMhz),
                coverage),
            Summary(
                TelemetryStandardMetrics.CpuClockMaximumAverageMhz,
                accepted.Average(observation => observation.MaxMhz),
                coverage),
            Summary(
                TelemetryStandardMetrics.CpuClockLimitMinimumMhz,
                accepted.Min(observation => observation.MhzLimit),
                coverage)
        };

        return new TelemetryFrame(snapshot.Timestamp, metrics);
    }

    private static TelemetryMetricObservation Summary(
        TelemetryMetricDescriptor descriptor,
        double value,
        double coverage)
        => new(
            descriptor,
            value,
            TelemetryMetricQuality.Measured,
            coverage,
            SourceId,
            TelemetryMetricOrigin.Derived);
}
