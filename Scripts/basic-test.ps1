# Basic Test Runner for MonitoringWorker

Write-Host "MonitoringWorker Test Runner" -ForegroundColor Green
Write-Host "============================" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Cyan

try {
    $dotnetVersion = dotnet --version
    Write-Host "OK .NET SDK: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "ERROR .NET SDK not found" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj")) {
    Write-Host "ERROR Test project not found" -ForegroundColor Red
    exit 1
}

Write-Host "OK Prerequisites check passed" -ForegroundColor Green
Write-Host ""

# Create output directory
if (-not (Test-Path "TestResults")) {
    New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Cyan

dotnet test tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results.trx" --results-directory TestResults --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK Tests passed!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Test execution completed successfully!" -ForegroundColor Green
} else {
    Write-Host "ERROR Tests failed!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Test execution failed!" -ForegroundColor Red
    exit 1
}
