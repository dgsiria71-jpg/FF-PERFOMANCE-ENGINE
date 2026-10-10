using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;

internal static class GenericGuardianCanaryComparabilitySelfTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 19, 12, 0, 0, TimeSpan.Zero);

    internal static void Run()
    {
        var policy = new GenericGuardianCanaryComparabilityPolicy();
        var before = Window(T, T.AddSeconds(2), T.AddSeconds(1));
        var after = Window(T.AddSeconds(5), T.AddSeconds(7), T.AddSeconds(6), before.SessionEpoch);
        var mutationStart = T.AddSeconds(3);
        var mutationEnd = T.AddSeconds(4);

        Require(policy.Evaluate(before, after, mutationStart, mutationEnd)
                == GenericGuardianCanaryComparability.InScopeOnSuppliedEvidence,
            "Only explicitly matching scene/mode/load/environment, exact lifecycle and ordered uncontaminated windows may be in scope; this alone does not prove real-world source authenticity or benefit.");

        foreach (var changed in new[]
        {
            after with { SessionEpoch = Guid.NewGuid() },
            after with { Target = after.Target with { GameId = "other.game" } },
            after with { Target = after.Target with { ProcessId = 2222 } },
            after with { Target = after.Target with { ExecutablePath = @"C:\Games\other.exe" } },
            after with { Target = after.Target with { BindingQuality = TelemetryWorkloadBindingQuality.UnavailableRunningProcess } },
            after with { SourceId = "unrelated-source" },
            after with { SceneId = "different-scene" },
            after with { ModeId = "different-mode" },
            after with { LoadFingerprint = "different-load" },
            after with { EnvironmentFingerprint = "different-environment" },
            after with { SourceId = " " },
            after with { SceneId = " " },
            after with { ModeId = " " },
            after with { LoadFingerprint = " " },
            after with { EnvironmentFingerprint = " " },
            after with { ControlledBenchmarkActive = true },
            after with { ControlledBenchmarkActive = null },
            after with { WorkloadDriftDetected = true },
            after with { WorkloadDriftDetected = null },
            after with { OtherMutationDetected = true },
            after with { OtherMutationDetected = null },
            after with { Frame = new TelemetryFrame(T.AddSeconds(6), Array.Empty<TelemetryMetricObservation>()) },
            after with { Frame = Frame(T.AddSeconds(8)) },
            after with { StartedAt = T.AddSeconds(8) },
            after with { EndedAt = T.AddSeconds(4) }
        })
        {
            Require(policy.Evaluate(before, changed, mutationStart, mutationEnd)
                    == GenericGuardianCanaryComparability.Inconclusive,
                "Any mismatch, unknown/active interference, missing context or invalid after-window must fail closed.");
        }

        foreach (var changed in new[]
        {
            before with { SessionEpoch = Guid.Empty },
            before with { SceneId = " " },
            before with { ControlledBenchmarkActive = null },
            before with { OtherMutationDetected = true },
            before with { Frame = Frame(T.AddSeconds(-1)) },
            before with { StartedAt = before.EndedAt },
            before with { Target = TelemetryWorkloadTarget.SystemOnly }
        })
        {
            Require(policy.Evaluate(changed, after, mutationStart, mutationEnd)
                    == GenericGuardianCanaryComparability.Inconclusive,
                "Missing, ambiguous, contaminated or temporally invalid BEFORE evidence cannot be compared.");
        }

        Require(policy.Evaluate(before, after, before.EndedAt.AddTicks(-1), mutationEnd)
                == GenericGuardianCanaryComparability.Inconclusive,
            "Mutation overlapping before capture is never comparable.");
        Require(policy.Evaluate(before, after, mutationStart, after.StartedAt.AddTicks(1))
                == GenericGuardianCanaryComparability.Inconclusive,
            "After capture must start only once mutation is completed.");
        Require(policy.Evaluate(before, after, mutationEnd, mutationStart)
                == GenericGuardianCanaryComparability.Inconclusive,
            "Reversed mutation boundary must fail closed.");
        Require(policy.Evaluate(before, after, DateTimeOffset.MinValue, mutationEnd)
                == GenericGuardianCanaryComparability.Inconclusive,
            "Unspecified mutation time cannot make causal evidence.");
        Require(policy.Evaluate(null, after, mutationStart, mutationEnd)
                == GenericGuardianCanaryComparability.Inconclusive
                && policy.Evaluate(before, null, mutationStart, mutationEnd)
                == GenericGuardianCanaryComparability.Inconclusive,
            "Absent before/after context always fails closed.");

        Console.WriteLine("PASS Track 6 Guardian comparability policy: exact lifecycle, scene/load/mode, temporal ordering and contamination fail closed");
    }

    private static GenericGuardianCanaryComparisonWindow Window(
        DateTimeOffset started,
        DateTimeOffset ended,
        DateTimeOffset frameTime,
        Guid? epoch = null)
        => new(
            epoch ?? new Guid("0157e8a3-f519-4ccc-aed9-99eacb27ef11"),
            new TelemetryWorkloadTarget
            {
                GameId = "game.test",
                ProcessId = 1111,
                ExecutablePath = @"C:\Games\test.exe",
                BindingQuality = TelemetryWorkloadBindingQuality.ExactRunningProcess
            },
            "adapter-scene-proof",
            "match-mode", "scene-42", "load-stable", "environment-stable",
            started, ended, Frame(frameTime),
            ControlledBenchmarkActive: false,
            WorkloadDriftDetected: false,
            OtherMutationDetected: false);

    private static TelemetryFrame Frame(DateTimeOffset timestamp)
        => new(timestamp,
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameFpsAverage,
                100,
                TelemetryMetricQuality.Measured,
                1.0,
                "guardian-comparability-selftest",
                TelemetryMetricOrigin.Direct)
        ]);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
