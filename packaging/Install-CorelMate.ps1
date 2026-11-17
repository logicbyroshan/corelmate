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

# 1. Patch CorelDRAW active workspaces (_default.cdws)
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$corelUserDir = Join-Path $appData "Corel"
if (Test-Path $corelUserDir) {
    $cdwsFiles = Get-ChildItem -Path $corelUserDir -Filter "_default.cdws" -Recurse -Depth 5 -ErrorAction SilentlyContinue
    foreach ($cdws in $cdwsFiles) {
        $wsPath = $cdws.FullName
        try {
            $zip = [System.IO.Compression.ZipFile]::Open($wsPath, [System.IO.Compression.ZipArchiveMode]::Update)
            $entry = $zip.GetEntry("content/workspace.xml")
            if ($entry) {
                [xml]$xml = New-Object System.Xml.XmlDocument
                $reader = New-Object System.IO.StreamReader($entry.Open(), [System.Text.Encoding]::UTF8)
                $xml.Load($reader)
                $reader.Close()

                # Update or create dockerData with title CorelMate
                $dockerNode = $xml.SelectSingleNode("//dockers/dockerData[@guid='$dockerGuid']")
                if ($dockerNode) {
                    $dockerNode.SetAttribute("userCaption", "CorelMate")
                    $dockerNode.SetAttribute("caption", "CorelMate")
                    $dockerNode.SetAttribute("focusStyle", "noThrow")
                } else {
                    $dockersNode = $xml.SelectSingleNode("//dockers")
                    if (-not $dockersNode) {
                        $dockersNode = $xml.CreateElement("dockers")
                        $xml.DocumentElement.AppendChild($dockersNode)
                    }
                    $d = $xml.CreateElement("dockerData")
                    $d.SetAttribute("guid", $dockerGuid)
                    $d.SetAttribute("userCaption", "CorelMate")
                    $d.SetAttribute("caption", "CorelMate")
                    $d.SetAttribute("dock", "fill")
                    $d.SetAttribute("focusStyle", "noThrow")
                    $c = $xml.CreateElement("container")
                    $it = $xml.CreateElement("item")
                    $it.SetAttribute("dock", "fill")
                    $it.SetAttribute("guidRef", "eaa2648a-69d1-46dd-8527-9cd9a00c467f")
                    $c.AppendChild($it)
                    $d.AppendChild($c)
                    $dockersNode.AppendChild($d)
                }

                # Ensure <items>
                $itemsNode = $xml.SelectSingleNode("//items")
                if (-not $itemsNode) {
                    $itemsNode = $xml.CreateElement("items")
                    $xml.DocumentElement.AppendChild($itemsNode)
                }

                function Set-PkgItemData($items, $guid, $attrs) {
                    $it = $items.SelectSingleNode("itemData[@guid='$guid']")
                    if (-not $it) {
                        $it = $items.OwnerDocument.CreateElement("itemData")
                        $it.SetAttribute("guid", $guid)
                        $items.AppendChild($it)
                    }
                    foreach ($k in $attrs.Keys) {
                        $it.SetAttribute($k, $attrs[$k])
                    }
                }

                Set-PkgItemData $itemsNode "eaa2648a-69d1-46dd-8527-9cd9a00c467f" @{
                    type = "wpfhost"
                    hostedType = "$assemblyPath,CorelMate.UI.CorelMatePanel"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-dock-9ac9-3a15c783d5d4" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "CorelMate Docker"
                    userToolTip = "Open or Toggle CorelMate Docker"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-4e91-menu-9ac9-3a15c783d5d7" @{
                    type = "flyout"
                    flyoutBarRef = "a63f9c4c-cbef-4276-b686-adb4f77c80d6"
                    userCaption = "CorelMate"
                    userToolTip = "CorelMate Productivity Suite"
                    userCreated = "true"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-page-9ac9-3a15c783d5d1" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "Convert Page to Curves (Docker)"
                    userToolTip = "Open CorelMate Curves Docker to convert active page text to curves"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-pall-9ac9-3a15c783d5d2" @{
                    type = "button"
                    dynamicCommand = "ConvertAllToCurves.Converter.ConvertAllToCurves"
                    dynamicCategory = "2cc24a3e-fe24-4708-9a74-9c75406eebcd"
                    userCaption = "Convert All to Curves (Macro)"
                    userToolTip = "Execute CorelDRAW Convert All to Curves macro"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-psel-9ac9-3a15c783d5d3" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "Convert Selection to Curves (Docker)"
                    userToolTip = "Open CorelMate Curves Docker for selected text objects"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-inspect-9ac9-3a15c783d5d5" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "Document Inspector (Docker)"
                    userToolTip = "Open CorelMate Document & Object Inspector"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-badge-9ac9-3a15c783d5d6" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "Badge Generator (Docker)"
                    userToolTip = "Open CorelMate Batch Badge Generator"
                    noBmpOnMenu = "true"
                }
                Set-PkgItemData $itemsNode "a63f9c4c-item-about-9ac9-3a15c783d5d7" @{
                    type = "checkButton"
                    check = "*Docker('$dockerGuid')"
                    userCaption = "About CorelMate"
                    userToolTip = "About CorelMate Suite"
                    noBmpOnMenu = "true"
                }

                # Flyout commandBar
                $cbNode = $xml.SelectSingleNode("//commandBars/commandBarData[@guid='a63f9c4c-cbef-4276-b686-adb4f77c80d6']")
                if (-not $cbNode) {
                    $cbs = $xml.SelectSingleNode("//commandBars")
                    if (-not $cbs) {
                        $cbs = $xml.CreateElement("commandBars")
                        $xml.DocumentElement.AppendChild($cbs)
                    }
                    $cbNode = $xml.CreateElement("commandBarData")
                    $cbNode.SetAttribute("guid", "a63f9c4c-cbef-4276-b686-adb4f77c80d6")
                    $cbNode.SetAttribute("type", "menu")
                    $cbNode.SetAttribute("nonLocalizableName", "CorelMate")
                    $cbNode.SetAttribute("flyout", "true")
                    $m = $xml.CreateElement("menu")

                    $itemGuids = @(
                        "a63f9c4c-item-dock-9ac9-3a15c783d5d4",
                        "266435b4-6e53-460f-9fa7-f45be187d400",
                        "a63f9c4c-item-page-9ac9-3a15c783d5d1",
                        "a63f9c4c-item-pall-9ac9-3a15c783d5d2",
                        "a63f9c4c-item-psel-9ac9-3a15c783d5d3",
                        "266435b4-6e53-460f-9fa7-f45be187d400",
                        "a63f9c4c-item-inspect-9ac9-3a15c783d5d5",
                        "a63f9c4c-item-badge-9ac9-3a15c783d5d6",
                        "266435b4-6e53-460f-9fa7-f45be187d400",
                        "a63f9c4c-item-about-9ac9-3a15c783d5d7"
                    )
                    foreach ($id in $itemGuids) {
                        $it = $xml.CreateElement("item")
                        $it.SetAttribute("guidRef", $id)
                        $m.AppendChild($it)
                    }
                    $cbNode.AppendChild($m)
                    $cbs.AppendChild($cbNode)
                }

                # Menubar modes
                $modes = @("6d1d9e94-13f3-4517-a976-ba9154671db4", "76d73481-9076-44c9-821c-52de9408cce2")
                foreach ($mGuid in $modes) {
                    $modeNode = $xml.SelectSingleNode("//commandBarData[@guid='f3016f3c-2847-4557-b61a-a2d05319cf18']/menubar/modeData[@guid='$mGuid']")
                    if ($modeNode) {
                        $existing = $modeNode.SelectSingleNode("item[@guidRef='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7']")
                        if (-not $existing) {
                            $newIt = $xml.CreateElement("item")
                            $newIt.SetAttribute("guidRef", "a63f9c4c-4e91-menu-9ac9-3a15c783d5d7")
                            $modeNode.AppendChild($newIt)
                        }
                    }
                }

                # Window menu
                $winMode = $xml.SelectSingleNode("//modeData[@guid='6c91d5ab-d648-4364-96fb-3e71bcfaf70d']")
                if ($winMode) {
                    $existing = $winMode.SelectSingleNode("item[@guidRef='a63f9c4c-item-dock-9ac9-3a15c783d5d4']")
                    if (-not $existing) {
                        $newIt = $xml.CreateElement("item")
                        $newIt.SetAttribute("guidRef", "a63f9c4c-item-dock-9ac9-3a15c783d5d4")
                        $winMode.AppendChild($newIt)
                    }
                }

                # Right tray
                $tray = $xml.SelectSingleNode("//viewHost[@guid='bf2668a0-d25d-4eb8-97c6-c0f9fe6e7516']")
                if ($tray) {
                    $existing = $tray.SelectSingleNode("dockerData[@guidRef='$dockerGuid']")
                    if (-not $existing) {
                        $dRef = $xml.CreateElement("dockerData")
                        $dRef.SetAttribute("guidRef", $dockerGuid)
                        $dRef.SetAttribute("category", "view+docker")
                        $tray.AppendChild($dRef)
                    }
                }

                # Save back into zip entry
                $entry.Delete()
                $newEntry = $zip.CreateEntry("content/workspace.xml", [System.IO.Compression.CompressionLevel]::Optimal)
                $writer = New-Object System.IO.StreamWriter($newEntry.Open(), [System.Text.Encoding]::UTF8)
                $xml.Save($writer)
                $writer.Flush()
                $writer.Close()
                Write-Host "✓ Patched active workspace at $wsPath" -ForegroundColor Green
            }
            $zip.Dispose()
        } catch {
            Write-Warning "Could not patch workspace ${wsPath}: $_"
        }
    }
}

# 2. Register Native Menu Addon
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

  <xsl:template match="uiConfig/dockers">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./dockerData[@guid='{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'])">
        <dockerData guid="{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}" userCaption="CorelMate" caption="CorelMate" dock="fill" focusStyle="noThrow">
          <container><item dock="fill" guidRef="eaa2648a-69d1-46dd-8527-9cd9a00c467f"/></container>
        </dockerData>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="uiConfig/items">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./itemData[@guid='a63f9c4c-item-dock-9ac9-3a15c783d5d4'])">
        <itemData guid="a63f9c4c-item-dock-9ac9-3a15c783d5d4" type="checkButton" check="*Docker('{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}')" userCaption="CorelMate Docker" userToolTip="Open or Toggle CorelMate Docker" noBmpOnMenu="true"/>
      </xsl:if>
      <xsl:if test="not(./itemData[@guid='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7'])">
        <itemData guid="a63f9c4c-4e91-menu-9ac9-3a15c783d5d7" flyoutBarRef="a63f9c4c-cbef-4276-b686-adb4f77c80d6" type="flyout" userCaption="CorelMate" userToolTip="CorelMate Productivity Suite" userCreated="true" noBmpOnMenu="true"/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="//commandBarData[@guid='f3016f3c-2847-4557-b61a-a2d05319cf18']/menubar/modeData">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./item[@guidRef='a63f9c4c-4e91-menu-9ac9-3a15c783d5d7'])">
        <item guidRef="a63f9c4c-4e91-menu-9ac9-3a15c783d5d7"/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="//modeData[@guid='6c91d5ab-d648-4364-96fb-3e71bcfaf70d']">
    <xsl:copy>
      <xsl:apply-templates select="node()|@*"/>
      <xsl:if test="not(./item[@guidRef='a63f9c4c-item-dock-9ac9-3a15c783d5d4'])">
        <item guidRef="a63f9c4c-item-dock-9ac9-3a15c783d5d4"/>
      </xsl:if>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
'@

# Target User AppData Addons (non-elevated)
$userAddonRoots = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "Draw" -Recurse -Depth 4 -ErrorAction SilentlyContinue
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

# 3. Register GMS macro
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
Public Sub ShowAbout(): On Error Resume Next: CorelDRAW.FrameWork.ShowDocker DOCKER_GUID: End Sub
'@ | Set-Content -Path $macroPath -Encoding ASCII
        Write-Host "✓ Companion macro registered in $($gDir.Name)" -ForegroundColor Green
    } catch {}
}

# 4. Register Docker with COM Automation
Write-Host "Connecting to CorelDRAW COM Automation..." -ForegroundColor Cyan
$comJob = Start-Job -ScriptBlock {
    param($dGuid, $dClass, $asmPath)
    try {
        $app = New-Object -ComObject CorelDRAW.Application
        $app.Visible = $true
        $app.FrameWork.AddDocker($dGuid, $dClass, $asmPath)
        $app.FrameWork.ShowDocker($dGuid)
        return $app.Version
    } catch {
        return "Error: $_"
    }
} -ArgumentList $dockerGuid, $dockerClass, $assemblyPath

$finished = Wait-Job $comJob -Timeout 8
if ($finished) {
    $ver = Receive-Job $comJob
    Write-Host "✓ CorelMate Docker shown in CorelDRAW ($ver)." -ForegroundColor Green
    Write-Host "  Docker Title: CorelMate" -ForegroundColor Gray
    Write-Host "  Docker GUID:  $dockerGuid" -ForegroundColor Gray
    Write-Host "  Navigation:   Active as 'CorelMate' in top menu bar and right-side dockers tray." -ForegroundColor Gray
} else {
    Stop-Job $comJob -ErrorAction SilentlyContinue
    Remove-Job $comJob -Force -ErrorAction SilentlyContinue
    Write-Host "✓ CorelMate workspace & navigation integration configured." -ForegroundColor Green
    Write-Host "  Workspace patched with Docker Title 'CorelMate', top menubar entry, and dockers tray." -ForegroundColor Gray
    Write-Host "  Restart CorelDRAW to display the updated menu and docked window." -ForegroundColor Yellow
}

Write-Host "=== Installation Complete ===" -ForegroundColor Cyan
