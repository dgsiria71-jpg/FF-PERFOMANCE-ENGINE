using System.Collections;
using System.Reflection;
using FFPerformanceEngine.Core.Models;
using FFPerformanceEngine.Core.Services;

internal static class UniversalDiagnosticFoundationSelfTests
{
    internal static void Run()
    {
        var assembly = typeof(EnvironmentSnapshot).Assembly;
        var hardwareServiceType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.HardwareDiscoveryService");
        var capabilityType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.WindowsPerformanceCapability");
        var registryType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.WindowsPerformanceCapabilityRegistry");
        var machineContextServiceType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.MachineContextService");
        var bottleneckContextType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.BottleneckAnalysisContext");
        var bottleneckAnalyzerType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.UniversalBottleneckAnalyzer");
        var diagnosticServiceType = RequireType(assembly, "FFPerformanceEngine.Core.Diagnostics.UniversalDiagnosticService");

        var environment = Environment();
        var hardwareService = Activator.CreateInstance(hardwareServiceType)
            ?? throw new InvalidOperationException("HardwareDiscoveryService must support default construction.");
        var hardware = Invoke(hardwareServiceType, hardwareService, "Discover", environment)
            ?? throw new InvalidOperationException("Hardware discovery must return a result.");
        var components = AsObjects(Read<object>(hardware, "Components"));
        Require(components.Any(item => Read(item, "Kind").ToString() == "Cpu"),
            "Hardware discovery must always represent the CPU described by EnvironmentSnapshot instead of requiring a game/emulator.");
        var cpu = components.First(item => Read(item, "Kind").ToString() == "Cpu");
        Require(Read<int?>(cpu, "LogicalProcessors") == 16,
            "CPU discovery must preserve the exact logical processor count from the existing EnvironmentSnapshot bridge.");
        var memory = components.FirstOrDefault(item => Read(item, "Kind").ToString() == "Memory");
        Require(memory is not null && Read<long?>(memory!, "CapacityBytes") is > 34_000_000_000L,
            "Hardware discovery must preserve real memory capacity without inventing a rounded preset.");

        var registry = Activator.CreateInstance(registryType)
            ?? throw new InvalidOperationException("WindowsPerformanceCapabilityRegistry must support default construction.");
        var capabilities = AsObjects(Invoke(registryType, registry, "GetAll")!);
        Require(capabilities.Count >= 8,
            "The Track 1 registry must ship a centralized seed catalog spanning the Windows performance control plane.");
        var ids = capabilities.Select(item => Read<string>(item, "CapabilityId")).ToArray();
        Require(ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Length,
            "Capability IDs must be unique case-insensitively.");
        Require(capabilities.All(item => Read(item, "Availability").ToString() == "Unknown"),
            "Track 1 must not claim a mutation capability is available before a concrete reader/adapter verifies it.");
        Require(capabilities.All(item => Read<string?>(item, "CurrentValue") is null),
            "The catalog must never fabricate a current Windows value before discovery reads it.");

        var boost = capabilities.Single(item => Read<string>(item, "CapabilityId") == "windows.cpu.boost_policy");
        var dependencies = AsStrings(Read<object>(boost, "Dependencies"));
        Require(dependencies.Contains("windows.power.active_policy", StringComparer.OrdinalIgnoreCase),
            "CPU boost policy must explicitly depend on the active power policy in the capability graph.");
        var plan = Invoke(registryType, registry, "ResolvePlan", new[] { "windows.cpu.boost_policy" })!;
        Require(Read<bool>(plan, "IsValid"), "A known capability and its dependencies must resolve to a valid plan.");
        var ordered = AsObjects(Read<object>(plan, "OrderedCapabilities"))
            .Select(item => Read<string>(item, "CapabilityId")).ToArray();
        Require(ordered.SequenceEqual(new[] { "windows.power.active_policy", "windows.cpu.boost_policy" }, StringComparer.OrdinalIgnoreCase),
            "Capability planning must topologically order dependencies before dependents.");
        Require(AsObjects(Invoke(registryType, registry, "ValidateGraph")!).Count == 0,
            "The built-in capability graph must be internally valid.");

        VerifyGraphRejectsConflictsAndCycles(capabilityType, registryType);

        var machineContextService = Activator.CreateInstance(machineContextServiceType, hardwareService, registry)
            ?? throw new InvalidOperationException("MachineContextService must accept shared hardware discovery and capability registry services.");
        var machine = Invoke(machineContextServiceType, machineContextService, "Capture", environment)
            ?? throw new InvalidOperationException("MachineContextService must capture a machine context.");
        Require(Read<int>(machine, "SchemaVersion") == 2, "MachineContext must start at schema version 2 for DG universal diagnostics.");
        Require(Read<EnvironmentSnapshot>(machine, "Environment").MachineName == "DG-TRACK1-PC",
            "MachineContext must bridge the existing EnvironmentSnapshot rather than create a disconnected parallel environment model.");
        Require(AsObjects(Read<object>(machine, "Capabilities")).Count == capabilities.Count,
            "MachineContext must expose the same centralized capability catalog used by the Windows control plane.");

        var fingerprint = Read<object>(machine, "Fingerprint");
        Require(Read<int>(fingerprint, "SchemaVersion") == 2 && Read<string>(fingerprint, "Id").Length == 64,
            "Environment fingerprint v2 must use a deterministic SHA-256 identity.");
        var secondMachine = Invoke(machineContextServiceType, machineContextService, "Capture", environment)!;
        Require(string.Equals(Read<string>(fingerprint, "Id"), Read<string>(Read<object>(secondMachine, "Fingerprint"), "Id"), StringComparison.Ordinal),
            "Repeated captures of the same stable environment must produce the same v2 fingerprint even if discovery ordering changes.");

        var instance = environment.Instances.Single();
        var legacy = PerformanceEnvironmentFingerprint.Capture(environment, instance, GameKind.FreeFireMax);
        var compatibleWithLegacy = Invoke(fingerprint.GetType(), fingerprint, "IsCompatibleWithLegacy", legacy);
        Require(compatibleWithLegacy is true,
            "Fingerprint v2 must explicitly recognize matching v1 performance evidence so existing validated profiles remain usable.");
        var driftedLegacy = legacy with { WindowsDescription = "Windows 11 drifted build" };
        Require(Invoke(fingerprint.GetType(), fingerprint, "IsCompatibleWithLegacy", driftedLegacy) is false,
            "Fingerprint v2 compatibility must reject Windows/build drift instead of silently treating old evidence as current.");

        var analyzer = Activator.CreateInstance(bottleneckAnalyzerType)
            ?? throw new InvalidOperationException("UniversalBottleneckAnalyzer must support default construction.");
        VerifyBottleneckClassification(bottleneckAnalyzerType, bottleneckContextType, analyzer);

        var diagnostic = Activator.CreateInstance(diagnosticServiceType, machineContextService, analyzer)
            ?? throw new InvalidOperationException("UniversalDiagnosticService must compose MachineContext and BottleneckAnalyzer.");
        var gpuContext = NewContext(bottleneckContextType, targetFps: 120, criticalThreadCpu: 55);
        var snapshot = Invoke(diagnosticServiceType, diagnostic, "Analyze", environment, GpuBoundSample(), gpuContext)
            ?? throw new InvalidOperationException("Universal diagnostic analysis must return a snapshot.");
        var snapshotMachine = Read<object>(snapshot, "Machine");
        var snapshotBottleneck = Read<object>(snapshot, "Bottleneck");
        Require(Read<string>(Read<object>(snapshotMachine, "Fingerprint"), "Id") == Read<string>(fingerprint, "Id"),
            "UniversalDiagnosticService must reuse the shared MachineContext fingerprint rather than recalculate a divergent identity.");
        Require(Read(snapshotBottleneck, "Primary").ToString() == "Gpu",
            "UniversalDiagnosticService must expose the universal bottleneck result in the same diagnostic snapshot.");

        Console.WriteLine("PASS Track 1 universal MachineContext, hardware discovery, capability registry/graph, fingerprint v2 compatibility, and bottleneck analysis");
    }

    private static void VerifyGraphRejectsConflictsAndCycles(Type capabilityType, Type registryType)
    {
        var left = NewCapability(capabilityType, "test.left", dependencies: ["test.base"], conflicts: ["test.conflict"]);
        var baseCapability = NewCapability(capabilityType, "test.base");
        var conflict = NewCapability(capabilityType, "test.conflict");
        var customRegistry = NewRegistry(registryType, capabilityType, left, baseCapability, conflict);
        var conflictPlan = Invoke(registryType, customRegistry, "ResolvePlan", new[] { "test.left", "test.conflict" })!;
        Require(!Read<bool>(conflictPlan, "IsValid") && AsObjects(Read<object>(conflictPlan, "Issues")).Count > 0,
            "Capability planning must fail closed when selected capabilities conflict.");

        var cycleA = NewCapability(capabilityType, "cycle.a", dependencies: ["cycle.b"]);
        var cycleB = NewCapability(capabilityType, "cycle.b", dependencies: ["cycle.a"]);
        var cycleRegistry = NewRegistry(registryType, capabilityType, cycleA, cycleB);
        Require(AsObjects(Invoke(registryType, cycleRegistry, "ValidateGraph")!).Count > 0,
            "Capability graph validation must detect dependency cycles before Track 2 transactions use the graph.");
    }

    private static void VerifyBottleneckClassification(Type analyzerType, Type contextType, object analyzer)
    {
        var gpu = Invoke(analyzerType, analyzer, "Analyze", GpuBoundSample(), NewContext(contextType, 120, 55))!;
        Require(Read(gpu, "Primary").ToString() == "Gpu" && Read<double>(gpu, "Confidence") >= 0.70,
            "Frame pressure plus near-saturated GPU and a non-saturated critical CPU thread must classify as a probable GPU bottleneck.");

        var cpuSample = new TelemetrySample
        {
            Fps = 84,
            FrameTimeMs = 11.9,
            CpuPercent = 68,
            GpuPercent = 58,
            DataQuality = "Measured"
        };
        var cpu = Invoke(analyzerType, analyzer, "Analyze", cpuSample, NewContext(contextType, 120, 99))!;
        Require(Read(cpu, "Primary").ToString() == "Cpu" && Read<double>(cpu, "Confidence") >= 0.70,
            "A saturated critical CPU thread with GPU headroom must classify as a probable CPU bottleneck even when total CPU is below 100%.");

        var sparse = Invoke(analyzerType, analyzer, "Analyze", new TelemetrySample { Fps = 120, DataQuality = "Partial" }, NewContext(contextType, null, null))!;
        Require(Read(sparse, "Primary").ToString() == "Unknown",
            "Sparse telemetry must remain Unknown instead of inventing a bottleneck.");

        var hotButUnproven = new TelemetrySample
        {
            Fps = 80,
            FrameTimeMs = 12.5,
            CpuTemperatureC = 101,
            GpuTemperatureC = 99,
            DataQuality = "Measured"
        };
        var hot = Invoke(analyzerType, analyzer, "Analyze", hotButUnproven, NewContext(contextType, 120, null))!;
        Require(Read(hot, "Primary").ToString() != "Thermal",
            "Temperature alone must never use a universal magic threshold to claim thermal throttling.");

        var thermalContext = NewContext(contextType, 120, null);
        Set(thermalContext, "IsThermallyThrottled", true);
        var thermal = Invoke(analyzerType, analyzer, "Analyze", hotButUnproven, thermalContext)!;
        Require(Read(thermal, "Primary").ToString() == "Thermal",
            "An explicit throttling signal must allow the analyzer to classify a thermal bottleneck.");
    }

    private static TelemetrySample GpuBoundSample() => new()
    {
        Fps = 84,
        FrameTimeMs = 11.9,
        FrameTimeP99Ms = 14.2,
        CpuPercent = 56,
        GpuPercent = 99,
        MemoryUsedGb = 12,
        MemoryTotalGb = 32,
        DataQuality = "Measured"
    };

    private static EnvironmentSnapshot Environment()
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
            Dpi = 240
        };
        return new EnvironmentSnapshot
        {
            MachineName = "DG-TRACK1-PC",
            WindowsDescription = "Windows 11 Track 1",
            LogicalProcessors = 16,
            MemoryTotalGb = 32,
            Is64BitOs = true,
            BlueStacksDetected = true,
            ActiveGame = GameKind.FreeFireMax,
            Instances = [instance]
        };
    }

    private static object NewContext(Type type, double? targetFps, double? criticalThreadCpu)
    {
        var value = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("BottleneckAnalysisContext must support default construction.");
        if (targetFps is not null) Set(value, "TargetFps", targetFps.Value);
        if (criticalThreadCpu is not null) Set(value, "CriticalThreadCpuPercent", criticalThreadCpu.Value);
        return value;
    }

    private static object NewCapability(Type type, string id, string[]? dependencies = null, string[]? conflicts = null)
    {
        var capability = Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("WindowsPerformanceCapability must support object-initializer style construction.");
        Set(capability, "CapabilityId", id);
        Set(capability, "Name", id);
        Set(capability, "Description", "self-test");
        if (dependencies is not null) Set(capability, "Dependencies", dependencies);
        if (conflicts is not null) Set(capability, "Conflicts", conflicts);
        return capability;
    }

    private static object NewRegistry(Type registryType, Type capabilityType, params object[] values)
    {
        var array = Array.CreateInstance(capabilityType, values.Length);
        for (var index = 0; index < values.Length; index++) array.SetValue(values[index], index);
        var constructor = registryType.GetConstructors()
            .FirstOrDefault(item => item.GetParameters().Length == 1);
        Require(constructor is not null,
            "WindowsPerformanceCapabilityRegistry must accept a custom capability collection for graph validation/testing/adapters.");
        return constructor!.Invoke([array]);
    }

    private static Type RequireType(Assembly assembly, string name)
        => assembly.GetType(name)
           ?? throw new InvalidOperationException($"Track 1 requires public Core type {name}.");

    private static object? Invoke(Type type, object target, string name, params object?[] args)
    {
        var candidates = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(item => string.Equals(item.Name, name, StringComparison.Ordinal))
            .ToArray();
        var method = candidates.FirstOrDefault(item => ParametersMatch(item.GetParameters(), args))
            ?? throw new InvalidOperationException($"Expected public method {type.Name}.{name} for the supplied arguments.");
        try { return method.Invoke(target, args); }
        catch (TargetInvocationException exception) when (exception.InnerException is not null) { throw exception.InnerException; }
    }

    private static bool ParametersMatch(ParameterInfo[] parameters, object?[] args)
    {
        if (parameters.Length != args.Length) return false;
        for (var index = 0; index < parameters.Length; index++)
        {
            var argument = args[index];
            if (argument is null) continue;
            if (!parameters[index].ParameterType.IsInstanceOfType(argument)) return false;
        }
        return true;
    }

    private static object Read(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance)
           ?? throw new InvalidOperationException($"Expected non-null public property {propertyName} on {instance.GetType().Name}.");

    private static T Read<T>(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Expected public property {propertyName} on {instance.GetType().Name}.");
        return (T)property.GetValue(instance)!;
    }

    private static void Set(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Expected public property {propertyName} on {instance.GetType().Name}.");
        property.SetValue(instance, value);
    }

    private static List<object> AsObjects(object value)
        => ((IEnumerable)value).Cast<object>().ToList();

    private static string[] AsStrings(object value)
        => ((IEnumerable)value).Cast<object>().Select(item => item.ToString() ?? string.Empty).ToArray();

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
