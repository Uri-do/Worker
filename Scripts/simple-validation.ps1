# Simple Monitoring Stack Validation Script

Write-Host "MonitoringWorker Stack Validation" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green
Write-Host ""

function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$Name,
        [int]$TimeoutSec = 10
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSec -UseBasicParsing
        
        if ($response.StatusCode -eq 200) {
            Write-Host "OK $Name" -ForegroundColor Green
            return $true
        } else {
            Write-Host "ERROR $Name - Status: $($response.StatusCode)" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "ERROR $Name - $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test MonitoringWorker endpoints
Write-Host "Testing MonitoringWorker Health..." -ForegroundColor Cyan

$results = @{}

# Health endpoint
$results.Health = Test-HttpEndpoint -Url "http://localhost:5000/health" -Name "Health Endpoint"

# Metrics endpoint
$results.Metrics = Test-HttpEndpoint -Url "http://localhost:5000/metrics" -Name "Metrics Endpoint"

# Configuration API
$results.ConfigAPI = Test-HttpEndpoint -Url "http://localhost:5000/api/monitoringconfiguration/status" -Name "Configuration API"

# Feature toggles
$results.Features = Test-HttpEndpoint -Url "http://localhost:5000/api/monitoringconfiguration/features" -Name "Feature Toggles API"

# Summary
Write-Host ""
Write-Host "Validation Summary" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan

$totalTests = $results.Count
$passedTests = ($results.Values | Where-Object { $_ -eq $true }).Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })

if ($passedTests -eq $totalTests) {
    Write-Host ""
    Write-Host "Validation PASSED!" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "Validation FAILED!" -ForegroundColor Red
    exit 1
}
