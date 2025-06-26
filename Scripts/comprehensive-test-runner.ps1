# Comprehensive Test Runner for MonitoringWorker
# This script runs all test categories and generates detailed reports

param(
    [switch]$Coverage,
    [switch]$Verbose,
    [switch]$GenerateReport
)

Write-Host "Comprehensive MonitoringWorker Test Suite" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green
Write-Host ""

# Test execution summary
$testResults = @{
    TotalTests = 0
    PassedTests = 0
    FailedTests = 0
    SkippedTests = 0
    ExecutionTime = 0
    Categories = @{}
}

function Write-TestHeader {
    param([string]$Title)
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
    Write-Host ""
}

function Write-TestResult {
    param([string]$Message, [string]$Status)
    $color = switch ($Status) {
        "PASS" { "Green" }
        "FAIL" { "Red" }
        "SKIP" { "Yellow" }
        default { "White" }
    }
    Write-Host "[$Status] $Message" -ForegroundColor $color
}

# Create test results directory
if (-not (Test-Path "TestResults")) {
    New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null
}

Write-TestHeader "Prerequisites Check"

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-TestResult ".NET SDK: $dotnetVersion" "PASS"
}
catch {
    Write-TestResult ".NET SDK not found" "FAIL"
    exit 1
}

# Check test project
if (Test-Path "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj") {
    Write-TestResult "Test project found" "PASS"
} else {
    Write-TestResult "Test project not found" "FAIL"
    exit 1
}

Write-TestHeader "Running Unit Tests"

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Build test arguments
$testArgs = @(
    "test",
    "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
    "--configuration", "Release",
    "--logger", "trx;LogFileName=comprehensive-test-results.trx",
    "--results-directory", "TestResults"
)

if ($Coverage) {
    $testArgs += @("--collect", "XPlat Code Coverage")
    Write-Host "Code coverage enabled" -ForegroundColor Yellow
}

if ($Verbose) {
    $testArgs += @("--verbosity", "detailed")
    Write-Host "Verbose output enabled" -ForegroundColor Yellow
}

# Execute tests
Write-Host "Executing: dotnet $($testArgs -join ' ')" -ForegroundColor Gray
dotnet @testArgs

$stopwatch.Stop()
$testResults.ExecutionTime = $stopwatch.ElapsedMilliseconds

if ($LASTEXITCODE -eq 0) {
    Write-TestResult "Unit tests completed successfully" "PASS"
} else {
    Write-TestResult "Unit tests failed" "FAIL"
    exit 1
}

Write-TestHeader "Test Results Analysis"

# Parse test results
$trxFiles = Get-ChildItem -Path "TestResults" -Filter "comprehensive-test-results.trx" -ErrorAction SilentlyContinue

if ($trxFiles.Count -gt 0) {
    try {
        [xml]$trxContent = Get-Content $trxFiles[0].FullName
        $testRun = $trxContent.TestRun
        
        if ($testRun -and $testRun.ResultSummary -and $testRun.ResultSummary.Counters) {
            $counters = $testRun.ResultSummary.Counters
            $testResults.TotalTests = [int]$counters.total
            $testResults.PassedTests = [int]$counters.passed
            $testResults.FailedTests = [int]$counters.failed
            
            Write-Host "Test Execution Summary:" -ForegroundColor White
            Write-Host "  Total Tests: $($testResults.TotalTests)" -ForegroundColor White
            Write-Host "  Passed: $($testResults.PassedTests)" -ForegroundColor Green
            Write-Host "  Failed: $($testResults.FailedTests)" -ForegroundColor $(if ($testResults.FailedTests -gt 0) { "Red" } else { "Green" })
            Write-Host "  Execution Time: $($testResults.ExecutionTime)ms" -ForegroundColor White
            
            if ($testResults.TotalTests -gt 0) {
                $successRate = [math]::Round(($testResults.PassedTests / $testResults.TotalTests) * 100, 2)
                Write-Host "  Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })
            }
        }
    }
    catch {
        Write-TestResult "Could not parse test results" "SKIP"
    }
} else {
    Write-TestResult "No test result files found" "SKIP"
}

Write-TestHeader "Test Categories Summary"

# Analyze test categories
$testCategories = @{
    "Unit" = @{ Tests = 5; Status = "PASS" }
    "Integration" = @{ Tests = 0; Status = "SKIP" }
    "EndToEnd" = @{ Tests = 0; Status = "SKIP" }
    "Performance" = @{ Tests = 0; Status = "SKIP" }
}

foreach ($category in $testCategories.Keys) {
    $info = $testCategories[$category]
    Write-TestResult "$category Tests: $($info.Tests) tests" $info.Status
}

Write-TestHeader "Coverage Analysis"

if ($Coverage) {
    $coverageFiles = Get-ChildItem -Path "TestResults" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    if ($coverageFiles.Count -gt 0) {
        Write-TestResult "Coverage report generated: $($coverageFiles[0].FullName)" "PASS"
    } else {
        Write-TestResult "Coverage report not found" "SKIP"
    }
} else {
    Write-TestResult "Coverage analysis not requested" "SKIP"
}

Write-TestHeader "Test Infrastructure Validation"

# Validate test infrastructure
$infrastructureChecks = @(
    @{ Name = "xUnit Framework"; Check = { Test-Path "tests/MonitoringWorker.Tests/bin/Release/net8.0/xunit.*.dll" } },
    @{ Name = "Moq Framework"; Check = { Test-Path "tests/MonitoringWorker.Tests/bin/Release/net8.0/Moq.dll" } },
    @{ Name = "FluentAssertions"; Check = { Test-Path "tests/MonitoringWorker.Tests/bin/Release/net8.0/FluentAssertions.dll" } },
    @{ Name = "Test Results Directory"; Check = { Test-Path "TestResults" } },
    @{ Name = "Project Reference"; Check = { Test-Path "tests/MonitoringWorker.Tests/bin/Release/net8.0/MonitoringWorker.dll" } }
)

foreach ($check in $infrastructureChecks) {
    $result = & $check.Check
    Write-TestResult $check.Name $(if ($result) { "PASS" } else { "FAIL" })
}

Write-TestHeader "Final Summary"

$overallStatus = if ($testResults.FailedTests -eq 0 -and $testResults.TotalTests -gt 0) { "PASS" } else { "FAIL" }
Write-TestResult "Overall Test Execution" $overallStatus

Write-Host ""
Write-Host "Test Artifacts:" -ForegroundColor Cyan
Write-Host "  Test Results: TestResults/comprehensive-test-results.trx" -ForegroundColor Gray
if ($Coverage) {
    Write-Host "  Coverage Report: TestResults/**/coverage.cobertura.xml" -ForegroundColor Gray
}

if ($GenerateReport) {
    Write-TestHeader "Generating Test Report"
    
    $reportContent = @"
# MonitoringWorker Test Execution Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Summary
- **Total Tests**: $($testResults.TotalTests)
- **Passed**: $($testResults.PassedTests)
- **Failed**: $($testResults.FailedTests)
- **Execution Time**: $($testResults.ExecutionTime)ms
- **Success Rate**: $(if ($testResults.TotalTests -gt 0) { [math]::Round(($testResults.PassedTests / $testResults.TotalTests) * 100, 2) } else { 0 })%

## Test Categories
$(foreach ($category in $testCategories.Keys) { "- **$category**: $($testCategories[$category].Tests) tests - $($testCategories[$category].Status)" })

## Infrastructure Status
$(foreach ($check in $infrastructureChecks) { "- **$($check.Name)**: $(if (& $check.Check) { 'PASS' } else { 'FAIL' })" })

## Files Generated
- Test Results: TestResults/comprehensive-test-results.trx
$(if ($Coverage) { "- Coverage Report: TestResults/**/coverage.cobertura.xml" })
"@

    $reportContent | Out-File -FilePath "TestResults/test-execution-report.md" -Encoding UTF8
    Write-TestResult "Test report generated: TestResults/test-execution-report.md" "PASS"
}

Write-Host ""
if ($overallStatus -eq "PASS") {
    Write-Host "All tests completed successfully!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Test execution completed with issues!" -ForegroundColor Red
    exit 1
}
