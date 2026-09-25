using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class PerformanceHistorySelfTests
{
    public static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-performance-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var historyPath = Path.Combine(tempRoot, "history.json");
            var start = new DateTimeOffset(2026, 9, 6, 3, 0, 0, TimeSpan.Zero);
            var configuration = TestConfiguration();
            var first = Comparison(
                "A · Sessão 1", 100, 10.0, 14.0,
                "B · Sessão 1", 120, 8.2, 11.0,
                start,
                configuration);
            var second = Comparison(
                "A · Sessão 2", 118, 8.4, 11.5,
                "B · Sessão 2", 140, 7.1, 9.0,
                start.AddMinutes(10),
                configuration);

            var history = new HistoryService(historyPath);
            var historyType = typeof(HistoryService);
            var save = historyType.GetMethod("SavePerformanceComparisonAsync", BindingFlags.Public | BindingFlags.Instance);
            Require(save is not null,
                "History must expose SavePerformanceComparisonAsync so A/B evidence survives the current process.");

            var firstRecord = await InvokeTaskResultAsync(save!, history, "Sessão 1", first, CancellationToken.None);
            var secondRecord = await InvokeTaskResultAsync(save!, history, "Sessão 2", second, CancellationToken.None);
            Require(firstRecord is not null && secondRecord is not null,
                "Saving historical A/B evidence must return reusable typed records.");

            var load = historyType.GetMethod("LoadPerformanceComparisonsAsync", BindingFlags.Public | BindingFlags.Instance);
            Require(load is not null,
                "History must expose LoadPerformanceComparisonsAsync for cross-session reuse.");

            var reopened = new HistoryService(historyPath);
            var loadedObject = await InvokeTaskResultAsync(load!, reopened, CancellationToken.None);
            var loaded = ((System.Collections.IEnumerable)loadedObject!).Cast<object>().ToArray();
            Require(loaded.Length == 2,
                "Historical A/B comparisons must round-trip through a fresh HistoryService instance.");

            var latest = loaded.OrderByDescending(x => Read<DateTimeOffset>(x, "SavedAt")).First();
            var earliest = loaded.OrderBy(x => Read<DateTimeOffset>(x, "SavedAt")).First();
            Require(Read<string>(earliest, "Label") == "Sessão 1"
                    && Read<string>(latest, "Label") == "Sessão 2",
                "Historical comparisons must preserve stable user-facing labels and save times.");
            Require(Read<object>(earliest, "ValidationStatus").ToString() == "Observed"
                    && !Read<bool>(earliest, "CanOriginateProfile"),
                "A saved comparison must remain Observed and ineligible to originate a profile before explicit validation.");

            var candidate1 = Read<PerformanceEvidenceSnapshot>(earliest, "Candidate");
            var candidate2 = Read<PerformanceEvidenceSnapshot>(latest, "Candidate");
            Require(candidate1.Configuration?.Environment.Id == configuration.Environment.Id
                    && candidate2.Configuration?.Environment.Id == configuration.Environment.Id,
                "Historical A/B persistence must preserve the exact candidate configuration and environment fingerprint.");

            var session = new PerformanceComparisonSession();
            var snapshotBaseline = typeof(PerformanceComparisonSession).GetMethod(
                "SetBaseline",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(PerformanceEvidenceSnapshot)],
                modifiers: null);
            var snapshotCandidate = typeof(PerformanceComparisonSession).GetMethod(
                "SetCandidate",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: [typeof(PerformanceEvidenceSnapshot)],
                modifiers: null);
            Require(snapshotBaseline is not null && snapshotCandidate is not null,
                "PerformanceComparisonSession must accept historical evidence snapshots directly for session-to-session comparison.");
            snapshotBaseline!.Invoke(session, [candidate1]);
            snapshotCandidate!.Invoke(session, [candidate2]);
            Require(Math.Abs((session.CurrentComparison!.Metrics.AverageFpsDelta ?? 0) - 20.0) < 0.001,
                "Cross-session comparison must use the persisted measured candidates without inventing missing telemetry.");

            var requestValidation = historyType.GetMethod("RequestPerformanceValidationAsync", BindingFlags.Public | BindingFlags.Instance);
            var completeValidation = historyType.GetMethod("CompletePerformanceValidationAsync", BindingFlags.Public | BindingFlags.Instance);
            Require(requestValidation is not null && completeValidation is not null,
                "History must provide an explicit validation transition for a historical candidate.");

            var recordId = Read<Guid>(earliest, "Id");
            var pending = await InvokeTaskResultAsync(requestValidation!, reopened, recordId, CancellationToken.None);
            Require(Read<object>(pending!, "ValidationStatus").ToString() == "PendingValidation"
                    && !Read<bool>(pending!, "CanOriginateProfile"),
                "Requesting validation must not promote observed evidence by itself.");

            var partialValidation = Snapshot(
                "Validação parcial",
                122,
                8.0,
                null,
                start.AddMinutes(20),
                "Partial",
                configuration);
            var partialRejected = false;
            try
            {
                await InvokeTaskResultAsync(completeValidation!, reopened, recordId, partialValidation, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                partialRejected = true;
            }
            Require(partialRejected,
                "Partial telemetry must be rejected as validation evidence instead of being promoted to Validated.");

            var measuredValidation = Snapshot(
                "Validação medida",
                121,
                8.1,
                10.8,
                start.AddMinutes(21),
                "Measured",
                configuration);
            var validated = await InvokeTaskResultAsync(
                completeValidation!, reopened, recordId, measuredValidation, CancellationToken.None);
            Require(Read<object>(validated!, "ValidationStatus").ToString() == "Validated"
                    && Read<bool>(validated!, "CanOriginateProfile"),
                "Only a separate fully measured validation capture under the same exact configuration may make the candidate eligible to originate a profile.");

            var bridge = typeof(PerformanceProfileEvidenceBridge).GetMethod(
                "FromValidatedRecord",
                BindingFlags.Public | BindingFlags.Static);
            Require(bridge is not null,
                "Profiles integration must consume validated historical records through an explicit bridge.");
            var validatedProjection = bridge!.Invoke(null, [validated]);
            Require(validatedProjection is not null
                    && Read<EvidenceLevel>(validatedProjection, "Evidence") == EvidenceLevel.Validated,
                "A validated historical record must project Validated evidence only after the explicit validation transition.");

            Console.WriteLine("PASS Performance historical A/B persistence, cross-session reuse, exact configuration, and explicit validation gate");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static PerformanceConfigurationSnapshot TestConfiguration()
    {
        var instance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 6,
            RamMb = 6144,
            Renderer = "Vulkan",
            Fps = 120,
            Resolution = "1920x1080",
            Dpi = 320
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "FFPE-HISTORY-TEST",
            WindowsDescription = "Windows 11 test",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [instance],
            ActiveGame = GameKind.FreeFireMax
        };
        return PerformanceConfigurationSnapshot.Capture(environment, instance, GameKind.FreeFireMax)
            ?? throw new InvalidOperationException("Test configuration must be complete.");
    }

    private static PerformanceABComparison Comparison(
        string baselineName,
        double baselineFps,
        double baselineFrameTime,
        double baselineLatency,
        string candidateName,
        double candidateFps,
        double candidateFrameTime,
        double candidateLatency,
        DateTimeOffset start,
        PerformanceConfigurationSnapshot configuration)
        => PerformanceABComparison.Create(
            Snapshot(baselineName, baselineFps, baselineFrameTime, baselineLatency, start, "Measured"),
            Snapshot(candidateName, candidateFps, candidateFrameTime, candidateLatency, start.AddMinutes(1), "Measured", configuration));

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        double? fps,
        double? frameTime,
        double? latency,
        DateTimeOffset start,
        string quality,
        PerformanceConfigurationSnapshot? configuration = null)
    {
        var points = new[]
        {
            new PerformanceTimelinePoint
            {
                Timestamp = start,
                Fps = fps,
                FrameTimeMs = frameTime,
                LatencyMs = latency,
                DataQuality = quality
            },
            new PerformanceTimelinePoint
            {
                Timestamp = start.AddSeconds(1),
                Fps = fps,
                FrameTimeMs = frameTime,
                LatencyMs = latency,
                DataQuality = quality
            }
        };
        var interval = new PerformanceIntervalSummary
        {
            Start = start,
            End = start.AddSeconds(1),
            TelemetrySamples = points.Length,
            FpsEvidenceSamples = points.Count(point => point.Fps is not null),
            AverageFps = fps,
            AverageFrameTimeMs = frameTime,
            Points = points
        };
        return configuration is null
            ? PerformanceEvidenceSnapshot.Capture(name, interval, start.AddSeconds(2))
            : PerformanceEvidenceSnapshot.Capture(name, interval, start.AddSeconds(2), configuration);
    }

    private static async Task<object?> InvokeTaskResultAsync(MethodInfo method, object? target, params object?[] args)
    {
        object? invocation;
        try
        {
            invocation = method.Invoke(target, args);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }

        Require(invocation is Task, $"{method.Name} must return Task or Task<T>.");
        try
        {
            await ((Task)invocation!).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not InvalidOperationException && exception.InnerException is InvalidOperationException inner)
        {
            throw inner;
        }

        return invocation!.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(invocation);
    }

    private static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Require(property is not null, $"Expected public property {propertyName} on {instance.GetType().Name}.");
        return (T)property!.GetValue(instance)!;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
