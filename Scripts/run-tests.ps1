# Test Automation Script for MonitoringWorker
# This script runs comprehensive tests for the monitoring system

param(
    [switch]$Unit,
    [switch]$Integration,
    [switch]$EndToEnd,
    [switch]$Performance,
    [switch]$All,
    [switch]$Coverage,
    [switch]$Verbose,
    [string]$Filter = "",
    [string]$Output = "TestResults"
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-Prerequisites {
    Write-ColorOutput "🔍 Checking test prerequisites..." "Cyan"
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-ColorOutput "✅ .NET SDK: $dotnetVersion" "Green"
    }
    catch {
        Write-ColorOutput "❌ .NET SDK not found. Please install .NET 8 SDK." "Red"
        exit 1
    }

    # Check if test project exists
    if (-not (Test-Path "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj")) {
        Write-ColorOutput "❌ Test project not found at tests/MonitoringWorker.Tests/" "Red"
        exit 1
    }

    # Check if main project builds
    Write-ColorOutput "🔨 Building main project..." "Yellow"
    dotnet build src/MonitoringWorker/MonitoringWorker.csproj --configuration Release --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "❌ Main project build failed" "Red"
        exit 1
    }

    Write-ColorOutput "✅ Prerequisites check passed" "Green"
}

function Run-UnitTests {
    Write-ColorOutput "🧪 Running Unit Tests..." "Cyan"
    
    $testFilter = "Category=Unit"
    if ($Filter) {
        $testFilter = "$testFilter`&$Filter"
    }

    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--filter", $testFilter,
        "--logger", "trx;LogFileName=unit-tests.trx",
        "--results-directory", $Output
    )

    if ($Coverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Unit tests passed" "Green"
    } else {
        Write-ColorOutput "❌ Unit tests failed" "Red"
        return $false
    }
    return $true
}

function Run-IntegrationTests {
    Write-ColorOutput "🔗 Running Integration Tests..." "Cyan"
    
    $testFilter = "Category=Integration"
    if ($Filter) {
        $testFilter = "$testFilter`&$Filter"
    }

    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--filter", $testFilter,
        "--logger", "trx;LogFileName=integration-tests.trx",
        "--results-directory", $Output
    )

    if ($Coverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Integration tests passed" "Green"
    } else {
        Write-ColorOutput "❌ Integration tests failed" "Red"
        return $false
    }
    return $true
}

function Run-EndToEndTests {
    Write-ColorOutput "🎯 Running End-to-End Tests..." "Cyan"
    
    $testFilter = "Category=EndToEnd"
    if ($Filter) {
        $testFilter = "$testFilter`&$Filter"
    }

    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--filter", $testFilter,
        "--logger", "trx;LogFileName=e2e-tests.trx",
        "--results-directory", $Output
    )

    if ($Coverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ End-to-end tests passed" "Green"
    } else {
        Write-ColorOutput "❌ End-to-end tests failed" "Red"
        return $false
    }
    return $true
}

function Run-PerformanceTests {
    Write-ColorOutput "🚀 Running Performance Tests..." "Cyan"
    
    $testFilter = "Category=Performance"
    if ($Filter) {
        $testFilter = "$testFilter`&$Filter"
    }

    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--filter", $testFilter,
        "--logger", "trx;LogFileName=performance-tests.trx",
        "--results-directory", $Output
    )

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Performance tests passed" "Green"
    } else {
        Write-ColorOutput "❌ Performance tests failed" "Red"
        return $false
    }
    return $true
}

function Run-AllTests {
    Write-ColorOutput "🎯 Running All Tests..." "Cyan"
    
    $filterArg = if ($Filter) { $Filter } else { "" }

    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--logger", "trx;LogFileName=all-tests.trx",
        "--results-directory", $Output
    )

    if ($filterArg) {
        $arguments += @("--filter", $filterArg)
    }

    if ($Coverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ All tests passed" "Green"
    } else {
        Write-ColorOutput "❌ Some tests failed" "Red"
        return $false
    }
    return $true
}

function Generate-CoverageReport {
    if (-not $Coverage) {
        return
    }

    Write-ColorOutput "📊 Generating coverage report..." "Cyan"
    
    # Find coverage files
    $coverageFiles = Get-ChildItem -Path $Output -Filter "*.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -eq 0) {
        Write-ColorOutput "⚠️ No coverage files found" "Yellow"
        return
    }

    # Install reportgenerator if not present
    try {
        dotnet tool list -g | Select-String "reportgenerator" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput "📦 Installing ReportGenerator..." "Yellow"
            dotnet tool install -g dotnet-reportgenerator-globaltool
        }
    }
    catch {
        Write-ColorOutput "📦 Installing ReportGenerator..." "Yellow"
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }

    # Generate HTML report
    $coverageReportPath = Join-Path $Output "coverage-report"
    $coverageFilePaths = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
    
    reportgenerator -reports:$coverageFilePaths -targetdir:$coverageReportPath -reporttypes:Html
    
    Write-ColorOutput "📊 Coverage report generated at: $coverageReportPath" "Green"
    Write-ColorOutput "🌐 Open: $coverageReportPath\index.html" "Cyan"
}

function Show-TestSummary {
    Write-ColorOutput "`n📋 Test Execution Summary" "Cyan"
    Write-ColorOutput "=========================" "Cyan"
    
    # Find and parse test result files
    $trxFiles = Get-ChildItem -Path $Output -Filter "*.trx" -Recurse
    
    if ($trxFiles.Count -eq 0) {
        Write-ColorOutput "⚠️ No test result files found" "Yellow"
        return
    }

    $totalTests = 0
    $passedTests = 0
    $failedTests = 0
    $skippedTests = 0

    foreach ($trxFile in $trxFiles) {
        try {
            [xml]$trxContent = Get-Content $trxFile.FullName
            $testRun = $trxContent.TestRun
            
            if ($testRun) {
                $resultSummary = $testRun.ResultSummary
                if ($resultSummary) {
                    $counters = $resultSummary.Counters
                    if ($counters) {
                        $totalTests += [int]$counters.total
                        $passedTests += [int]$counters.passed
                        $failedTests += [int]$counters.failed
                        $skippedTests += [int]$counters.inconclusive
                    }
                }
            }
        }
        catch {
            Write-ColorOutput "⚠️ Could not parse $($trxFile.Name)" "Yellow"
        }
    }

    Write-ColorOutput "📊 Results:" "White"
    Write-ColorOutput "   Total Tests: $totalTests" "White"
    Write-ColorOutput "   Passed: $passedTests" "Green"
    Write-ColorOutput "   Failed: $failedTests" $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    Write-ColorOutput "   Skipped: $skippedTests" "Yellow"
    
    if ($totalTests -gt 0) {
        $successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
        Write-ColorOutput "   Success Rate: $successRate%" $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })
    }

    Write-ColorOutput "`n📁 Test Results: $Output" "Cyan"
}

function Show-Help {
    Write-ColorOutput "MonitoringWorker Test Automation Script" "Cyan"
    Write-ColorOutput "=======================================" "Cyan"
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\run-tests.ps1 [OPTIONS]" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Test Categories:" "Yellow"
    Write-ColorOutput "  -Unit           Run unit tests only" "White"
    Write-ColorOutput "  -Integration    Run integration tests only" "White"
    Write-ColorOutput "  -EndToEnd       Run end-to-end tests only" "White"
    Write-ColorOutput "  -Performance    Run performance tests only" "White"
    Write-ColorOutput "  -All            Run all tests (default)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Options:" "Yellow"
    Write-ColorOutput "  -Coverage       Generate code coverage report" "White"
    Write-ColorOutput "  -Verbose        Enable detailed test output" "White"
    Write-ColorOutput "  -Filter         Additional test filter" "White"
    Write-ColorOutput "  -Output         Test results directory (default: TestResults)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" "Yellow"
    Write-ColorOutput "  .\run-tests.ps1 -Unit -Coverage" "Gray"
    Write-ColorOutput "  .\run-tests.ps1 -Integration -Verbose" "Gray"
    Write-ColorOutput "  .\run-tests.ps1 -All -Coverage -Output MyResults" "Gray"
    Write-ColorOutput "  .\run-tests.ps1 -Performance -Filter 'LoadTest'" "Gray"
}

# Main execution
try {
    Write-ColorOutput "🧪 MonitoringWorker Test Automation" "Green"
    Write-ColorOutput "====================================" "Green"
    Write-ColorOutput ""

    # Create output directory
    if (-not (Test-Path $Output)) {
        New-Item -ItemType Directory -Path $Output -Force | Out-Null
    }

    # Check prerequisites
    Test-Prerequisites

    $success = $true

    # Run tests based on parameters
    if ($Unit) {
        $success = Run-UnitTests
    }
    elseif ($Integration) {
        $success = Run-IntegrationTests
    }
    elseif ($EndToEnd) {
        $success = Run-EndToEndTests
    }
    elseif ($Performance) {
        $success = Run-PerformanceTests
    }
    elseif ($All -or (-not $Unit -and -not $Integration -and -not $EndToEnd -and -not $Performance)) {
        $success = Run-AllTests
    }
    else {
        Show-Help
        exit 0
    }

    # Generate coverage report if requested
    Generate-CoverageReport

    # Show summary
    Show-TestSummary

    if ($success) {
        Write-ColorOutput "`n🎉 Test execution completed successfully!" "Green"
        exit 0
    } else {
        Write-ColorOutput "`n❌ Test execution failed!" "Red"
        exit 1
    }
catch {
    Write-ColorOutput "❌ Script execution failed: $($_.Exception.Message)" "Red"
    Write-ColorOutput "Stack trace: $($_.ScriptStackTrace)" "Gray"
    exit 1
}
