using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Telemetry;
using FFPerformanceEngine.Core.Workloads;

internal static class GenericGuardianWorkloadObservationSelfTests
{
    public static async Task RunAsync()
    {
        ConstructorIsSideEffectFree();
        await ExactForegroundRecentInputAndDirectRenderBecomeActiveAsync();
        await BackgroundInputCannotBecomeActiveAsync();
        await MissingTrustworthyRenderStaysReadyAsync();
        await NonExactTargetsDoNotProbeAsync();
        await MismatchedCaptureIsRejectedAsync();
        await OnlyDirectMeasuredPositiveAcceptedFramesCountAsync();
        await OfflineSkipsExternalProbesAsync();
        Console.WriteLine("PASS Track 6 generic Guardian workload observation bridge");
    }

    private static void ConstructorIsSideEffectFree()
    {
        var foreground = new CountingForegroundProbe(77);
        var input = new CountingRecentInputProbe(true);
        var captureCalls = 0;
        var service = CreateService(foreground, input, (_, _, _) =>
        {
            captureCalls++;
            return Task.FromResult(new PerformanceWorkloadTypedCaptureResult { Target = TelemetryWorkloadTarget.SystemOnly });
        });

        Require(service is not null && foreground.Calls == 0 && input.Calls == 0 && captureCalls == 0,
            "Constructing the generic workload observation service must not perform discovery/probing/capture side effects.");
        Require(typeof(IForegroundProcessProbe).IsAssignableFrom(typeof(WindowsForegroundProcessProbe)),
            "WindowsForegroundProcessProbe must implement the neutral exact foreground-PID contract.");
    }

    private static async Task ExactForegroundRecentInputAndDirectRenderBecomeActiveAsync()
    {
        var executable = FullPath("game.exe");
        var catalog = Catalog("steam:100", 77, executable);
        var foreground = new CountingForegroundProbe(77);
        var input = new CountingRecentInputProbe(true);
        var frame = Frame(1, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Direct);
        var service = CreateService(foreground, input, Capture(frame));

        var first = await service.ObserveAsync(catalog, "steam:100", true, TimeSpan.FromSeconds(2));
        var second = await service.ObserveAsync(catalog, "steam:100", true, TimeSpan.FromSeconds(2));

        Require(first.State.State == GuardianWorkloadState.Starting,
            "The first exact workload observation must preserve the state-machine Starting transition.");
        Require(second.State.State == GuardianWorkloadState.Active
                && second.Signals.IsForeground
                && second.Signals.HasRecentInput
                && second.Signals.HasRenderActivity
                && ReferenceEquals(second.Frame, frame),
            "Exact foreground + recent input + direct measured render evidence must become Active and preserve the accepted frame.");
        Require(foreground.Calls == 2 && input.Calls == 2,
            "Foreground and recent-input probes must be invoked once per exact foreground observation.");
    }

    private static async Task BackgroundInputCannotBecomeActiveAsync()
    {
        var executable = FullPath("background.exe");
        var catalog = Catalog("steam:200", 88, executable);
        var foreground = new CountingForegroundProbe(999);
        var input = new CountingRecentInputProbe(true);
        var frame = Frame(1, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Direct);
        var service = CreateService(foreground, input, Capture(frame));

        _ = await service.ObserveAsync(catalog, "steam:200", true, TimeSpan.FromSeconds(2));
        var second = await service.ObserveAsync(catalog, "steam:200", true, TimeSpan.FromSeconds(2));

        Require(second.State.State == GuardianWorkloadState.Ready
                && !second.Signals.IsForeground
                && !second.Signals.HasRecentInput
                && second.Signals.HasRenderActivity,
            "Global recent input must not be attributed to a background workload PID.");
        Require(input.Calls == 0,
            "Recent-input probe must not be called when the exact workload PID is not foreground.");
    }

    private static async Task MissingTrustworthyRenderStaysReadyAsync()
    {
        var executable = FullPath("no-render.exe");
        var catalog = Catalog("steam:300", 99, executable);
        var service = CreateService(
            new CountingForegroundProbe(99),
            new CountingRecentInputProbe(true),
            Capture(null));

        _ = await service.ObserveAsync(catalog, "steam:300", true, TimeSpan.FromSeconds(2));
        var second = await service.ObserveAsync(catalog, "steam:300", true, TimeSpan.FromSeconds(2));

        Require(second.State.State == GuardianWorkloadState.Ready
                && !second.Signals.HasRenderActivity
                && second.Frame is null,
            "Foreground/input without trustworthy render evidence must remain Ready rather than fabricate Active.");
    }

    private static async Task NonExactTargetsDoNotProbeAsync()
    {
        var foreground = new CountingForegroundProbe(1);
        var input = new CountingRecentInputProbe(true);
        var captureCalls = 0;
        var service = CreateService(foreground, input, (target, _, _) =>
        {
            captureCalls++;
            return Task.FromResult(new PerformanceWorkloadTypedCaptureResult { Target = target });
        });

        var unknown = await service.ObserveAsync(new ResolvedGameCatalogResult(), "missing", true, TimeSpan.FromSeconds(2));
        var unavailable = await service.ObserveAsync(CatalogWithoutProcess("steam:400"), "steam:400", true, TimeSpan.FromSeconds(2));
        var ambiguous = await service.ObserveAsync(CatalogAmbiguous("steam:500", FullPath("ambiguous.exe")), "steam:500", true, TimeSpan.FromSeconds(2));

        Require(unknown.State.State == GuardianWorkloadState.Unresolved
                && unavailable.State.State == GuardianWorkloadState.Desktop
                && ambiguous.State.State == GuardianWorkloadState.Unresolved,
            "Unknown/ambiguous/unavailable targets must preserve the fail-closed generic state-machine outcomes.");
        Require(foreground.Calls == 0 && input.Calls == 0 && captureCalls == 0,
            "Unknown/ambiguous/unavailable targets must not invoke foreground, input or telemetry probes.");
    }

    private static async Task MismatchedCaptureIsRejectedAsync()
    {
        var executable = FullPath("mismatch.exe");
        var catalog = Catalog("steam:600", 606, executable);
        var frame = Frame(1, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Direct);
        var service = CreateService(
            new CountingForegroundProbe(606),
            new CountingRecentInputProbe(true),
            (target, _, _) => Task.FromResult(new PerformanceWorkloadTypedCaptureResult
            {
                Target = target with { ProcessId = 999 },
                Frame = frame
            }));

        _ = await service.ObserveAsync(catalog, "steam:600", true, TimeSpan.FromSeconds(2));
        var second = await service.ObserveAsync(catalog, "steam:600", true, TimeSpan.FromSeconds(2));

        Require(second.State.State == GuardianWorkloadState.Ready
                && !second.Signals.HasRenderActivity
                && second.Frame is null,
            "A typed capture returned for a different GameId/PID/path target must be rejected as render evidence.");
    }

    private static async Task OnlyDirectMeasuredPositiveAcceptedFramesCountAsync()
    {
        await RequireUntrustedFrameRejectedAsync(Frame(1, TelemetryMetricQuality.Partial, TelemetryMetricOrigin.Direct), "partial");
        await RequireUntrustedFrameRejectedAsync(Frame(1, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Derived), "derived");
        await RequireUntrustedFrameRejectedAsync(Frame(1, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Legacy), "legacy");
        await RequireUntrustedFrameRejectedAsync(Frame(0, TelemetryMetricQuality.Measured, TelemetryMetricOrigin.Direct), "zero-count");
    }

    private static async Task RequireUntrustedFrameRejectedAsync(TelemetryFrame frame, string label)
    {
        var executable = FullPath(label + ".exe");
        var gameId = "steam:" + Math.Abs(label.GetHashCode()).ToString();
        var service = CreateService(
            new CountingForegroundProbe(707),
            new CountingRecentInputProbe(true),
            Capture(frame));
        var catalog = Catalog(gameId, 707, executable);

        _ = await service.ObserveAsync(catalog, gameId, true, TimeSpan.FromSeconds(2));
        var second = await service.ObserveAsync(catalog, gameId, true, TimeSpan.FromSeconds(2));

        Require(second.State.State == GuardianWorkloadState.Ready
                && !second.Signals.HasRenderActivity
                && second.Frame is null,
            $"{label} accepted-frame telemetry must not be promoted to trustworthy generic render activity.");
    }

    private static async Task OfflineSkipsExternalProbesAsync()
    {
        var executable = FullPath("offline.exe");
        var foreground = new CountingForegroundProbe(808);
        var input = new CountingRecentInputProbe(true);
        var captureCalls = 0;
        var service = CreateService(foreground, input, (target, _, _) =>
        {
            captureCalls++;
            return Task.FromResult(new PerformanceWorkloadTypedCaptureResult { Target = target });
        });

        var offline = await service.ObserveAsync(Catalog("steam:800", 808, executable), "steam:800", false, TimeSpan.FromSeconds(2));

        Require(offline.State.State == GuardianWorkloadState.Offline
                && !offline.Signals.SystemOnline
                && offline.Frame is null,
            "Explicit offline state must flow through the generic state machine without manufacturing runtime evidence.");
        Require(foreground.Calls == 0 && input.Calls == 0 && captureCalls == 0,
            "Offline observation must not invoke foreground, input or telemetry probes.");
    }

    private static GenericGuardianWorkloadObservationService CreateService(
        IForegroundProcessProbe foreground,
        IRecentInputProbe input,
        Func<TelemetryWorkloadTarget, TimeSpan, CancellationToken, Task<PerformanceWorkloadTypedCaptureResult>> capture)
    {
        var resolver = new TelemetryWorkloadTargetResolver();
        return new GenericGuardianWorkloadObservationService(
            resolver,
            new GenericGuardianWorkloadStateMachine(resolver),
            foreground,
            input,
            capture);
    }

    private static Func<TelemetryWorkloadTarget, TimeSpan, CancellationToken, Task<PerformanceWorkloadTypedCaptureResult>> Capture(TelemetryFrame? frame)
        => (target, _, _) => Task.FromResult(new PerformanceWorkloadTypedCaptureResult
        {
            Target = target,
            Frame = frame
        });

    private static TelemetryFrame Frame(double acceptedCount, TelemetryMetricQuality quality, TelemetryMetricOrigin origin)
        => new(new DateTimeOffset(2026, 9, 10, 20, 0, 0, TimeSpan.Zero),
        [
            new TelemetryMetricObservation(
                TelemetryStandardMetrics.FrameAcceptedSampleCount,
                acceptedCount,
                quality,
                1,
                "fixture",
                origin)
        ]);

    private static ResolvedGameCatalogResult Catalog(string gameId, int processId, string executablePath)
    {
        var identity = Identity(gameId);
        return new ResolvedGameCatalogResult
        {
            Games = [new ResolvedGameCatalogEntry { Identity = identity, Adapter = new GenericGameAdapter() }],
            BoundEvidence = [Bound(gameId, processId, executablePath)]
        };
    }

    private static ResolvedGameCatalogResult CatalogWithoutProcess(string gameId)
    {
        var identity = Identity(gameId);
        return new ResolvedGameCatalogResult
        {
            Games = [new ResolvedGameCatalogEntry { Identity = identity, Adapter = new GenericGameAdapter() }]
        };
    }

    private static ResolvedGameCatalogResult CatalogAmbiguous(string gameId, string executablePath)
    {
        var identity = Identity(gameId);
        return new ResolvedGameCatalogResult
        {
            Games = [new ResolvedGameCatalogEntry { Identity = identity, Adapter = new GenericGameAdapter() }],
            BoundEvidence = [Bound(gameId, 1, executablePath), Bound(gameId, 2, executablePath)]
        };
    }

    private static GameIdentity Identity(string gameId) => new()
    {
        GameId = gameId,
        Name = gameId,
        AdapterId = "generic"
    };

    private static BoundGameEvidence Bound(string gameId, int processId, string executablePath)
        => new()
        {
            GameId = gameId,
            BindingReason = GameEvidenceBindingReason.ExactGameIdHint,
            SourceId = "windows-running-process",
            Priority = 40,
            Observation = new GameEvidenceObservation
            {
                ObservationId = $"running:{gameId}:{processId}",
                Kind = GameEvidenceKind.RunningProcess,
                Confidence = 1,
                ObservedAtUtc = new DateTimeOffset(2026, 9, 10, 20, 0, 0, TimeSpan.Zero),
                GameIdHint = gameId,
                ProcessId = processId,
                ExecutablePath = executablePath,
                EvidenceText = "fixture"
            }
        };

    private static string FullPath(string fileName)
        => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DG-Guardian-Track6-Observation", fileName));

    private sealed class CountingForegroundProbe(int? processId) : IForegroundProcessProbe
    {
        public int Calls { get; private set; }
        public int? GetForegroundProcessId()
        {
            Calls++;
            return processId;
        }
    }

    private sealed class CountingRecentInputProbe(bool hasRecentInput) : IRecentInputProbe
    {
        public int Calls { get; private set; }
        public bool HasRecentInput()
        {
            Calls++;
            return hasRecentInput;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
