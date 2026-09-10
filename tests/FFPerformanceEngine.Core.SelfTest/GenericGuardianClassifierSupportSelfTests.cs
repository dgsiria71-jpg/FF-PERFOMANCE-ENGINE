using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianClassifierSupportSelfTests
{
    public static void Run()
    {
        CoversEveryTaxonomyValueExactlyOnce();
        UnknownIsFallbackNotHealthy();
        EvidenceBackedSetIsExact();
        UnavailableEvidenceSetIsExact();
        CanClassifyMatchesEvidenceAuthority();
        ForReturnsCanonicalDescriptor();
        CatalogCollectionIsReadOnly();
        ReadingCatalogDoesNotRewriteClassifierEvidence();
        Console.WriteLine("PASS Track 6 Guardian classifier support/availability contract");
    }

    private static void CoversEveryTaxonomyValueExactlyOnce()
    {
        var all = GenericGuardianClassifierSupportCatalog.All;
        var enumValues = Enum.GetValues<GuardianAnomalyKind>();

        Require(all.Count == enumValues.Length,
            "Classifier support catalog must contain exactly one descriptor for every approved Guardian anomaly enum value.");
        foreach (var family in enumValues)
        {
            Require(all.Count(item => item.Family == family) == 1,
                $"Guardian anomaly family {family} must appear exactly once in the support catalog.");
        }
        Require(all.All(item => !string.IsNullOrWhiteSpace(item.Reason)),
            "Every Guardian anomaly support descriptor must explain its current authority honestly.");
    }

    private static void UnknownIsFallbackNotHealthy()
    {
        var unknown = GenericGuardianClassifierSupportCatalog.For(GuardianAnomalyKind.Unknown);

        Require(unknown.Support == GuardianAnomalySupportLevel.Fallback
                && !unknown.CanClassify,
            "Unknown must be a non-classifying fallback rather than evidence-backed causality.");
        Require(unknown.Reason.Contains("healthy", StringComparison.OrdinalIgnoreCase)
                && unknown.Reason.Contains("causal", StringComparison.OrdinalIgnoreCase),
            "Unknown support reason must explicitly prevent callers from interpreting it as proven healthy/causal state.");
    }

    private static void EvidenceBackedSetIsExact()
    {
        var expected = new HashSet<GuardianAnomalyKind>
        {
            GuardianAnomalyKind.CpuContention,
            GuardianAnomalyKind.GpuSaturation,
            GuardianAnomalyKind.MemoryPressure,
            GuardianAnomalyKind.VramPressure,
            GuardianAnomalyKind.FrameTimeInstability,
            GuardianAnomalyKind.ThermalThrottling,
            GuardianAnomalyKind.NetworkInstability
        };
        var actual = GenericGuardianClassifierSupportCatalog.All
            .Where(item => item.Support == GuardianAnomalySupportLevel.EvidenceBacked)
            .Select(item => item.Family)
            .ToHashSet();

        Require(actual.SetEquals(expected) && actual.Count == 7,
            "Only the seven currently typed-analyzer-backed Guardian anomaly families may advertise EvidenceBacked support.");
    }

    private static void UnavailableEvidenceSetIsExact()
    {
        var expected = new HashSet<GuardianAnomalyKind>
        {
            GuardianAnomalyKind.BackgroundLoad,
            GuardianAnomalyKind.RendererEngineStall,
            GuardianAnomalyKind.SchedulerImbalance,
            GuardianAnomalyKind.InputFrameLatencySpike
        };
        var actual = GenericGuardianClassifierSupportCatalog.All
            .Where(item => item.Support == GuardianAnomalySupportLevel.UnavailableEvidence)
            .Select(item => item.Family)
            .ToHashSet();

        Require(actual.SetEquals(expected) && actual.Count == 4,
            "Unsupported causal families must remain explicitly UnavailableEvidence rather than being guessed from unrelated telemetry.");
        Require(GenericGuardianClassifierSupportCatalog.All
            .Where(item => expected.Contains(item.Family))
            .All(item => item.Reason.Contains("evidence", StringComparison.OrdinalIgnoreCase)),
            "Unavailable families must explain that dedicated causal evidence is missing.");
    }

    private static void CanClassifyMatchesEvidenceAuthority()
    {
        foreach (var item in GenericGuardianClassifierSupportCatalog.All)
        {
            Require(item.CanClassify == (item.Support == GuardianAnomalySupportLevel.EvidenceBacked),
                $"CanClassify for {item.Family} must derive only from EvidenceBacked support.");
        }
    }

    private static void ForReturnsCanonicalDescriptor()
    {
        foreach (var item in GenericGuardianClassifierSupportCatalog.All)
        {
            Require(ReferenceEquals(item, GenericGuardianClassifierSupportCatalog.For(item.Family)),
                $"For({item.Family}) must return the canonical descriptor already exposed by All.");
        }
    }

    private static void CatalogCollectionIsReadOnly()
    {
        var all = GenericGuardianClassifierSupportCatalog.All;
        Require(all is IList<GuardianAnomalySupportDescriptor> list && list.IsReadOnly,
            "Classifier support catalog must expose a genuinely read-only collection.");

        var mutationBlocked = false;
        try
        {
            ((IList<GuardianAnomalySupportDescriptor>)all).Add(new GuardianAnomalySupportDescriptor
            {
                Family = GuardianAnomalyKind.Unknown,
                Support = GuardianAnomalySupportLevel.Fallback,
                Reason = "mutation fixture"
            });
        }
        catch (NotSupportedException)
        {
            mutationBlocked = true;
        }

        Require(mutationBlocked,
            "Ordinary collection mutation must be rejected by the static Guardian classifier support catalog.");
    }

    private static void ReadingCatalogDoesNotRewriteClassifierEvidence()
    {
        var observation = new GenericGuardianWorkloadObservation
        {
            State = new GuardianWorkloadStateSnapshot
            {
                State = GuardianWorkloadState.Active,
                Confidence = GuardianWorkloadStateConfidence.High,
                Target = new TelemetryWorkloadTarget
                {
                    GameId = "fixture:support",
                    ProcessId = 6262,
                    ExecutablePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Support", "game.exe")),
                    BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
                }
            },
            Signals = new GenericGuardianWorkloadSignals
            {
                SystemOnline = true,
                IsForeground = true,
                HasRecentInput = true,
                HasRenderActivity = true
            },
            Frame = new TelemetryFrame(
                new DateTimeOffset(2026, 9, 10, 21, 0, 0, TimeSpan.Zero),
                [new TelemetryMetricObservation(
                    TelemetryStandardMetrics.FrameFpsAverage,
                    80,
                    TelemetryMetricQuality.Measured,
                    1,
                    "guardian-support-selftest",
                    TelemetryMetricOrigin.Direct)])
        };
        var analysis = new BottleneckAnalysisResult
        {
            Primary = BottleneckKind.Unknown,
            Confidence = 0,
            Candidates = Array.Empty<BottleneckCandidate>(),
            Signals = Array.Empty<string>()
        };
        var classification = new GenericGuardianBottleneckClassification
        {
            Observation = observation,
            Analysis = analysis,
            Reason = "fixture"
        };

        _ = GenericGuardianClassifierSupportCatalog.All;
        _ = GenericGuardianClassifierSupportCatalog.For(GuardianAnomalyKind.CpuContention);

        Require(ReferenceEquals(classification.Observation, observation)
                && ReferenceEquals(classification.Analysis, analysis)
                && classification.Family == GuardianAnomalyKind.Unknown,
            "Reading the support catalog must not mutate or upgrade existing classifier evidence.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
