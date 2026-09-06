using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class BlueStacksAutoTunerRuntimeSelfTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        await AppliesPreparesMeasuresAndRestoresOwnedSession();
        await RejectsUnmanagedRunningPlayer();
        await RejectsUnsupportedRendererMutation();
        Console.WriteLine("PASS safe physical auto tuner runtime lifecycle");
    }

    private static async Task AppliesPreparesMeasuresAndRestoresOwnedSession()
    {
        var instance = BaselineInstance();
        var platform = new FakePlatform();
        var runtime = new BlueStacksAutoTunerRuntime(instance, platform, new AutoTunerRuntimeOptions
        {
            StartupSettleDelay = TimeSpan.Zero,
            ForegroundTimeout = TimeSpan.FromSeconds(1),
            BenchmarkDuration = TimeSpan.FromSeconds(2),
            ProcessStopTimeout = TimeSpan.FromSeconds(1)
        });
        var candidate = new TuningCandidate
        {
            CpuCores = 6,
            RamMb = 6144,
            Renderer = "OpenGL",
            FpsTarget = 120,
            Resolution = "1600x900"
        };

        var applied = await runtime.ApplyCandidateAsync(candidate);
        Require(applied.Success, applied.Message);
        Require(platform.LastUpdates is not null, "Candidate settings must be written through the allow-listed BlueStacks writer.");
        Require(platform.LastUpdates!["cpus"] == "6", "CPU candidate must be applied.");
        Require(platform.LastUpdates["ram"] == "6144", "RAM candidate must be applied.");
        Require(platform.LastUpdates["max_fps"] == "120", "Existing FPS key must be reused rather than inventing a version-specific key.");
        Require(platform.LastUpdates["enable_high_fps"] == "1", "High FPS must be enabled when the target is above 60 and the installed build exposes that key.");
        Require(platform.LastUpdates["fb_width"] == "1600" && platform.LastUpdates["fb_height"] == "900", "Existing framebuffer resolution keys must be reused.");

        var prepared = await runtime.PrepareGameAsync(GameKind.FreeFireMax);
        Require(prepared.Success, prepared.Message);
        Require(platform.Events.Contains("start:Pie64"), "Runtime must start the selected instance itself when no player is running.");
        Require(platform.Events.Contains("prepare:Pie64:FreeFireMax"), "Runtime must prepare the requested game on the selected instance.");

        var sample = await runtime.CaptureBenchmarkAsync();
        Require(sample?.Fps == 120, "Runtime must return the real benchmark sample supplied by the telemetry platform.");
        Require(platform.Events.Contains("capture:4242:2"), "Benchmark telemetry must target the exact BlueStacks PID owned by the tuning session, never an arbitrary HD-Player process.");

        await runtime.CompleteCandidateAsync();
        Require(platform.Events.Contains("stop:4242"), "Only the player PID started by the runtime may be stopped automatically.");
        Require(platform.Events.Contains("restore:C:/ProgramData/BlueStacks_nxt/bluestacks.conf.ffpe-baseline.bak"), "Candidate cleanup must restore the exact baseline backup.");
        Require(!platform.PlayerRunning, "Owned player must be stopped before baseline restoration.");

        var restoreCount = platform.Events.Count(x => x.StartsWith("restore:", StringComparison.Ordinal));
        await runtime.RestoreBaselineAsync();
        Require(platform.Events.Count(x => x.StartsWith("restore:", StringComparison.Ordinal)) == restoreCount, "Final baseline restore must be idempotent after successful candidate cleanup.");
    }

    private static async Task RejectsUnmanagedRunningPlayer()
    {
        var platform = new FakePlatform { PlayerRunning = true };
        var runtime = new BlueStacksAutoTunerRuntime(BaselineInstance(), platform, new AutoTunerRuntimeOptions { StartupSettleDelay = TimeSpan.Zero });
        var result = await runtime.ApplyCandidateAsync(new TuningCandidate
        {
            CpuCores = 6,
            RamMb = 4096,
            Renderer = "OpenGL",
            FpsTarget = 90,
            Resolution = "1280x720"
        });

        Require(!result.Success, "Runtime must not mutate restart-required settings while an unmanaged BlueStacks player is running.");
        Require(platform.LastUpdates is null, "No configuration write is allowed when the running player is not owned by this tuning session.");
        Require(!platform.Events.Any(x => x.StartsWith("stop:", StringComparison.Ordinal)), "Runtime must never kill an unowned BlueStacks process.");
    }

    private static async Task RejectsUnsupportedRendererMutation()
    {
        var platform = new FakePlatform();
        var runtime = new BlueStacksAutoTunerRuntime(BaselineInstance(), platform, new AutoTunerRuntimeOptions { StartupSettleDelay = TimeSpan.Zero });
        var result = await runtime.ApplyCandidateAsync(new TuningCandidate
        {
            CpuCores = 4,
            RamMb = 4096,
            Renderer = "Vulkan",
            FpsTarget = 90,
            Resolution = "1280x720"
        });

        Require(!result.Success && result.Message.Contains("Renderer", StringComparison.OrdinalIgnoreCase), "Version-dependent renderer mutations must stay evidence-gated rather than being guessed.");
        Require(platform.LastUpdates is null, "Unsupported candidate dimensions must reject the candidate instead of partially applying it.");
    }

    private static BlueStacksInstance BaselineInstance() => new()
    {
        Name = "Pie64",
        CpuCores = 4,
        RamMb = 4096,
        Renderer = "OpenGL",
        Fps = 90,
        Resolution = "1280x720",
        AdbPort = 5555,
        AdbEnabled = true
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakePlatform : IBlueStacksAutoTunerPlatform
    {
        public bool PlayerRunning { get; set; }
        public Dictionary<string, string>? LastUpdates { get; private set; }
        public List<string> Events { get; } = [];

        public bool IsPlayerRunning() => PlayerRunning;

        public IReadOnlyDictionary<string, string> CaptureAllowedSettings(string instanceName)
            => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [$"bst.instance.{instanceName}.cpus"] = "\"4\"",
                [$"bst.instance.{instanceName}.ram"] = "\"4096\"",
                [$"bst.instance.{instanceName}.max_fps"] = "\"90\"",
                [$"bst.instance.{instanceName}.enable_high_fps"] = "\"1\"",
                [$"bst.instance.{instanceName}.fb_width"] = "\"1280\"",
                [$"bst.instance.{instanceName}.fb_height"] = "\"720\""
            };

        public BlueStacksConfigWriteResult ApplyInstanceSettings(string instanceName, IReadOnlyDictionary<string, string> updates)
        {
            LastUpdates = new Dictionary<string, string>(updates, StringComparer.OrdinalIgnoreCase);
            Events.Add($"apply:{instanceName}");
            return new(true, false, "candidate applied", "C:/ProgramData/BlueStacks_nxt/bluestacks.conf.ffpe-baseline.bak");
        }

        public BlueStacksConfigWriteResult RestoreBackup(string backupPath)
        {
            Events.Add($"restore:{backupPath}");
            return new(true, false, "restored", backupPath);
        }

        public string? FindPlayerExecutable() => "C:/Program Files/BlueStacks_nxt/HD-Player.exe";

        public ProcessStartResult StartInstance(BlueStacksInstance instance)
        {
            PlayerRunning = true;
            Events.Add($"start:{instance.Name}");
            return new(true, 4242, null);
        }

        public Task<AutomationActionResult> PrepareRunningGameAsync(BlueStacksInstance instance, GameKind game, TimeSpan foregroundTimeout, CancellationToken cancellationToken = default)
        {
            Events.Add($"prepare:{instance.Name}:{game}");
            return Task.FromResult(new AutomationActionResult(true, "prepared"));
        }

        public Task<TelemetrySample?> CaptureBenchmarkAsync(int processId, TimeSpan duration, CancellationToken cancellationToken = default)
        {
            Events.Add($"capture:{processId}:{duration.TotalSeconds:0}");
            return Task.FromResult<TelemetrySample?>(new TelemetrySample
            {
                Fps = 120,
                OnePercentLow = 108,
                FrameTimeMs = 8.33,
                FrameTimeP95Ms = 9.1,
                StutterPercent = 0.6,
                DataQuality = "PresentMon test"
            });
        }

        public Task<OwnedProcessStopResult> StopOwnedPlayerAsync(int processId, string expectedExecutablePath, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            Events.Add($"stop:{processId}");
            PlayerRunning = false;
            return Task.FromResult(new OwnedProcessStopResult(true, "stopped"));
        }
    }
}
