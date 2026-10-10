using FFPerformanceEngine.Core.Diagnostics;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.Workloads;

internal static class UniversalValidatedPerformanceProjectionSelfTests
{
    internal static void Run()
    {
        ProjectsAlreadyAuthorizedValidationToExactlyOneUniversalCandidate();
        BlocksObservedPendingAndNonMeasuredCandidate();
        PreservesLegacyValidatedRecordWithoutUniversalContext();
        RejectsUniversalContextWorkloadAdapterAndEquivalenceMismatch();
        RejectsMissingOrAmbiguousCandidateBinding();

        Console.WriteLine("PASS Track 5 universal validated performance projection preserves specialized validation authority");
    }

    private static void ProjectsAlreadyAuthorizedValidationToExactlyOneUniversalCandidate()
    {
        var fixture = CreateFixture();
        var record = ValidatedRecord(fixture);
        Require(record.CanOriginateProfile,
            "The universal validation seam must begin only after the existing specialized profile-origin gate has already authorized the record.");

        var projection = BlueStacksUniversalValidatedPerformanceBridge.TryProject(record, fixture.CandidateSpace);

        Require(projection is not null,
            "A specialized validated record with equivalent universal candidate/validation context must receive a universal read-only projection.");
        Require(ReferenceEquals(projection!.SpecializedRecord, record),
            "Universal validation projection must retain the exact specialized validated History record instead of copying or recreating validation authority.");
        Require(ReferenceEquals(projection.UniversalCandidate, fixture.Binding.UniversalCandidate),
            "Universal validation projection must retain the exact Slice 3 UniversalTuningCandidate binding for the validated candidate configuration.");
    }

    private static void BlocksObservedPendingAndNonMeasuredCandidate()
    {
        var fixture = CreateFixture();
        var validated = ValidatedRecord(fixture);

        var observed = validated with
        {
            ValidationStatus = PerformanceComparisonValidationStatus.Observed,
            ValidationEvidence = null,
            ValidatedAt = null
        };
        Require(!observed.CanOriginateProfile,
            "Observed specialized history must remain blocked by the existing profile-origin authority gate.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(observed, fixture.CandidateSpace) is null,
            "Universal metadata must never upgrade Observed evidence into a validation projection.");

        var pending = validated with
        {
            ValidationStatus = PerformanceComparisonValidationStatus.PendingValidation,
            ValidationEvidence = null,
            ValidatedAt = null
        };
        Require(!pending.CanOriginateProfile,
            "PendingValidation specialized history must remain blocked by the existing profile-origin authority gate.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(pending, fixture.CandidateSpace) is null,
            "Universal metadata must never complete PendingValidation by itself.");

        var partialCandidate = PerformanceEvidenceSnapshot.Capture(
            "B · partial",
            Interval(validated.Candidate.CapturedAt, measured: false),
            validated.Candidate.CapturedAt,
            fixture.Configuration,
            fixture.UniversalContext);
        var specializedGateStillTrue = validated with { Candidate = partialCandidate };
        Require(specializedGateStillTrue.CanOriginateProfile,
            "This adversarial fixture intentionally proves the legacy CanOriginateProfile property does not itself inspect candidate measurement quality.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(specializedGateStillTrue, fixture.CandidateSpace) is null,
            "The additive universal validation projection must still require Candidate and Validation to be fully Measured without changing the legacy gate.");
    }

    private static void PreservesLegacyValidatedRecordWithoutUniversalContext()
    {
        var fixture = CreateFixture();
        var timestamp = fixture.Timestamp;
        var baseline = PerformanceEvidenceSnapshot.Capture(
            "A · legacy",
            Interval(timestamp, measured: true),
            timestamp,
            fixture.Configuration);
        var candidate = PerformanceEvidenceSnapshot.Capture(
            "B · legacy",
            Interval(timestamp.AddMinutes(1), measured: true),
            timestamp.AddMinutes(1),
            fixture.Configuration);
        var validation = PerformanceEvidenceSnapshot.Capture(
            "Validation · legacy",
            Interval(timestamp.AddMinutes(2), measured: true),
            timestamp.AddMinutes(2),
            fixture.Configuration);
        var legacy = new PerformanceComparisonHistoryRecord
        {
            Label = "Legacy validated specialized record",
            SavedAt = timestamp.AddMinutes(1),
            Baseline = baseline,
            Candidate = candidate,
            ValidationStatus = PerformanceComparisonValidationStatus.Validated,
            ValidationEvidence = validation,
            ValidatedAt = timestamp.AddMinutes(2)
        };

        Require(legacy.CanOriginateProfile,
            "Legacy History without UniversalContext must preserve the existing specialized CanOriginateProfile behavior.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(legacy, fixture.CandidateSpace) is null,
            "Legacy History without UniversalContext must simply receive no universal projection; no context may be retroactively invented.");
        Require(legacy.CanOriginateProfile,
            "Attempting universal projection must be read-only and must not invalidate the specialized legacy record.");
    }

    private static void RejectsUniversalContextWorkloadAdapterAndEquivalenceMismatch()
    {
        var fixture = CreateFixture();
        var validated = ValidatedRecord(fixture);

        var differentMachineContext = UniversalContext(
            fixture.CandidateSpace,
            machineFingerprintId: "different-machine-fingerprint");
        var contextMismatchValidation = PerformanceEvidenceSnapshot.Capture(
            "Validation · context mismatch",
            Interval(fixture.Timestamp.AddMinutes(2), measured: true),
            fixture.Timestamp.AddMinutes(2),
            fixture.Configuration,
            differentMachineContext);
        var contextMismatch = validated with { ValidationEvidence = contextMismatchValidation };
        Require(contextMismatch.CanOriginateProfile,
            "Universal context mismatch must not rewrite the existing specialized configuration/profile-origin authority.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(contextMismatch, fixture.CandidateSpace) is null,
            "Candidate and validation universal contexts must be exactly equivalent before a universal projection can exist.");

        var wrongAdapterContext = fixture.UniversalContext with { AdapterId = "tampered-adapter" };
        var adapterMismatchCandidate = PerformanceEvidenceSnapshot.Capture(
            "B · wrong adapter",
            Interval(fixture.Timestamp.AddMinutes(1), measured: true),
            fixture.Timestamp.AddMinutes(1),
            fixture.Configuration,
            wrongAdapterContext);
        var adapterMismatchValidation = PerformanceEvidenceSnapshot.Capture(
            "Validation · wrong adapter",
            Interval(fixture.Timestamp.AddMinutes(2), measured: true),
            fixture.Timestamp.AddMinutes(2),
            fixture.Configuration,
            wrongAdapterContext);
        var adapterMismatch = validated with
        {
            Candidate = adapterMismatchCandidate,
            ValidationEvidence = adapterMismatchValidation
        };
        Require(adapterMismatch.CanOriginateProfile,
            "Adapter mismatch in additive universal metadata must not alter specialized validation authority.");
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(adapterMismatch, fixture.CandidateSpace) is null,
            "Universal validation context must match the exact resolved candidate-space AdapterId.");

        var crossWorkloadIdentity = fixture.CandidateSpace.Identity with
        {
            GameId = "garena.free-fire-max",
            AdapterId = "bluestacks.free-fire-max",
            LegacyGameKind = GameKind.FreeFireMax
        };
        var crossWorkloadSpace = fixture.CandidateSpace with
        {
            Identity = crossWorkloadIdentity,
            AdapterId = crossWorkloadIdentity.AdapterId
        };
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(validated, crossWorkloadSpace) is null,
            "A validated Free Fire record must not project onto a Free Fire MAX universal candidate space even if candidate values happen to match.");

        var wrongGameContext = fixture.UniversalContext with { GameId = "garena.free-fire-max" };
        var wrongGameCandidate = PerformanceEvidenceSnapshot.Capture(
            "B · wrong game context",
            Interval(fixture.Timestamp.AddMinutes(1), measured: true),
            fixture.Timestamp.AddMinutes(1),
            fixture.Configuration,
            wrongGameContext);
        var wrongGameValidation = PerformanceEvidenceSnapshot.Capture(
            "Validation · wrong game context",
            Interval(fixture.Timestamp.AddMinutes(2), measured: true),
            fixture.Timestamp.AddMinutes(2),
            fixture.Configuration,
            wrongGameContext);
        var wrongGameRecord = validated with
        {
            Candidate = wrongGameCandidate,
            ValidationEvidence = wrongGameValidation
        };
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(wrongGameRecord, fixture.CandidateSpace) is null,
            "A universal GameId mismatch must fail closed; snapshot capture may omit incompatible universal context rather than fabricate cross-workload identity.");
    }

    private static void RejectsMissingOrAmbiguousCandidateBinding()
    {
        var fixture = CreateFixture();
        var validated = ValidatedRecord(fixture);

        var missing = fixture.CandidateSpace with
        {
            Bindings = fixture.CandidateSpace.Bindings
                .Where(binding => !ReferenceEquals(binding, fixture.Binding))
                .ToArray()
        };
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(validated, missing) is null,
            "Validated candidate configuration with zero exact Slice 3 bindings must fail closed.");

        var ambiguous = fixture.CandidateSpace with
        {
            Bindings = fixture.CandidateSpace.Bindings.Concat([fixture.Binding]).ToArray()
        };
        Require(BlueStacksUniversalValidatedPerformanceBridge.TryProject(validated, ambiguous) is null,
            "Validated candidate configuration with more than one exact Slice 3 binding must fail closed.");
    }

    private static PerformanceComparisonHistoryRecord ValidatedRecord(Fixture fixture)
    {
        var baseline = PerformanceEvidenceSnapshot.Capture(
            "A · baseline",
            Interval(fixture.Timestamp, measured: true),
            fixture.Timestamp,
            fixture.Configuration,
            fixture.UniversalContext);
        var candidate = PerformanceEvidenceSnapshot.Capture(
            "B · candidate",
            Interval(fixture.Timestamp.AddMinutes(1), measured: true),
            fixture.Timestamp.AddMinutes(1),
            fixture.Configuration,
            fixture.UniversalContext);
        var validation = PerformanceEvidenceSnapshot.Capture(
            "Validation · fresh",
            Interval(fixture.Timestamp.AddMinutes(2), measured: true),
            fixture.Timestamp.AddMinutes(2),
            fixture.Configuration,
            fixture.UniversalContext);

        return new PerformanceComparisonHistoryRecord
        {
            Label = "Validated universal correlation fixture",
            SavedAt = fixture.Timestamp.AddMinutes(1),
            Baseline = baseline,
            Candidate = candidate,
            ValidationStatus = PerformanceComparisonValidationStatus.Validated,
            ValidationEvidence = validation,
            ValidatedAt = fixture.Timestamp.AddMinutes(2)
        };
    }

    private static Fixture CreateFixture()
    {
        var timestamp = new DateTimeOffset(2026, 9, 10, 3, 0, 0, TimeSpan.Zero);
        var baseInstance = new BlueStacksInstance
        {
            Name = "Pie64",
            AndroidVersion = "Pie 64-bit",
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            Fps = 90,
            Resolution = "1920x1080",
            Dpi = 320
        };
        var environment = new EnvironmentSnapshot
        {
            MachineName = "DG-UNIVERSAL-VALIDATION-TEST",
            WindowsDescription = "Windows 11 universal validation test",
            LogicalProcessors = 8,
            MemoryTotalGb = 16,
            Is64BitOs = true,
            BlueStacksDetected = true,
            Instances = [baseInstance],
            ActiveGame = GameKind.FreeFire
        };
        var engine = new AutoTunerEngine();
        var candidateSpace = new BlueStacksUniversalTuningCandidateBridge(engine, CreateResolver()).Build(
            environment,
            baseInstance,
            GameKind.FreeFire,
            AutoTunerMode.Deep,
            FullCapturedSettings(baseInstance.Name));
        Require(candidateSpace.Bindings.Count > 0,
            "Universal validation fixture requires at least one exact applicable Slice 3 candidate binding.");
        var binding = candidateSpace.Bindings[0];
        var candidate = binding.SpecializedCandidate;
        var configuredInstance = baseInstance with
        {
            CpuCores = candidate.CpuCores,
            RamMb = candidate.RamMb,
            Renderer = candidate.Renderer,
            Fps = candidate.FpsTarget,
            Resolution = candidate.Resolution
        };
        var configuredEnvironment = environment with { Instances = [configuredInstance] };
        var configuration = PerformanceConfigurationSnapshot.Capture(
            configuredEnvironment,
            configuredInstance,
            GameKind.FreeFire)
            ?? throw new InvalidOperationException("Universal validation fixture requires a complete specialized PerformanceConfigurationSnapshot.");
        var universalContext = UniversalContext(candidateSpace, "universal-validation-machine-v2");

        return new Fixture(timestamp, candidateSpace, binding, configuration, universalContext);
    }

    private static PerformanceUniversalConfigurationContext UniversalContext(
        BlueStacksUniversalTuningCandidateSpace candidateSpace,
        string machineFingerprintId)
        => new PerformanceUniversalConfigurationContext
        {
            SchemaVersion = 1,
            GameId = candidateSpace.Identity.GameId,
            AdapterId = candidateSpace.AdapterId,
            Machine = new MachineEnvironmentFingerprintV2
            {
                SchemaVersion = 2,
                Id = machineFingerprintId,
                MachineName = "DG-UNIVERSAL-VALIDATION-TEST",
                WindowsDescription = "Windows 11 universal validation test",
                LogicalProcessors = 8,
                MemoryTotalMb = 16384,
                Is64BitOs = true,
                HardwareSignatures = ["CPU|TEST", "GPU|TEST"]
            },
            CapabilityValues = Array.Empty<PerformanceCapabilityValueSnapshot>(),
            WorkloadConfiguration = new Dictionary<string, string>(),
            DisplayDriverContext = new Dictionary<string, string>()
        }.Rehydrate();

    private static PerformanceIntervalSummary Interval(DateTimeOffset start, bool measured)
    {
        var quality = measured ? "Measured" : "Partial";
        return new PerformanceIntervalSummary
        {
            Start = start,
            End = start.AddSeconds(1),
            TelemetrySamples = 2,
            FpsEvidenceSamples = 2,
            AverageFps = 120,
            AverageFrameTimeMs = measured ? 8.33 : null,
            Points =
            [
                new PerformanceTimelinePoint
                {
                    Timestamp = start,
                    Fps = 120,
                    FrameTimeMs = measured ? 8.33 : null,
                    LatencyMs = 7,
                    DataQuality = quality
                },
                new PerformanceTimelinePoint
                {
                    Timestamp = start.AddSeconds(1),
                    Fps = 120,
                    FrameTimeMs = measured ? 8.33 : null,
                    LatencyMs = 7,
                    DataQuality = quality
                }
            ]
        };
    }

    private static GameAdapterResolver CreateResolver()
        => new(
        [
            new GenericGameAdapter(),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFire),
            BlueStacksFreeFireGameAdapter.For(GameKind.FreeFireMax)
        ]);

    private static Dictionary<string, string> FullCapturedSettings(string instanceName)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            [$"bst.instance.{instanceName}.cpus"] = "\"4\"",
            [$"bst.instance.{instanceName}.ram"] = "\"4096\"",
            [$"bst.instance.{instanceName}.max_fps"] = "\"90\"",
            [$"bst.instance.{instanceName}.enable_high_fps"] = "\"1\"",
            [$"bst.instance.{instanceName}.display_width"] = "\"1920\"",
            [$"bst.instance.{instanceName}.display_height"] = "\"1080\"",
            [$"bst.instance.{instanceName}.graphics_renderer"] = "\"Vulkan\""
        };

    private sealed record Fixture(
        DateTimeOffset Timestamp,
        BlueStacksUniversalTuningCandidateSpace CandidateSpace,
        BlueStacksUniversalTuningCandidateBinding Binding,
        PerformanceConfigurationSnapshot Configuration,
        PerformanceUniversalConfigurationContext UniversalContext);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
