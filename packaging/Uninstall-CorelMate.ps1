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
        Write-Host "✓ CorelMate Docker removed from current CorelDRAW session." -ForegroundColor Green
    }
}
catch {
    Write-Host "No active CorelDRAW instance running." -ForegroundColor Gray
}

# 2. Remove Addons directory
$progFiles = [Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFiles)
$progPaths = @(
    (Join-Path $progFiles "Corel\CorelDRAW Graphics Suite\27\Programs64\Addons\CorelMate")
)
foreach ($p in $progPaths) {
    if (Test-Path $p) {
        Remove-Item $p -Recurse -Force
        Write-Host "✓ Removed Addon from $p" -ForegroundColor Green
    }
}

# 3. Remove GMS macros
$appData = [Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)
$gmsFiles = Get-ChildItem -Path (Join-Path $appData "Corel") -Filter "CorelMate.gms" -Recurse -Depth 5 -ErrorAction SilentlyContinue
foreach ($f in $gmsFiles) {
    Remove-Item $f.FullName -Force
    Write-Host "✓ Removed companion macro: $($f.FullName)" -ForegroundColor Green
}

Write-Host "✓ CorelMate uninstallation complete." -ForegroundColor Green
