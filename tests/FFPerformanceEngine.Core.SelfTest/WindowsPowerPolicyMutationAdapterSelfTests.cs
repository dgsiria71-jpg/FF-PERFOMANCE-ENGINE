using FFPerformanceEngine.Core.Services;
using FFPerformanceEngine.Core.SystemOptimization;

internal static class WindowsPowerPolicyMutationAdapterSelfTests
{
    private static readonly Guid Balanced = Guid.Parse("381b4222-f694-41f0-9685-ff5bb260df2e");
    private static readonly Guid HighPerformance = Guid.Parse("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

    internal static async Task RunAsync()
    {
        var executor = new FakePowerCfgExecutor(Balanced);
        var adapter = new WindowsPowerPolicyMutationAdapter(executor);

        var read = await adapter.ReadCurrentAsync();
        Require(read.Success && string.Equals(read.Value, Balanced.ToString("D"), StringComparison.OrdinalIgnoreCase),
            "Power policy adapter must parse the active GUID without depending on localized powercfg labels.");

        var sessionValidation = adapter.Validate(HighPerformance.ToString("D"), SystemOptimizationScope.Session);
        var persistentValidation = adapter.Validate(HighPerformance.ToString("D"), SystemOptimizationScope.Persistent);
        Require(sessionValidation.Success && persistentValidation.Success,
            "Active power policy must support both reversible session and persistent transactions.");
        Require(!adapter.Validate("not-a-guid", SystemOptimizationScope.Session).Success,
            "Power policy adapter must reject non-GUID targets before invoking powercfg.");

        var snapshot = await adapter.SnapshotAsync();
        Require(string.Equals(snapshot.OriginalValue, Balanced.ToString("D"), StringComparison.OrdinalIgnoreCase),
            "Power policy snapshot must preserve the exact active scheme GUID for rollback.");

        var applied = await adapter.ApplyAsync(HighPerformance.ToString("D"));
        Require(applied.Success && executor.ActiveScheme == HighPerformance,
            "Power policy adapter must apply the requested GUID using powercfg /setactive.");
        Require(await adapter.VerifyAsync(HighPerformance.ToString("D")),
            "Power policy adapter must re-read Windows state and verify the requested scheme after apply.");

        await adapter.RollbackAsync(snapshot);
        Require(executor.ActiveScheme == Balanced && await adapter.VerifyAsync(Balanced.ToString("D")),
            "Power policy rollback must restore and verify the exact pre-transaction scheme GUID.");

        Require(executor.Invocations.Any(call => call.SequenceEqual(new[] { "/getactivescheme" }, StringComparer.OrdinalIgnoreCase)),
            "Power policy adapter must query current state through powercfg /getactivescheme.");
        Require(executor.Invocations.Any(call => call.Count == 2
            && string.Equals(call[0], "/setactive", StringComparison.OrdinalIgnoreCase)
            && string.Equals(call[1], HighPerformance.ToString("D"), StringComparison.OrdinalIgnoreCase)),
            "Power policy apply must pass the target GUID as a separate ProcessStartInfo argument, not concatenate a shell command.");

        var unavailable = new WindowsPowerPolicyMutationAdapter(new FailingExecutor());
        var unavailableRead = await unavailable.ReadCurrentAsync();
        Require(!unavailableRead.Success && unavailableRead.Value is null,
            "If powercfg cannot read the active scheme, the adapter must report unavailable state instead of inventing a default.");

        Console.WriteLine("PASS Track 2 real Windows active power policy adapter read/validate/snapshot/apply/verify/rollback contract");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class FakePowerCfgExecutor(Guid activeScheme) : IProcessExecutor
    {
        public Guid ActiveScheme { get; private set; } = activeScheme;
        public List<IReadOnlyList<string>> Invocations { get; } = new();

        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Require(string.Equals(fileName, "powercfg.exe", StringComparison.OrdinalIgnoreCase),
                "Power policy adapter must invoke the Windows powercfg executable directly.");
            Invocations.Add(arguments.ToArray());

            if (arguments.Count == 1 && string.Equals(arguments[0], "/getactivescheme", StringComparison.OrdinalIgnoreCase))
            {
                // Deliberately use a non-English label/prefix. The parser contract is GUID-based.
                return Task.FromResult(new ProcessExecutionResult(
                    0,
                    $"Esquema ativo qualquer: {ActiveScheme:D}  (nome localizado)",
                    string.Empty));
            }

            if (arguments.Count == 2 && string.Equals(arguments[0], "/setactive", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(arguments[1], out var target))
            {
                ActiveScheme = target;
                return Task.FromResult(new ProcessExecutionResult(0, string.Empty, string.Empty));
            }

            return Task.FromResult(new ProcessExecutionResult(1, string.Empty, "unsupported fake command"));
        }

        public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
            => new(false, null, "not used");
    }

    private sealed class FailingExecutor : IProcessExecutor
    {
        public Task<ProcessExecutionResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessExecutionResult(1, string.Empty, "powercfg unavailable"));

        public ProcessStartResult StartDetached(string fileName, IReadOnlyList<string> arguments)
            => new(false, null, "not used");
    }
}
