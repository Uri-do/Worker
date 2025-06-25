# MonitoringWorker Startup Script
# This script helps start the application cleanly

param(
    [switch]$Clean,
    [switch]$Build,
    [switch]$Test
)

Write-Host "🚀 MonitoringWorker Startup Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Clean up if requested
if ($Clean) {
    Write-Host "🧹 Running cleanup..." -ForegroundColor Yellow
    & "$PSScriptRoot\cleanup.ps1"
    Start-Sleep -Seconds 2
}

# Build if requested
if ($Build) {
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build MonitoringWorker.sln
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Build successful!" -ForegroundColor Green
}

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    dotnet test MonitoringWorker.sln --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Tests passed!" -ForegroundColor Green
}

Write-Host ""
Write-Host "🌐 Starting MonitoringWorker..." -ForegroundColor Green
Write-Host "📍 API will be available at:" -ForegroundColor Yellow
Write-Host "   • http://localhost:5000" -ForegroundColor Cyan
Write-Host "   • https://localhost:5001" -ForegroundColor Cyan
Write-Host "📚 Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host ""

# Start the application
dotnet run --project MonitoringWorker.csproj
