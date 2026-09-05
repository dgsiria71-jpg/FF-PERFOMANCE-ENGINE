using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

var failures = new List<string>();
void Check(string name, bool condition)
{
    if (condition) Console.WriteLine($"PASS {name}");
    else { Console.WriteLine($"FAIL {name}"); failures.Add(name); }
}

var tempRoot = Path.Combine(Path.GetTempPath(), "ffpe-selftest-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
try
{
    var settingsPath = Path.Combine(tempRoot, "settings.json");
    var settingsService = new SettingsService(settingsPath);
    var expectedSettings = new AppSettings { Theme = ThemeMode.Dark, KeepDeepAsDefault = true, ArgbPreset = "Adaptive" };
    await settingsService.SaveAsync(expectedSettings);
    var loadedSettings = await settingsService.LoadAsync();
    Check("settings round-trip", loadedSettings.Theme == ThemeMode.Dark && loadedSettings.KeepDeepAsDefault);

    const string config = """
        bst.instance.Pie64.cpus="6"
        bst.instance.Pie64.ram="6144"
        bst.instance.Pie64.graphics_renderer="Vulkan"
        bst.instance.Pie64.fps="120"
        bst.instance.Pie64.display_width="1920"
        bst.instance.Pie64.display_height="1080"
        bst.instance.Android11.cpus="4"
        bst.instance.Android11.ram="4096"
        """;
    var blueStacks = new BlueStacksService();
    var instances = blueStacks.ParseConfig(config);
    var pie = instances.Single(x => x.Name == "Pie64");
    Check("BlueStacks config parser", pie.CpuCores == 6 && pie.RamMb == 6144 && pie.Renderer == "Vulkan" && pie.Resolution == "1920x1080");

    var guardian = new GuardianEngine { Mode = GuardianMode.Adaptive };
    guardian.SetState(GameState.Match);
    var unsafeAction = new GuardianAction { Id = "restart", Description = "restart", Safety = ActionSafety.RestartRequired, MinimumConfidence = 0.5 };
    var unsafeDecision = guardian.Evaluate(120, new TelemetrySample { Fps = 70 }, unsafeAction);
    Check("Guardian blocks restart-required action in match", !unsafeDecision.ShouldAct);

    var safeAction = new GuardianAction { Id = "priority", Description = "priority", Safety = ActionSafety.LiveSafe, MinimumConfidence = 0.5 };
    var safeDecision = guardian.Evaluate(120, new TelemetrySample { Fps = 80 }, safeAction);
    Check("Guardian allows high-confidence LiveSafe action", safeDecision.ShouldAct && safeDecision.Action?.Id == "priority");
    Check("Guardian canary keeps measured improvement", GuardianEngine.CanaryImproved(new TelemetrySample { Fps = 80, FrameTimeMs = 12.5 }, new TelemetrySample { Fps = 92, FrameTimeMs = 10.8 }));
    Check("Guardian canary rejects regression", !GuardianEngine.CanaryImproved(new TelemetrySample { Fps = 80, FrameTimeMs = 12.5 }, new TelemetrySample { Fps = 78, FrameTimeMs = 13.0 }));

    var presentMon = new PresentMonService();
    const string presentMonCsv = "Application,ProcessID,MsBetweenPresents,DisplayLatency\nHD-Player.exe,123,10.0,8.0\nHD-Player.exe,123,8.0,7.0\nHD-Player.exe,123,12.0,9.0\n";
    var parsedCapture = presentMon.ParseCsv(presentMonCsv);
    Check("PresentMon CSV produces evidence", parsedCapture is not null && parsedCapture.Fps is > 80 and < 130 && parsedCapture.LatencyMs == 8.0);

    var environment = new EnvironmentSnapshot { LogicalProcessors = 12 };
    var tuner = new AutoTunerEngine();
    var adaptive = tuner.GenerateCandidates(environment, pie, AutoTunerMode.Adaptive);
    var deep = tuner.GenerateCandidates(environment, pie, AutoTunerMode.Deep);
    Check("Deep tuner explores more candidates", deep.Count > adaptive.Count);

    var noEvidence = tuner.SelectWinners(GameKind.FreeFireMax, AutoTunerMode.Deep, Array.Empty<CandidateEvidence>());
    Check("Auto Tuner never invents winners without evidence", noEvidence.Winners.Count == 0 && noEvidence.Summary.Contains("no winner", StringComparison.OrdinalIgnoreCase));

    var evidence = new List<CandidateEvidence>
    {
        new() { Candidate = adaptive[0], Evidence = EvidenceLevel.Validated, Confidence = 0.95, Sample = new TelemetrySample { Fps = 100, OnePercentLow = 92, LatencyMs = 10, GpuTemperatureC = 60 } },
        new() { Candidate = adaptive[1], Evidence = EvidenceLevel.Validated, Confidence = 0.97, Sample = new TelemetrySample { Fps = 108, OnePercentLow = 90, LatencyMs = 12, GpuTemperatureC = 64 } },
        new() { Candidate = adaptive[2], Evidence = EvidenceLevel.Validated, Confidence = 0.98, Sample = new TelemetrySample { Fps = 103, OnePercentLow = 99, LatencyMs = 8, GpuTemperatureC = 61 } }
    };
    var winners = tuner.SelectWinners(GameKind.FreeFireMax, AutoTunerMode.Adaptive, evidence);
    Check("Auto Tuner returns five evidence-backed roles", winners.Winners.Count == 5 && winners.Winners.All(x => x.Evidence == EvidenceLevel.Validated));

    var profileService = new ProfileService(Path.Combine(tempRoot, "profiles.json"));
    await profileService.SaveAsync(winners.Winners);
    var reloadedProfiles = await profileService.LoadAsync();
    Check("profiles round-trip", reloadedProfiles.Count == 5);

    var historyService = new HistoryService(Path.Combine(tempRoot, "history.json"));
    await historyService.AppendAsync(new HistoryEvent { Kind = HistoryEventKind.Benchmark, Title = "Benchmark", Summary = "Validated" });
    Check("history append", (await historyService.LoadAsync()).Count == 1);

    // Persistence tests perform real async file I/O. Run them here, after process startup,
    // rather than under a CLR module initializer/loader context.
    await AutoTunerSessionPersistenceSelfTests.RunAsync();
    await OptimizeWorkflowSelfTests.RunAsync();
    await GuardianSupervisorSelfTests.RunAsync();
    await GuardianLiveSessionSelfTests.RunAsync();
    await GuardianSessionHostSelfTests.RunAsync();
    GuardianStartupPolicySelfTests.Run();
    GuardianPresentationSelfTests.Run();
}
finally
{
    try { Directory.Delete(tempRoot, true); } catch { }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine($"{failures.Count} self-test(s) failed: {string.Join(", ", failures)}");
    return 1;
}

Console.WriteLine("All FF Performance Engine core self-tests passed.");
return 0;
