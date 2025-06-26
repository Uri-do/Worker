# Simple Test Runner for MonitoringWorker
# This script runs tests for the monitoring system

param(
    [switch]$All,
    [switch]$Coverage,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    try {
        if ($Color -and $Color.Trim() -ne "") {
            Write-Host $Message -ForegroundColor $Color
        } else {
            Write-Host $Message -ForegroundColor White
        }
    } catch {
        Write-Host $Message
    }
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

    Write-ColorOutput "✅ Prerequisites check passed" "Green"
}

function Run-Tests {
    Write-ColorOutput "🧪 Running Tests..." "Cyan"
    
    $arguments = @(
        "test",
        "tests/MonitoringWorker.Tests/MonitoringWorker.Tests.csproj",
        "--configuration", "Release",
        "--logger", "trx;LogFileName=test-results.trx",
        "--results-directory", "TestResults"
    )

    if ($Coverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    if ($Verbose) {
        $arguments += @("--verbosity", "detailed")
    }

    Write-ColorOutput "Running: dotnet $($arguments -join ' ')" "Gray"
    
    dotnet @arguments
    
    if ($LASTEXITCODE -eq 0) {
        Write-ColorOutput "✅ Tests passed" "Green"
        return $true
    } else {
        Write-ColorOutput "❌ Tests failed" "Red"
        return $false
    }
}

function Show-TestSummary {
    Write-ColorOutput "`n📋 Test Execution Summary" "Cyan"
    Write-ColorOutput "=========================" "Cyan"
    
    # Find and parse test result files
    $trxFiles = Get-ChildItem -Path "TestResults" -Filter "*.trx" -Recurse -ErrorAction SilentlyContinue
    
    if ($trxFiles.Count -eq 0) {
        Write-ColorOutput "⚠️ No test result files found" "Yellow"
        return
    }

    $totalTests = 0
    $passedTests = 0
    $failedTests = 0

    foreach ($trxFile in $trxFiles) {
        try {
            [xml]$trxContent = Get-Content $trxFile.FullName
            $testRun = $trxContent.TestRun
            
            if ($testRun -and $testRun.ResultSummary -and $testRun.ResultSummary.Counters) {
                $counters = $testRun.ResultSummary.Counters
                $totalTests += [int]$counters.total
                $passedTests += [int]$counters.passed
                $failedTests += [int]$counters.failed
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
    
    if ($totalTests -gt 0) {
        $successRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
        Write-ColorOutput "   Success Rate: $successRate%" $(if ($successRate -ge 95) { "Green" } elseif ($successRate -ge 80) { "Yellow" } else { "Red" })
    }

    Write-ColorOutput "`n📁 Test Results: TestResults" "Cyan"
}

function Show-Help {
    Write-ColorOutput "Simple MonitoringWorker Test Runner" "Cyan"
    Write-ColorOutput "===================================" "Cyan"
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\run-simple-tests.ps1 [OPTIONS]" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Options:" "Yellow"
    Write-ColorOutput "  -All        Run all tests (default)" "White"
    Write-ColorOutput "  -Coverage   Generate code coverage report" "White"
    Write-ColorOutput "  -Verbose    Enable detailed test output" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" "Yellow"
    Write-ColorOutput "  .\run-simple-tests.ps1" "Gray"
    Write-ColorOutput "  .\run-simple-tests.ps1 -Coverage" "Gray"
    Write-ColorOutput "  .\run-simple-tests.ps1 -Verbose" "Gray"
}

# Main execution
try {
    Write-ColorOutput "🧪 MonitoringWorker Simple Test Runner" "Green"
    Write-ColorOutput "=======================================" "Green"
    Write-ColorOutput ""

    # Create output directory
    if (-not (Test-Path "TestResults")) {
        New-Item -ItemType Directory -Path "TestResults" -Force | Out-Null
    }

    # Check prerequisites
    Test-Prerequisites

    # Run tests
    $success = Run-Tests

    # Show summary
    Show-TestSummary

    if ($success) {
        Write-ColorOutput "`n🎉 Test execution completed successfully!" "Green"
        exit 0
    } else {
        Write-ColorOutput "`n❌ Test execution failed!" "Red"
        exit 1
    }
}
catch {
    Write-ColorOutput "❌ Script execution failed: $($_.Exception.Message)" "Red"
    exit 1
}
