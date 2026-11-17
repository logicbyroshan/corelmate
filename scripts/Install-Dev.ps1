param(
    [string]$Configuration = 'Release',
    [string]$TargetVersion = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'CorelMate.sln'
$uiAssembly = Join-Path $root "src\CorelMate.UI\bin\$Configuration\net48\CorelMate.UI.dll"
$hostAssembly = Join-Path $root "src\CorelMate.Host\bin\$Configuration\net48\CorelMate.Host.dll"
$dockerGuid = '{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'
$dockerClass = 'CorelMate.UI.CorelMatePanel'

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

# 1. Deploy CorelMate Addon for Native Main Navigation Menu
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

  <xsl:template match="node()|@*">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
    </xsl:copy>
  </xsl:template>

  <!-- Append CorelMate as LAST main menu item in menubar -->
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

# Target user AppData Addons directory (no admin rights needed)
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$userAddonRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 3 -ErrorAction SilentlyContinue
foreach ($dRoot in $userAddonRoots) {
    try {
        $uAddon = Join-Path $dRoot.FullName "Addons\CorelMate"
        if (-not (Test-Path $uAddon)) { New-Item -ItemType Directory -Path $uAddon -Force | Out-Null }
        [System.IO.File]::WriteAllBytes((Join-Path $uAddon "Coreldrw.addon"), @())
        [System.IO.File]::WriteAllText((Join-Path $uAddon "UserDraw.xslt"), $xsltContent, [System.Text.Encoding]::UTF8)
        Write-Host "CorelMate native Addon registered in User AppData: $uAddon" -ForegroundColor Green
    } catch {}
}

# Also attempt Program Files Addons if elevated
$addonDir = Join-Path $selected.ProgramsDir "Addons\CorelMate"
try {
    if (-not (Test-Path $addonDir)) { New-Item -ItemType Directory -Path $addonDir -Force | Out-Null }
    $marker = Join-Path $addonDir "Coreldrw.addon"
    if (-not (Test-Path $marker)) { [System.IO.File]::WriteAllBytes($marker, @()) }
    $xslt = Join-Path $addonDir "UserDraw.xslt"
    [System.IO.File]::WriteAllText($xslt, $xsltContent, [System.Text.Encoding]::UTF8)
    Write-Host "CorelMate native Addon registered in Programs: $addonDir" -ForegroundColor Green
}
catch {
    # System Program Files is protected, user AppData registration takes precedence
}

# 2. Deploy GMS Macro Companion to User AppData
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$gmsDirs = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "GMS" -Recurse -Depth 4 -ErrorAction SilentlyContinue
$macroContent = @'
Option Explicit
Private Const DOCKER_GUID As String = "{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}"
Public Sub ShowDashboard(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowBadges(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowCurves(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowInspector(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowCompatibility(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
Public Sub ShowSettings(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
'@

foreach ($gDir in $gmsDirs) {
    try {
        $macroPath = Join-Path $gDir.FullName "CorelMate.gms"
        [System.IO.File]::WriteAllText($macroPath, $macroContent, [System.Text.Encoding]::ASCII)
        Write-Host "GMS Macro companion deployed to $($gDir.FullName)" -ForegroundColor Green
    } catch {}
}

# 3. Connect to Running CorelDRAW or register Docker via COM
Write-Host "Connecting to CorelDRAW COM Automation..." -ForegroundColor Cyan
$app = New-Object -ComObject CorelDRAW.Application
$app.Visible = $true
$app.FrameWork.AddDocker($dockerGuid, $dockerClass, $uiAssembly)
$app.FrameWork.ShowDocker($dockerGuid)

Write-Host "CorelMate successfully installed & shown in CorelDRAW $($app.Version)!" -ForegroundColor Green
Write-Host "Docker GUID: $dockerGuid" -ForegroundColor Gray
Write-Host "CorelMate navigation menu active as last main menu item." -ForegroundColor Gray
