param(
    [string]$Configuration = 'Release',
    [string]$TargetVersion = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'CorelMate.sln'
$uiAssembly = Join-Path $root "src\CorelMate.UI\bin\$Configuration\net48\CorelMate.UI.dll"
$coreAssembly = Join-Path $root "src\CorelMate.Core\bin\$Configuration\netstandard2.0\CorelMate.Core.dll"
$hostAssembly = Join-Path $root "src\CorelMate.Host\bin\$Configuration\net48\CorelMate.Host.dll"

Write-Host "Building CorelMate ($Configuration)..." -ForegroundColor Cyan
dotnet build $solution -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "CorelMate build failed." }

Write-Host "Detecting installed CorelDRAW suites (2017 to 2026)..." -ForegroundColor Cyan
$installations = @()
$regPaths = @(
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Corel\CorelDRAW\*",
    "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Corel\CorelDRAW\*"
)
foreach ($regPath in $regPaths) {
    Get-ItemProperty -Path $regPath -ErrorAction SilentlyContinue | ForEach-Object {
        $ver = $_.PSChildName
        $progDir = $_.ProgramsDir
        if (-not $progDir -and $_.PluginDir) {
            $progDir = Join-Path (Split-Path $_.PluginDir -Parent) "Programs64"
        }
        if ($progDir -and (Test-Path $progDir)) {
            $major = 0
            if ([double]::TryParse($ver, [ref]$major)) {
                $majorInt = [int][Math]::Floor($major)
                $installations += [PSCustomObject]@{
                    Major = $majorInt
                    Version = $ver
                    ProgramsDir = $progDir
                    Executable = Join-Path $progDir "CorelDRW.exe"
                }
            }
        }
    }
}

if ($installations.Count -eq 0) {
    $progFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    $defaultProgDir = Join-Path $progFiles "Corel\CorelDRAW Graphics Suite\27\Programs64"
    if (Test-Path $defaultProgDir) {
        $installations += [PSCustomObject]@{
            Major = 27
            Version = "27.0"
            ProgramsDir = $defaultProgDir
            Executable = Join-Path $defaultProgDir "CorelDRW.exe"
        }
    }
}

if ($installations.Count -eq 0) {
    throw "No supported CorelDRAW installation found. CorelMate requires CorelDRAW 2017 (v19) or newer."
}

# Select target installation
$selected = $installations | Sort-Object Major -Descending | Select-Object -First 1
if ($TargetVersion) {
    $match = $installations | Where-Object { $_.Major -eq $TargetVersion -or $_.Version -like "*$TargetVersion*" } | Select-Object -First 1
    if ($match) { $selected = $match }
}

Write-Host "Targeting CorelDRAW v$($selected.Major) at '$($selected.ProgramsDir)'" -ForegroundColor Green

# 1. Deploy CorelMate.gms and companion macros
Write-Host "Deploying CorelMate native companion macros into Draw\GMS and GlobalMacros..." -ForegroundColor Cyan
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)

# Deploy precompiled CorelMate.gms directly into all Draw\GMS folders
$srcCorelMateGms = Join-Path $root "assets\gms\CorelMate.gms"
if (Test-Path $srcCorelMateGms) {
    $drawRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 4 -ErrorAction SilentlyContinue
    foreach ($dRoot in $drawRoots) {
        try {
            $gmsFolder = Join-Path $dRoot.FullName "GMS"
            if (-not (Test-Path $gmsFolder)) { New-Item -ItemType Directory -Path $gmsFolder -Force | Out-Null }
            $destGms = Join-Path $gmsFolder "CorelMate.gms"
            Copy-Item -Path $srcCorelMateGms -Destination $destGms -Force
            Write-Host "  [OK] Deployed CorelMate.gms to: $destGms" -ForegroundColor Green
        } catch {}
    }
}

# Copy pre-compiled GlobalMacros.gms asset
$srcGlobalMacros = Join-Path $root "assets\gms\GlobalMacros.gms"
if (Test-Path $srcGlobalMacros) {
    $drawRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 4 -ErrorAction SilentlyContinue
    foreach ($dRoot in $drawRoots) {
        try {
            $destGm = Join-Path $dRoot.FullName "GlobalMacros.gms"
            Copy-Item -Path $srcGlobalMacros -Destination $destGm -Force
            Write-Host "  [OK] Deployed GlobalMacros.gms to: $destGm" -ForegroundColor Green
        } catch {}
    }
}

# Live VBA import if CorelDRAW is running
try {
    $type = [Type]::GetTypeFromProgID("CorelDRAW.Application")
    $app = [Activator]::CreateInstance($type)
    $app.InitializeVBA()
    $basFile = Join-Path $root "assets\vba\CorelMate.bas"
    if (Test-Path $basFile) {
        foreach ($vp in $app.VBE.VBProjects) {
            if ($vp.Name -eq "GlobalMacros" -or $vp.Name -eq "CorelMate") {
                foreach ($c in $vp.VBComponents) {
                    if ($c.Name -eq "CorelMate" -or $c.Name -eq "Workflows") {
                        $vp.VBComponents.Remove($c)
                        break
                    }
                }
                $vp.VBComponents.Import($basFile) | Out-Null
                Write-Host "  [OK] Live CorelMate VBA module imported into active $($vp.Name) project" -ForegroundColor Green
            }
        }
    }
} catch {}

# 2. Patch active user workspaces (.cdws) to remove Docker and add 3 Navbar menus
Write-Host "Patching CorelDRAW workspaces (removing Docker, adding direct Navbar menus)..." -ForegroundColor Cyan
Add-Type -Path $coreAssembly
Add-Type -Path $hostAssembly
$patchMsg = ""
[bool]$patched = [CorelMate.Core.CorelWorkspacePatcher]::PatchAllUserWorkspaces($uiAssembly, [ref]$patchMsg)
Write-Host "  $patchMsg" -ForegroundColor $(if ($patched) { "Green" } else { "Yellow" })

# 3. Deploy UserDraw.xslt Addon to User AppData and Program Files
Write-Host "Deploying CorelMate native UI Addon (UserDraw.xslt)..." -ForegroundColor Cyan
$xsltContent = [CorelMate.Host.CorelMenuIntegration]::GenerateUserDrawXslt()

$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$userAddonRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 4 -ErrorAction SilentlyContinue
foreach ($dRoot in $userAddonRoots) {
    try {
        $uAddon = Join-Path $dRoot.FullName "Addons\CorelMate"
        if (-not (Test-Path $uAddon)) { New-Item -ItemType Directory -Path $uAddon -Force | Out-Null }
        [System.IO.File]::WriteAllBytes((Join-Path $uAddon "Coreldrw.addon"), @())
        [System.IO.File]::WriteAllText((Join-Path $uAddon "UserDraw.xslt"), $xsltContent, [System.Text.Encoding]::UTF8)
        Write-Host "  [OK] CorelMate Addon deployed to User AppData: $uAddon" -ForegroundColor Green
    } catch {}
}

# System Addon (if permissions allow)
$addonDir = Join-Path $selected.ProgramsDir "Addons\CorelMate"
try {
    if (-not (Test-Path $addonDir)) { New-Item -ItemType Directory -Path $addonDir -Force | Out-Null }
    $marker = Join-Path $addonDir "Coreldrw.addon"
    if (-not (Test-Path $marker)) { [System.IO.File]::WriteAllBytes($marker, @()) }
    $xslt = Join-Path $addonDir "UserDraw.xslt"
    [System.IO.File]::WriteAllText($xslt, $xsltContent, [System.Text.Encoding]::UTF8)
    Write-Host "  [OK] CorelMate Addon deployed to Programs: $addonDir" -ForegroundColor Green
} catch {}

# 4. Hide/Remove Docker if CorelDRAW is running
try {
    $app = [System.Runtime.InteropServices.Marshal]::GetActiveObject('CorelDRAW.Application')
    if ($app) {
        $dockerGuid = '{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'
        if ($app.FrameWork.IsDockerVisible($dockerGuid)) {
            $app.FrameWork.HideDocker($dockerGuid)
        }
        $app.FrameWork.RemoveDocker($dockerGuid)
        Write-Host "  [OK] CorelMate Docker closed and removed from active CorelDRAW instance." -ForegroundColor Green
    }
} catch {
    # CorelDRAW not currently running; workspace and addon changes take effect on launch
}

Write-Host "`n=== Installation Complete! ===" -ForegroundColor Green
Write-Host "CorelMate single Navbar menu installed before Help:" -ForegroundColor Cyan
Write-Host "  CorelMate (Navbar immediately before Help)" -ForegroundColor White
Write-Host "    -> Curves: Curve Current Page | Curve All Pages | Curve Selected Objects" -ForegroundColor White
Write-Host "    -> Export: Export As ID Card | Export As Print" -ForegroundColor White
Write-Host "    -> Color:  Convert to CMYK | Convert to RGB" -ForegroundColor White
Write-Host "  (All operations execute directly with confirmation dialogs; Docker is completely removed)`n" -ForegroundColor Gray
