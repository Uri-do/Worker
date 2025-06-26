# PowerShell script to stop the monitoring stack

param(
    [switch]$RemoveData,
    [switch]$RemoveImages
)

Write-Host "🛑 Stopping MonitoringWorker Observability Stack" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Yellow

# Navigate to the project root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "📁 Working directory: $projectRoot" -ForegroundColor Cyan

# Stop the services
Write-Host "🛑 Stopping monitoring services..." -ForegroundColor Yellow
docker-compose -f docker-compose.monitoring.yml down

if ($RemoveData) {
    Write-Host "🗑️ Removing persistent data volumes..." -ForegroundColor Red
    docker volume rm monitoring-prometheus-data -f
    docker volume rm monitoring-grafana-data -f
    Write-Host "✅ Data volumes removed" -ForegroundColor Green
}

if ($RemoveImages) {
    Write-Host "🗑️ Removing Docker images..." -ForegroundColor Red
    docker rmi prom/prometheus:v2.47.0 -f
    docker rmi grafana/grafana:10.1.0 -f
    Write-Host "✅ Docker images removed" -ForegroundColor Green
}

# Clean up any orphaned containers
Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
docker system prune -f

Write-Host "✅ Monitoring stack stopped successfully!" -ForegroundColor Green

if (-not $RemoveData) {
    Write-Host "`n💡 Note: Data volumes are preserved. Use -RemoveData to delete them." -ForegroundColor Cyan
}
