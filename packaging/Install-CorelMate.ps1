param(
    [string]$TargetVersion = ''
)

$ErrorActionPreference = 'Stop'
$dockerGuid = '{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'
$dockerClass = 'CorelMate.UI.CorelMatePanel'
$assemblyPath = Join-Path $PSScriptRoot 'CorelMate.UI.dll'

if (-not (Test-Path $assemblyPath -PathType Leaf)) {
    throw "CorelMate.UI.dll was not found beside this script. Extract the complete release package first."
}

Write-Host "=== CorelMate Suite Installer ===" -ForegroundColor Cyan
Write-Host "Detecting CorelDRAW installations (2017–2026)..." -ForegroundColor Cyan

$installations = @()
$regPaths = @("Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Corel\CorelDRAW\*", "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Corel\CorelDRAW\*")
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
                }
            }
        }
    }
}

if ($installations.Count -eq 0) {
    $progFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
    $fallback = Join-Path $progFiles "Corel\CorelDRAW Graphics Suite\27\Programs64"
    if (Test-Path $fallback) {
        $installations += [PSCustomObject]@{ Major = 27; Version = "27.0"; ProgramsDir = $fallback }
    }
}

if ($installations.Count -eq 0) {
    throw "No CorelDRAW installation detected. CorelMate requires CorelDRAW 2017 (v19) or newer."
}

$selected = $installations | Sort-Object Major -Descending | Select-Object -First 1
if ($TargetVersion) {
    $match = $installations | Where-Object { $_.Major -eq $TargetVersion -or $_.Version -like "*$TargetVersion*" } | Select-Object -First 1
    if ($match) { $selected = $match }
}

if ($selected.Major -lt 19) {
    throw "CorelDRAW v$($selected.Major) is not supported. CorelMate requires CorelDRAW 2017 (v19) through 2026."
}

Write-Host "Configuring CorelMate for CorelDRAW v$($selected.Major)..." -ForegroundColor Green

# 1. Register Native Menu Addon
$xsltContent = @'
<?xml version="1.0"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:frmwrk="Corel Framework Data"
    exclude-result-prefixes="frmwrk">
  <xsl:output method="xml" encoding="UTF-8" indent="yes"/>
  <frmwrk:uiconfig>
    <frmwrk:applicationInfo name="CorelDRAW" framework="CorelDRAW" userConfiguration="true" />
  </frmwrk:uiconfig>
  <xsl:template match="node()|@*"><xsl:copy><xsl:apply-templates select="node()|@*"/></xsl:copy></xsl:template>
  <xsl:template match="//commandBarData[@guid='f3016f3c-2847-4557-b61a-a2d05319cf18']/menubar/modeData">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./item[@guidRef='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7'])">
        <item guidRef="a63f9c4c-4e91-menu-9ac9-3a15c783d5d7"/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
'@

# Target User AppData Addons (non-elevated)
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$userAddonRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 3 -ErrorAction SilentlyContinue
foreach ($dRoot in $userAddonRoots) {
    try {
        $uAddon = Join-Path $dRoot.FullName "Addons\CorelMate"
        if (-not (Test-Path $uAddon)) { New-Item -ItemType Directory -Path $uAddon -Force | Out-Null }
        [System.IO.File]::WriteAllBytes((Join-Path $uAddon "Coreldrw.addon"), @())
        [System.IO.File]::WriteAllText((Join-Path $uAddon "UserDraw.xslt"), $xsltContent, [System.Text.Encoding]::UTF8)
        Write-Host "✓ CorelMate menu registered in User AppData: $uAddon" -ForegroundColor Green
    } catch {}
}

# Also attempt system Addon
$addonDir = Join-Path $selected.ProgramsDir "Addons\CorelMate"
try {
    if (-not (Test-Path $addonDir)) { New-Item -ItemType Directory -Path $addonDir -Force | Out-Null }
    $marker = Join-Path $addonDir "Coreldrw.addon"
    if (-not (Test-Path $marker)) { [System.IO.File]::WriteAllBytes($marker, @()) }
    $xslt = Join-Path $addonDir "UserDraw.xslt"
    [System.IO.File]::WriteAllText($xslt, $xsltContent, [System.Text.Encoding]::UTF8)
    Write-Host "✓ CorelMate main navigation menu registered in Programs." -ForegroundColor Green
}
catch {
    # Non-elevated install: User AppData registration will be used
}

# 2. Register GMS macro
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$gmsDirs = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "GMS" -Recurse -Depth 4 -ErrorAction SilentlyContinue
foreach ($gDir in $gmsDirs) {
    try {
        $macroPath = Join-Path $gDir.FullName "CorelMate.gms"
        @'
Option Explicit
Private Const DOCKER_GUID As String = "{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}"
Public Sub ShowDashboard(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowBadges(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowCurves(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowInspector(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowCompatibility(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowSettings(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
'@ | Set-Content -Path $macroPath -Encoding ASCII
        Write-Host "✓ Companion macro registered in $($gDir.Name)" -ForegroundColor Green
    } catch {}
}

# 3. Register Docker with COM Automation
try {
    $app = New-Object -ComObject CorelDRAW.Application
    $app.Visible = $true
    if ($app.FrameWork.IsDockerVisible($dockerGuid)) {
        $app.FrameWork.ShowDocker($dockerGuid)
    }
    else {
        $app.FrameWork.AddDocker($dockerGuid, $dockerClass, $assemblyPath)
        $app.FrameWork.ShowDocker($dockerGuid)
    }
    Write-Host "✓ CorelMate Docker shown in CorelDRAW $($app.Version)." -ForegroundColor Green
}
catch {
    Write-Warning "CorelDRAW was not running; CorelMate will initialize automatically on next CorelDRAW startup."
}

Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
