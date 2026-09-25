param([string]$Configuration = "Release")
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$nativeBuild = Join-Path $root "build/native"
$publish = Join-Path $root "artifacts/FFPerformanceEngine"
cmake -S "$root/src/FFPerformanceEngine.Native" -B $nativeBuild -A x64
cmake --build $nativeBuild --config $Configuration
ctest --test-dir $nativeBuild -C $Configuration --output-on-failure
dotnet build "$root/FFPerformanceEngine.sln" -c $Configuration
dotnet run --project "$root/tests/FFPerformanceEngine.Core.SelfTest/FFPerformanceEngine.Core.SelfTest.csproj" -c $Configuration
dotnet publish "$root/src/FFPerformanceEngine.App/FFPerformanceEngine.App.csproj" -c $Configuration -r win-x64 --self-contained false -o $publish
Copy-Item "$nativeBuild/$Configuration/ffpe_native.dll" $publish -Force
Write-Host "Build ready: $publish"
