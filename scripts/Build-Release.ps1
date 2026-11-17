param(
    [string]$Configuration = 'Release',
    [string]$Version = '0.2.0'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'CorelMate.sln'
$outputRoot = Join-Path $root 'artifacts'
$staging = Join-Path $outputRoot "CorelMate-$Version"
$package = Join-Path $outputRoot "CorelMate-$Version-docker.zip"
$uiAssembly = Join-Path $root "src\CorelMate.UI\bin\$Configuration\net48\CorelMate.UI.dll"
$hostOutput = Join-Path $root "src\CorelMate.Host\bin\$Configuration\net48"
$managedAssemblies = @(
  'CorelMate.UI.dll',
  'CorelMate.Host.dll',
  'CorelMate.Badges.dll',
  'CorelMate.Infrastructure.dll',
  'CorelMate.Core.dll',
  'CorelMate.Curves.dll'
)

if (Test-Path $outputRoot) { Remove-Item $outputRoot -Recurse -Force }
New-Item $staging -ItemType Directory -Force | Out-Null

Get-ChildItem $root -Directory -Recurse -Force | Where-Object { $_.Name -in @('bin', 'obj') } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { throw 'CorelMate restore failed.' }

dotnet build $solution -c $Configuration --no-restore -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { throw 'CorelMate build failed.' }

dotnet run --project (Join-Path $root 'tests\CorelMate.Tests\CorelMate.Tests.csproj') -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { throw 'CorelMate tests failed.' }

if (-not (Test-Path $uiAssembly -PathType Leaf)) { throw "Required Docker assembly was not built: $uiAssembly" }
foreach ($assemblyName in $managedAssemblies)
{
  $assemblyPath = if ($assemblyName -eq 'CorelMate.UI.dll') { $uiAssembly } else { Join-Path $hostOutput $assemblyName }
  if (-not (Test-Path $assemblyPath -PathType Leaf)) { throw "Required package assembly was not built: $assemblyPath" }
  Copy-Item $assemblyPath (Join-Path $staging $assemblyName)
}
Copy-Item (Join-Path $root 'packaging\Install-CorelMate.ps1') $staging
Copy-Item (Join-Path $root 'packaging\Uninstall-CorelMate.ps1') $staging

@"
CorelMate 0.2.0 Release Package

Targets: CorelDRAW Graphics Suite 2017 through 2026 (v19 to v28)
Features:
- Version-Adaptive UI (2017 Classic, 2019 Charcoal, 2021 Teal, 2023 Azure, 2026 Adaptive Pro)
- Native CorelDRAW Main Navigation Menu Integration [CorelMate]
- Batch Badge Generator (CSV/XLSX import, multi-page planning)
- Convert Text to Curves (safe preflight, locked/hidden preservation)
- Document Inspector (shapes breakdown, color model, font analysis)
- Central Compatibility Registry (v19 to v28 validation)

Install:
  powershell -ExecutionPolicy Bypass -File .\Install-CorelMate.ps1

Uninstall:
  powershell -ExecutionPolicy Bypass -File .\Uninstall-CorelMate.ps1

This package intentionally does not contain Corel-owned interop assemblies.
CorelDRAW supplies those assemblies.
"@ | Set-Content (Join-Path $staging 'README.txt') -Encoding ASCII

$files = @(Get-ChildItem $staging -File)
if ($files.Name -contains 'Corel.Interop.CorelDRAW.dll' -or $files.Name -contains 'Corel.Interop.VGCore.dll') { throw 'Corel-owned interop assemblies must not be packaged.' }
if ($files.Name | Where-Object { $_ -match '\.pdb$|\.user$|\.secret|appsettings|local' }) { throw 'Development-only or local files detected in package.' }
$content = $files | ForEach-Object { Get-Content $_.FullName -Raw -ErrorAction Stop }
if ($content | Where-Object { $_ -match 'C:\\Users\\|(?<![A-Za-z])[A-Za-z]:\\|Program Files' }) { throw 'Absolute developer path detected in package contents.' }
foreach ($assemblyName in $managedAssemblies) { if (-not ($files.Name -contains $assemblyName)) { throw "$assemblyName is missing from package." } }

Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $package -CompressionLevel Optimal
Remove-Item $staging -Recurse -Force
Write-Output "Release package created: $package"