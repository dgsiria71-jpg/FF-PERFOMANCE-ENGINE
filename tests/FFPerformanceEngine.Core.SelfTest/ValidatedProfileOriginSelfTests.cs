using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class ValidatedProfileOriginSelfTests
{
    public static async Task RunAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-profile-origin-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var blueStacks = new BlueStacksService();
            const string configText = """
                bst.instance.Pie64.cpus="6"
                bst.instance.Pie64.ram="6144"
                bst.instance.Pie64.graphics_renderer="Vulkan"
                bst.instance.Pie64.max_fps="120"
                bst.instance.Pie64.fb_width="1600"
                bst.instance.Pie64.fb_height="900"
                bst.instance.Pie64.dpi="320"
                """;
            var instance = blueStacks.ParseConfig(configText).Single();
            var dpiProperty = typeof(BlueStacksInstance).GetProperty("Dpi", BindingFlags.Public | BindingFlags.Instance);
            Require(dpiProperty is not null && (int?)dpiProperty.GetValue(instance) == 320,
                "BlueStacks instance parsing must preserve DPI so profile origin never invents it.");

            var environment = new EnvironmentSnapshot
            {
                MachineName = "FFPE-TEST-PC",
                WindowsDescription = "Windows 11 test",
                LogicalProcessors = 16,
                MemoryTotalGb = 32,
                BlueStacksDetected = true,
                Instances = [instance],
                ActiveGame = GameKind.FreeFireMax
            };

            var core = typeof(ProfileService).Assembly;
            var configurationType = core.GetType("FFPerformanceEngine.Core.Services.PerformanceConfigurationSnapshot");
            Require(configurationType is not null,
                "Core must expose PerformanceConfigurationSnapshot to bind measured B evidence to exact tuning values.");
            var captureConfiguration = configurationType!.GetMethod(
                "Capture",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(EnvironmentSnapshot), typeof(BlueStacksInstance), typeof(GameKind)],
                modifiers: null);
            Require(captureConfiguration is not null,
                "PerformanceConfigurationSnapshot must capture an environment and exact BlueStacks instance configuration.");

            var candidateConfiguration = captureConfiguration!.Invoke(null, [environment, instance, GameKind.FreeFireMax]);
            Require(candidateConfiguration is not null,
                "A complete Free Fire MAX BlueStacks configuration must be capturable without defaults.");
            Require(Read<int>(candidateConfiguration!, "CpuCores") == 6
                    && Read<int>(candidateConfiguration!, "RamMb") == 6144
                    && Read<int>(candidateConfiguration!, "FpsTarget") == 120
                    && Read<int>(candidateConfiguration!, "Dpi") == 320
                    && Read<string>(candidateConfiguration!, "Renderer") == "Vulkan"
                    && Read<string>(candidateConfiguration!, "Resolution") == "1600x900",
                "Captured candidate configuration must preserve CPU, RAM, renderer, FPS, resolution and DPI exactly.");
            Require(!string.IsNullOrWhiteSpace(Read<object>(candidateConfiguration!, "Environment").GetType()
                    .GetProperty("Id")!.GetValue(Read<object>(candidateConfiguration!, "Environment"))?.ToString()),
                "Captured configuration must include a deterministic environment fingerprint id.");

            var start = new DateTimeOffset(2026, 9, 6, 6, 0, 0, TimeSpan.Zero);
            var baseline = Snapshot("A", 100, 10.0, 12.0, start, "Measured");
            var candidate = SnapshotWithConfiguration(
                "B",
                122,
                8.2,
                9.5,
                start.AddMinutes(1),
                "Measured",
                configurationType,
                candidateConfiguration!);
            var comparison = PerformanceABComparison.Create(baseline, candidate);

            var historyPath = Path.Combine(tempRoot, "history.json");
            var history = new HistoryService(historyPath);
            var record = await history.SavePerformanceComparisonAsync("Perfil candidato", comparison);
            Require(!record.CanOriginateProfile,
                "Observed A/B evidence must remain ineligible before explicit measured validation.");
            await history.RequestPerformanceValidationAsync(record.Id);

            var mismatchInstance = instance with { Fps = 90 };
            var mismatchConfiguration = captureConfiguration.Invoke(null, [environment with { Instances = [mismatchInstance] }, mismatchInstance, GameKind.FreeFireMax]);
            var mismatchedValidation = SnapshotWithConfiguration(
                "Validation mismatch",
                121,
                8.3,
                9.6,
                start.AddMinutes(3),
                "Measured",
                configurationType,
                mismatchConfiguration!);
            var mismatchRejected = false;
            try
            {
                await history.CompletePerformanceValidationAsync(record.Id, mismatchedValidation);
            }
            catch (InvalidOperationException)
            {
                mismatchRejected = true;
            }
            Require(mismatchRejected,
                "Validation measured under a different tuning configuration must be rejected.");

            var measuredValidation = SnapshotWithConfiguration(
                "Validation measured",
                123,
                8.1,
                9.2,
                start.AddMinutes(4),
                "Measured",
                configurationType,
                candidateConfiguration!);
            var validated = await history.CompletePerformanceValidationAsync(record.Id, measuredValidation);
            Require(validated.CanOriginateProfile,
                "A later measured validation using the same exact configuration must make the record eligible for explicit profile origin.");

            var profilePath = Path.Combine(tempRoot, "profiles.json");
            var profiles = new ProfileService(profilePath);
            var originMethod = typeof(ProfileService).GetMethod(
                "CreateCustomFromValidatedComparisonAsync",
                BindingFlags.Public | BindingFlags.Instance);
            Require(originMethod is not null,
                "ProfileService must expose an explicit profile-origin operation for validated historical evidence.");

            var incompatibleEnvironment = environment with { MachineName = "OTHER-PC" };
            var incompatibleRejected = false;
            try
            {
                await InvokeTaskResultAsync(originMethod!, profiles, validated, incompatibleEnvironment, "Validated A/B Custom", CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                incompatibleRejected = true;
            }
            Require(incompatibleRejected,
                "A structurally incompatible current environment must not originate a profile from historical evidence.");

            var createdObject = await InvokeTaskResultAsync(
                originMethod!, profiles, validated, environment, "Validated A/B Custom", CancellationToken.None);
            Require(createdObject is PerformanceProfile created,
                "Explicit origin must return the persisted PerformanceProfile.");
            Require(created.Kind == ProfileKind.Custom
                    && created.Evidence == EvidenceLevel.Validated
                    && created.Game == GameKind.FreeFireMax
                    && created.InstanceName == "Pie64"
                    && created.CpuCores == 6
                    && created.RamMb == 6144
                    && created.Renderer == "Vulkan"
                    && created.FpsTarget == 120
                    && created.Resolution == "1600x900"
                    && created.Dpi == 320,
                "Originated custom profile must copy only the exact validated candidate configuration.");
            Require(Read<Guid?>(created, "SourceComparisonId") == validated.Id
                    && !string.IsNullOrWhiteSpace(Read<string>(created, "EnvironmentFingerprint")),
                "Originated profile must retain auditable source comparison and environment fingerprint metadata.");
            Require(created.AverageFps is > 122.9 and < 123.1
                    && created.FrameTimeMs is > 8.0 and < 8.2
                    && created.LatencyMs is > 9.1 and < 9.3,
                "Originated profile metrics must come from the separate validation capture, not the earlier candidate observation.");

            var reopenedProfiles = await new ProfileService(profilePath).LoadAsync();
            Require(reopenedProfiles.Count == 1
                    && reopenedProfiles[0].Id == created.Id
                    && reopenedProfiles[0].Kind == ProfileKind.Custom
                    && reopenedProfiles.All(profile => profile.Kind != ProfileKind.Recommended),
                "Profile origin must persist exactly one Custom profile and never auto-promote it to Recommended/winner.");

            Console.WriteLine("PASS validated A/B configuration binding and explicit custom profile origin");
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static PerformanceEvidenceSnapshot Snapshot(
        string name,
        double? fps,
        double? frameTime,
        double? latency,
        DateTimeOffset start,
        string quality)
        => PerformanceEvidenceSnapshot.Capture(name, Interval(fps, frameTime, latency, start, quality), start.AddSeconds(2));

    private static PerformanceEvidenceSnapshot SnapshotWithConfiguration(
        string name,
        double? fps,
        double? frameTime,
        double? latency,
        DateTimeOffset start,
        string quality,
        Type configurationType,
        object configuration)
    {
        var capture = typeof(PerformanceEvidenceSnapshot).GetMethod(
            "Capture",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(string), typeof(PerformanceIntervalSummary), typeof(DateTimeOffset), configurationType],
            modifiers: null);
        Require(capture is not null,
            "PerformanceEvidenceSnapshot.Capture must accept the exact configuration that produced the evidence.");
        return (PerformanceEvidenceSnapshot)capture!.Invoke(
            null,
            [name, Interval(fps, frameTime, latency, start, quality), start.AddSeconds(2), configuration])!;
    }

    private static PerformanceIntervalSummary Interval(
        double? fps,
        double? frameTime,
        double? latency,
        DateTimeOffset start,
        string quality)
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
        return new PerformanceIntervalSummary
        {
            Start = start,
            End = start.AddSeconds(1),
            TelemetrySamples = points.Length,
            FpsEvidenceSamples = points.Count(point => point.Fps is not null),
            AverageFps = fps,
            AverageFrameTimeMs = frameTime,
            Points = points
        };
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
