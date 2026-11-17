param()

$ErrorActionPreference = 'SilentlyContinue'
$dockerGuid = '{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'

Write-Host "Uninstalling CorelMate..." -ForegroundColor Cyan

# 1. COM unregistration
try {
    $app = [System.Runtime.InteropServices.Marshal]::GetActiveObject('CorelDRAW.Application')
    if ($app) {
        if ($app.FrameWork.IsDockerVisible($dockerGuid)) {
            $app.FrameWork.HideDocker($dockerGuid)
        }
        $app.FrameWork.RemoveDocker($dockerGuid)
        Write-Host "Docker removed from running CorelDRAW instance." -ForegroundColor Green
    }
}
catch {
    Write-Host "No active CorelDRAW instance found." -ForegroundColor Gray
}

# 2. Remove Addons directory
$progDir = "C:\Program Files\Corel\CorelDRAW Graphics Suite\27\Programs64\Addons\CorelMate"
if (Test-Path $progDir) {
    try {
        Remove-Item $progDir -Recurse -Force
        Write-Host "CorelMate Addon directory removed." -ForegroundColor Green
    } catch {}
}

# 3. Remove GMS macro companion
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$gmsFiles = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "CorelMate.gms" -Recurse -Depth 5 -ErrorAction SilentlyContinue
foreach ($f in $gmsFiles) {
    try {
        Remove-Item $f.FullName -Force
        Write-Host "Removed macro: $($f.FullName)" -ForegroundColor Green
    } catch {}
}

Write-Host "✓ CorelMate uninstallation complete." -ForegroundColor Green
