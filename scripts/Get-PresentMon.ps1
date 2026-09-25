param([string]$Destination = "$env:LOCALAPPDATA\FFPerformanceEngine\tools")
$ErrorActionPreference = 'Stop'
$version = '2.5.1'
$name = "PresentMon-$version-x64.exe"
$url = "https://github.com/GameTechDev/PresentMon/releases/download/v$version/$name"
$expected = '9bec3083069f58f911e6a512f4806db51a27bd096103087bc1d05ef54c80a191'
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$target = Join-Path $Destination $name
Invoke-WebRequest -Uri $url -OutFile $target
$actual = (Get-FileHash -Algorithm SHA256 -Path $target).Hash.ToLowerInvariant()
if ($actual -ne $expected) { Remove-Item $target -Force; throw "PresentMon SHA256 mismatch: $actual" }
Write-Host "PresentMon $version installed and verified at $target"
