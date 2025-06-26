# Simple Test Runner for MonitoringWorker

Write-Host "🧪 MonitoringWorker Test Runner" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "🔍 Checking prerequisites..." -ForegroundColor Cyan

try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ .NET SDK not found" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj")) {
    Write-Host "❌ Test project not found" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Prerequisites check passed" -ForegroundColor Green
Write-Host ""

# Create output directory
if (-not (Test-Path "TestResults")) {
    New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null
}

# Run tests
Write-Host "🧪 Running tests..." -ForegroundColor Cyan

dotnet test tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj --configuration Release --logger "trx;LogFileName=test-results.trx" --results-directory TestResults --verbosity normal

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Tests passed!" -ForegroundColor Green
} else {
    Write-Host "❌ Tests failed!" -ForegroundColor Red
    exit 1
}

# Show summary
Write-Host ""
Write-Host "📋 Test Summary" -ForegroundColor Cyan
Write-Host "===============" -ForegroundColor Cyan

$trxFiles = Get-ChildItem -Path "TestResults" -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue

if ($trxFiles.Count -gt 0) {
    foreach ($trxFile in $trxFiles) {
        try {
            [xml]$trxContent = Get-Content $trxFile.FullName
            $testRun = $trxContent.TestRun
            
            if ($testRun -and $testRun.ResultSummary -and $testRun.ResultSummary.Counters) {
                $counters = $testRun.ResultSummary.Counters
                $totalTests = [int]$counters.total
                $passedTests = [int]$counters.passed
                $failedTests = [int]$counters.failed
                
                Write-Host "📊 Results:" -ForegroundColor White
                Write-Host "   Total Tests: $totalTests" -ForegroundColor White
                Write-Host "   Passed: $passedTests" -ForegroundColor Green
                Write-Host "   Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
                
                if ($totalTests -gt 0) {
                    $successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
                    Write-Host "   Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })
                }
            }
        }
        catch {
            Write-Host "⚠️ Could not parse test results" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "⚠️ No test result files found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📁 Test Results: TestResults" -ForegroundColor Cyan
Write-Host "🎉 Test execution completed!" -ForegroundColor Green
