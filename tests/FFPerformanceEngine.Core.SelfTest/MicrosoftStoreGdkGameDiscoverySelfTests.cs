using System.Runtime.CompilerServices;
using FFPerformanceEngine.Core.Workloads;

internal static class MicrosoftStoreGdkGameDiscoverySelfTests
{
    private static string _stage = "not-started";

    [ModuleInitializer]
    internal static void Run()
    {
        try
        {
            RunAsync().WaitAsync(TimeSpan.FromSeconds(30)).GetAwaiter().GetResult();
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException(
                $"Microsoft Store GDK discovery self-test exceeded 30 seconds at stage '{_stage}'.",
                ex);
        }
    }

    private static async Task RunAsync()
    {
        _stage = "create-fixture";
        var root = Path.Combine(Path.GetTempPath(), "dgpe-msstore-discovery-" + Guid.NewGuid().ToString("N"));
        var forzaPath = Path.Combine(root, "ForzaPackage");
        var groundedPath = Path.Combine(root, "GroundedPackage");

        var observations = new MicrosoftStoreGamePackageObservation[]
        {
            new(
                PackageFamilyName: "Microsoft.624F8B84B80_8wekyb3d8bbwe",
                PackageFullName: "Microsoft.624F8B84B80_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Microsoft.624F8B84B80",
                DisplayName: "Forza Horizon 5",
                InstalledPath: forzaPath,
                EffectivePath: forzaPath,
                MicrosoftGameConfigXml: MainGameConfig(
                    identityName: "Microsoft.624F8B84B80",
                    executableName: "ForzaHorizon5.exe",
                    storeId: "9NNX1VVR3KNQ",
                    titleId: "1234ABCD")),

            // Same Package Family Name through another package observation/version.
            // It must merge into the same stable game identity.
            new(
                PackageFamilyName: "MICROSOFT.624F8B84B80_8WEKYB3D8BBWE",
                PackageFullName: "Microsoft.624F8B84B80_1.0.1.0_x64__8wekyb3d8bbwe",
                PackageName: "Microsoft.624F8B84B80",
                DisplayName: "Forza Horizon 5",
                InstalledPath: forzaPath,
                EffectivePath: forzaPath,
                MicrosoftGameConfigXml: MainGameConfig(
                    identityName: "Microsoft.624F8B84B80",
                    executableName: "ForzaHorizon5.exe",
                    storeId: "9NNX1VVR3KNQ",
                    titleId: "1234ABCD")),

            // StoreId is optional in MicrosoftGame.config. Package family identity
            // must remain sufficient and the config Identity.Name is a safe label fallback.
            new(
                PackageFamilyName: "Microsoft.Grounded_8wekyb3d8bbwe",
                PackageFullName: "Microsoft.Grounded_2.1.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Microsoft.Grounded",
                DisplayName: "ms-resource:GroundedDisplayName",
                InstalledPath: groundedPath,
                EffectivePath: groundedPath,
                MicrosoftGameConfigXml: MainGameConfig(
                    identityName: "Microsoft.Grounded",
                    executableName: "Grounded.exe")),

            // Ordinary Store app: package identity alone is not positive game evidence.
            new(
                PackageFamilyName: "Microsoft.WindowsCalculator_8wekyb3d8bbwe",
                PackageFullName: "Microsoft.WindowsCalculator_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Microsoft.WindowsCalculator",
                DisplayName: "Calculator",
                InstalledPath: Path.Combine(root, "Calculator"),
                EffectivePath: Path.Combine(root, "Calculator"),
                MicrosoftGameConfigXml: null),

            // Framework/resource/bundle/optional packages are not the main playable title.
            ValidButNonMain("Framework.Game_8wekyb3d8bbwe", root, isFramework: true),
            ValidButNonMain("Resource.Game_8wekyb3d8bbwe", root, isResourcePackage: true),
            ValidButNonMain("Bundle.Game_8wekyb3d8bbwe", root, isBundle: true),
            ValidButNonMain("Optional.Game_8wekyb3d8bbwe", root, isOptional: true),

            // DLC semantics must not be promoted as a base game even when identity exists.
            new(
                PackageFamilyName: "Publisher.GameDlc_8wekyb3d8bbwe",
                PackageFullName: "Publisher.GameDlc_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Publisher.GameDlc",
                DisplayName: "Game DLC",
                InstalledPath: Path.Combine(root, "Dlc"),
                EffectivePath: Path.Combine(root, "Dlc"),
                MicrosoftGameConfigXml:
                    """
                    <Game configVersion="1">
                      <Identity Name="Publisher.GameDlc" Publisher="CN=Publisher" Version="1.0.0.0" />
                      <AllowedProducts><AllowedProduct>9NNX1VVR3KNQ</AllowedProduct></AllowedProducts>
                    </Game>
                    """),

            // Malformed / DTD-bearing manifests are not trusted evidence.
            new(
                PackageFamilyName: "Publisher.Malformed_8wekyb3d8bbwe",
                PackageFullName: "Publisher.Malformed_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Publisher.Malformed",
                DisplayName: "Malformed",
                InstalledPath: Path.Combine(root, "Malformed"),
                EffectivePath: Path.Combine(root, "Malformed"),
                MicrosoftGameConfigXml: "<Game><Identity Name='broken'"),
            new(
                PackageFamilyName: "Publisher.Unsafe_8wekyb3d8bbwe",
                PackageFullName: "Publisher.Unsafe_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Publisher.Unsafe",
                DisplayName: "Unsafe",
                InstalledPath: Path.Combine(root, "Unsafe"),
                EffectivePath: Path.Combine(root, "Unsafe"),
                MicrosoftGameConfigXml:
                    "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///c:/windows/win.ini'>]><Game><Identity Name='Publisher.Unsafe' Publisher='CN=Publisher'/><ExecutableList><Executable Name='&e;'/></ExecutableList></Game>"),

            // Stable package identity itself must be valid; paths/titles cannot rescue it.
            new(
                PackageFamilyName: "   ",
                PackageFullName: "Publisher.Invalid_1.0.0.0_x64__8wekyb3d8bbwe",
                PackageName: "Publisher.Invalid",
                DisplayName: "Invalid identity",
                InstalledPath: Path.Combine(root, "Invalid"),
                EffectivePath: Path.Combine(root, "Invalid"),
                MicrosoftGameConfigXml: MainGameConfig("Publisher.Invalid", "Invalid.exe"))
        };

        var provider = new FakeProvider(observations);
        _stage = "construct";
        var source = new MicrosoftStoreGdkGameDiscoverySource(provider);
        Require(provider.Calls == 0,
            "Constructing Microsoft Store discovery must be side-effect free and must not enumerate packages.");

        _stage = "discover";
        var candidates = await source.DiscoverAsync();

        _stage = "assert-discovery";
        Require(provider.Calls == 1,
            "Explicit Microsoft Store discovery must enumerate the package provider exactly once.");
        Require(source.SourceId == "microsoft-store-gdk" && source.Priority >= 70,
            "Microsoft Store GDK discovery must expose a stable source identity.");
        Require(candidates.Count == 2,
            "Only positively proven base GDK game packages should enter the catalog.");
        Require(candidates.Select(candidate => candidate.Identity.GameId)
                .SequenceEqual([
                    "xbox:microsoft.624f8b84b80_8wekyb3d8bbwe",
                    "xbox:microsoft.grounded_8wekyb3d8bbwe"
                ]),
            "Microsoft Store identity must be deterministic and based on normalized Package Family Name, not version/full-name/path/title.");

        var forza = candidates.Single(candidate =>
            candidate.Identity.GameId == "xbox:microsoft.624f8b84b80_8wekyb3d8bbwe");
        Require(forza.Identity.Name == "Forza Horizon 5"
                && forza.Identity.Launcher == GameLauncherKind.Xbox
                && forza.Identity.AdapterId == "generic"
                && forza.Identity.Engine == GameEngineKind.Unknown,
            "A proven Store GDK game remains a neutral game identity until a specialized adapter proves more.");
        Require(forza.Identity.InstallPaths.SequenceEqual([Path.GetFullPath(forzaPath)]),
            "Multiple package observations for one PFN must merge to the same proven effective install path.");
        Require(forza.Identity.Executables.Count == 0,
            "MicrosoftGame.config executable names are evidence of package configuration, not proof that DG can directly access/launch those files.");
        Require(forza.Confidence >= 0.98
                && forza.Evidence.Contains("MicrosoftGame.config", StringComparison.OrdinalIgnoreCase)
                && forza.Evidence.Contains("9NNX1VVR3KNQ", StringComparison.Ordinal)
                && forza.Evidence.Contains("1234ABCD", StringComparison.Ordinal)
                && forza.Evidence.Contains("ForzaHorizon5.exe", StringComparison.Ordinal)
                && forza.Evidence.Contains("Microsoft.624F8B84B80_1.0.0.0_x64__8wekyb3d8bbwe", StringComparison.Ordinal),
            "Package full-name, StoreId, TitleId and configured executable names must remain provenance/evidence rather than changing stable identity.");

        var grounded = candidates.Single(candidate =>
            candidate.Identity.GameId == "xbox:microsoft.grounded_8wekyb3d8bbwe");
        Require(grounded.Identity.Name == "Microsoft.Grounded",
            "An unresolved ms-resource display string must fall back to MicrosoftGame.config Identity.Name instead of leaking a resource URI or guessing from a folder.");
        Require(!grounded.Evidence.Contains("StoreId", StringComparison.OrdinalIgnoreCase),
            "StoreId is optional and must not be fabricated when the game config does not provide one.");

        _stage = "assert-cancellation";
        var cancellationProvider = new FakeProvider(observations);
        var cancellationSource = new MicrosoftStoreGdkGameDiscoverySource(cancellationProvider);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await ExpectThrowsAsync<OperationCanceledException>(
            () => cancellationSource.DiscoverAsync(cancelled.Token),
            "Microsoft Store discovery must honor cancellation before touching the Windows package provider.");
        Require(cancellationProvider.Calls == 0,
            "A pre-cancelled Microsoft Store discovery request must not enumerate packages.");

        _stage = "complete";
        Console.WriteLine("PASS Track 3 Microsoft Store GDK discovery uses PFN identity and positive MicrosoftGame.config evidence");
    }

    private static MicrosoftStoreGamePackageObservation ValidButNonMain(
        string packageFamilyName,
        string root,
        bool isFramework = false,
        bool isResourcePackage = false,
        bool isBundle = false,
        bool isOptional = false)
        => new(
            PackageFamilyName: packageFamilyName,
            PackageFullName: packageFamilyName + "_1.0.0.0_x64__test",
            PackageName: packageFamilyName.Split('_')[0],
            DisplayName: "Not main",
            InstalledPath: Path.Combine(root, packageFamilyName),
            EffectivePath: Path.Combine(root, packageFamilyName),
            MicrosoftGameConfigXml: MainGameConfig(packageFamilyName.Split('_')[0], "Game.exe"),
            IsFramework: isFramework,
            IsResourcePackage: isResourcePackage,
            IsBundle: isBundle,
            IsOptional: isOptional);

    private static string MainGameConfig(
        string identityName,
        string executableName,
        string? storeId = null,
        string? titleId = null)
    {
        var store = storeId is null ? string.Empty : $"<StoreId>{storeId}</StoreId>";
        var title = titleId is null ? string.Empty : $"<TitleId>{titleId}</TitleId>";
        return $"""
               <?xml version="1.0" encoding="utf-8"?>
               <Game configVersion="1">
                 <Identity Name="{identityName}" Publisher="CN=Publisher" Version="1.0.0.0" />
                 {store}
                 {title}
                 <ExecutableList>
                   <Executable Name="{executableName}" Id="Game" TargetDeviceFamily="PC" />
                 </ExecutableList>
               </Game>
               """;
    }

    private sealed class FakeProvider : IMicrosoftStoreGamePackageProvider
    {
        private readonly IReadOnlyList<MicrosoftStoreGamePackageObservation> _observations;

        public FakeProvider(IReadOnlyList<MicrosoftStoreGamePackageObservation> observations)
            => _observations = observations;

        public int Calls { get; private set; }

        public Task<IReadOnlyList<MicrosoftStoreGamePackageObservation>> EnumerateAsync(
            CancellationToken cancellationToken = default)
        {
            Calls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_observations);
        }
    }

    private static async Task ExpectThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
