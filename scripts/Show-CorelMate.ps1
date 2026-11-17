param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$dockerGuid = '{A63F9C4C-64A3-4E91-9AC9-3A15C783D5D7}'
$dockerClass = 'CorelMate.UI.CorelMatePanel'
$root = Split-Path -Parent $PSScriptRoot
$uiAssembly = Join-Path $root "src\CorelMate.UI\bin\$Configuration\net48\CorelMate.UI.dll"

if (-not (Test-Path $uiAssembly)) {
    Write-Host "Building CorelMate ($Configuration)..." -ForegroundColor Cyan
    dotnet build (Join-Path $root "CorelMate.sln") -c $Configuration
}

Write-Host "Connecting to CorelDRAW..." -ForegroundColor Cyan
try {
    $app = New-Object -ComObject CorelDRAW.Application
    $app.Visible = $true
    $app.FrameWork.AddDocker($dockerGuid, $dockerClass, $uiAssembly)
    $app.FrameWork.ShowDocker($dockerGuid)
    Write-Host "✓ CorelMate Docker is now active and shown in CorelDRAW $($app.Version)!" -ForegroundColor Green
}
catch {
    Write-Error "Failed to show CorelMate Docker: $_"
}

