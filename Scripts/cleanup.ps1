# MonitoringWorker Cleanup Script
# This script helps clean up any stuck processes

Write-Host "🧹 Cleaning up MonitoringWorker processes..." -ForegroundColor Yellow

# Kill any MonitoringWorker.exe processes
$monitoringProcesses = Get-Process -Name "MonitoringWorker" -ErrorAction SilentlyContinue
if ($monitoringProcesses) {
    Write-Host "Found $($monitoringProcesses.Count) MonitoringWorker processes. Terminating..." -ForegroundColor Red
    $monitoringProcesses | Stop-Process -Force
    Write-Host "✅ MonitoringWorker processes terminated." -ForegroundColor Green
} else {
    Write-Host "ℹ️ No MonitoringWorker processes found." -ForegroundColor Blue
}

# Kill any dotnet processes running our application (be careful with this)
$dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
    $_.MainWindowTitle -like "*MonitoringWorker*" -or 
    $_.CommandLine -like "*MonitoringWorker*"
}

if ($dotnetProcesses) {
    Write-Host "Found $($dotnetProcesses.Count) related dotnet processes. Terminating..." -ForegroundColor Red
    $dotnetProcesses | Stop-Process -Force
    Write-Host "✅ Related dotnet processes terminated." -ForegroundColor Green
} else {
    Write-Host "ℹ️ No related dotnet processes found." -ForegroundColor Blue
}

# Check for processes using port 5000 and 5001
Write-Host "🔍 Checking for processes using ports 5000 and 5001..." -ForegroundColor Yellow

$port5000 = netstat -ano | findstr ":5000"
$port5001 = netstat -ano | findstr ":5001"

if ($port5000) {
    Write-Host "⚠️ Port 5000 is in use:" -ForegroundColor Yellow
    Write-Host $port5000
}

if ($port5001) {
    Write-Host "⚠️ Port 5001 is in use:" -ForegroundColor Yellow
    Write-Host $port5001
}

if (-not $port5000 -and -not $port5001) {
    Write-Host "✅ Ports 5000 and 5001 are free." -ForegroundColor Green
}

Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "You can now run: dotnet run" -ForegroundColor Cyan
