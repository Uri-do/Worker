# PowerShell script to start the monitoring stack
# This script starts Prometheus and Grafana using Docker Compose

param(
    [switch]$Build,
    [switch]$Logs,
    [switch]$Status
)

Write-Host "🚀 MonitoringWorker Observability Stack" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green

# Check if Docker is running
try {
    docker version | Out-Null
    Write-Host "✅ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Check if docker-compose is available
try {
    docker-compose version | Out-Null
    Write-Host "✅ Docker Compose is available" -ForegroundColor Green
} catch {
    Write-Host "❌ Docker Compose is not available. Please install Docker Compose." -ForegroundColor Red
    exit 1
}

# Navigate to the project root
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "📁 Working directory: $projectRoot" -ForegroundColor Cyan

if ($Status) {
    Write-Host "📊 Checking monitoring stack status..." -ForegroundColor Yellow
    docker-compose -f docker-compose.monitoring.yml ps
    
    Write-Host "`n🔍 Service URLs:" -ForegroundColor Cyan
    Write-Host "  • MonitoringWorker: http://localhost:5002" -ForegroundColor White
    Write-Host "  • MonitoringWorker Metrics: http://localhost:5002/metrics" -ForegroundColor White
    Write-Host "  • Prometheus: http://localhost:9090" -ForegroundColor White
    Write-Host "  • Grafana: http://localhost:3000 (admin/admin123)" -ForegroundColor White
    exit 0
}

if ($Build) {
    Write-Host "🔨 Building and starting monitoring stack..." -ForegroundColor Yellow
    docker-compose -f docker-compose.monitoring.yml up --build -d
} else {
    Write-Host "🚀 Starting monitoring stack..." -ForegroundColor Yellow
    docker-compose -f docker-compose.monitoring.yml up -d
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Monitoring stack started successfully!" -ForegroundColor Green
    
    Write-Host "`n⏳ Waiting for services to be ready..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    Write-Host "`n🔍 Service URLs:" -ForegroundColor Cyan
    Write-Host "  • MonitoringWorker: http://localhost:5002" -ForegroundColor White
    Write-Host "  • MonitoringWorker Metrics: http://localhost:5002/metrics" -ForegroundColor White
    Write-Host "  • Prometheus: http://localhost:9090" -ForegroundColor White
    Write-Host "  • Grafana: http://localhost:3000 (admin/admin123)" -ForegroundColor White
    
    Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Start your MonitoringWorker: dotnet run" -ForegroundColor White
    Write-Host "  2. Check Prometheus targets: http://localhost:9090/targets" -ForegroundColor White
    Write-Host "  3. View metrics in Grafana: http://localhost:3000" -ForegroundColor White
    
    if ($Logs) {
        Write-Host "`n📜 Showing logs..." -ForegroundColor Yellow
        docker-compose -f docker-compose.monitoring.yml logs -f
    }
} else {
    Write-Host "❌ Failed to start monitoring stack" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎉 Setup complete! Happy monitoring! 📊" -ForegroundColor Green
