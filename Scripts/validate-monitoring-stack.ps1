# Monitoring Stack Validation Script
# This script validates the complete monitoring implementation

param(
    [switch]$Quick,
    [switch]$Full,
    [switch]$SkipExternal,
    [int]$TimeoutSeconds = 30
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-HttpEndpoint {
    param(
        [string]$Url,
        [string]$Name,
        [int]$ExpectedStatus = 200,
        [string]$ExpectedContent = $null,
        [int]$TimeoutSec = 10
    )
    
    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSec -UseBasicParsing
        
        if ($response.StatusCode -eq $ExpectedStatus) {
            if ($ExpectedContent -and $response.Content -notlike "*$ExpectedContent*") {
                Write-ColorOutput "⚠️  $Name: Status OK but content check failed" "Yellow"
                return $false
            }
            Write-ColorOutput "✅ $Name: OK ($($response.StatusCode))" "Green"
            return $true
        } else {
            Write-ColorOutput "❌ $Name: Unexpected status $($response.StatusCode)" "Red"
            return $false
        }
    }
    catch {
        Write-ColorOutput "❌ $Name: $($_.Exception.Message)" "Red"
        return $false
    }
}

function Test-MonitoringWorkerHealth {
    Write-ColorOutput "`n🏥 Testing MonitoringWorker Health..." "Cyan"
    
    $results = @{}
    
    # Health endpoint
    $results.Health = Test-HttpEndpoint -Url "http://localhost:5000/health" -Name "Health Endpoint" -ExpectedContent "Healthy"
    
    # Metrics endpoint
    $results.Metrics = Test-HttpEndpoint -Url "http://localhost:5000/metrics" -Name "Metrics Endpoint" -ExpectedContent "monitoring_worker"
    
    # Configuration API
    $results.ConfigAPI = Test-HttpEndpoint -Url "http://localhost:5000/api/monitoringconfiguration/status" -Name "Configuration API"
    
    # Feature toggles
    $results.Features = Test-HttpEndpoint -Url "http://localhost:5000/api/monitoringconfiguration/features" -Name "Feature Toggles API"
    
    return $results
}

function Test-PrometheusHealth {
    Write-ColorOutput "`n🔥 Testing Prometheus Health..." "Cyan"
    
    if ($SkipExternal) {
        Write-ColorOutput "⏭️  Skipping Prometheus tests (SkipExternal flag)" "Yellow"
        return @{ Skipped = $true }
    }
    
    $results = @{}
    
    # Prometheus health
    $results.Health = Test-HttpEndpoint -Url "http://localhost:9090/-/healthy" -Name "Prometheus Health"
    
    # Prometheus ready
    $results.Ready = Test-HttpEndpoint -Url "http://localhost:9090/-/ready" -Name "Prometheus Ready"
    
    # Targets endpoint
    $results.Targets = Test-HttpEndpoint -Url "http://localhost:9090/api/v1/targets" -Name "Prometheus Targets API"
    
    # Rules endpoint
    $results.Rules = Test-HttpEndpoint -Url "http://localhost:9090/api/v1/rules" -Name "Prometheus Rules API"
    
    # Check if MonitoringWorker target is being scraped
    try {
        $targetsResponse = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/targets" -TimeoutSec $TimeoutSeconds
        $monitoringWorkerTarget = $targetsResponse.data.activeTargets | Where-Object { $_.labels.job -eq "monitoring-worker" }
        
        if ($monitoringWorkerTarget) {
            if ($monitoringWorkerTarget.health -eq "up") {
                Write-ColorOutput "✅ MonitoringWorker Target: UP" "Green"
                $results.TargetUp = $true
            } else {
                Write-ColorOutput "❌ MonitoringWorker Target: DOWN" "Red"
                $results.TargetUp = $false
            }
        } else {
            Write-ColorOutput "⚠️  MonitoringWorker Target: Not found" "Yellow"
            $results.TargetUp = $false
        }
    }
    catch {
        Write-ColorOutput "❌ Failed to check Prometheus targets: $($_.Exception.Message)" "Red"
        $results.TargetUp = $false
    }
    
    return $results
}

function Test-AlertmanagerHealth {
    Write-ColorOutput "`n🚨 Testing Alertmanager Health..." "Cyan"
    
    if ($SkipExternal) {
        Write-ColorOutput "⏭️  Skipping Alertmanager tests (SkipExternal flag)" "Yellow"
        return @{ Skipped = $true }
    }
    
    $results = @{}
    
    # Alertmanager health
    $results.Health = Test-HttpEndpoint -Url "http://localhost:9093/-/healthy" -Name "Alertmanager Health"
    
    # Alertmanager ready
    $results.Ready = Test-HttpEndpoint -Url "http://localhost:9093/-/ready" -Name "Alertmanager Ready"
    
    # Status API
    $results.Status = Test-HttpEndpoint -Url "http://localhost:9093/api/v1/status" -Name "Alertmanager Status API"
    
    return $results
}

function Test-GrafanaHealth {
    Write-ColorOutput "`n📊 Testing Grafana Health..." "Cyan"
    
    if ($SkipExternal) {
        Write-ColorOutput "⏭️  Skipping Grafana tests (SkipExternal flag)" "Yellow"
        return @{ Skipped = $true }
    }
    
    $results = @{}
    
    # Grafana health
    $results.Health = Test-HttpEndpoint -Url "http://localhost:3000/api/health" -Name "Grafana Health"
    
    # Grafana login page
    $results.LoginPage = Test-HttpEndpoint -Url "http://localhost:3000/login" -Name "Grafana Login Page" -ExpectedContent "Grafana"
    
    return $results
}

function Test-MetricsCollection {
    Write-ColorOutput "`n📈 Testing Metrics Collection..." "Cyan"
    
    try {
        $metricsResponse = Invoke-WebRequest -Uri "http://localhost:5000/metrics" -TimeoutSec $TimeoutSeconds -UseBasicParsing
        $metricsContent = $metricsResponse.Content
        
        $expectedMetrics = @(
            "monitoring_worker_uptime_seconds",
            "monitoring_worker_heartbeat_total",
            "monitoring_worker_jobs_started_total",
            "monitoring_worker_jobs_completed_total",
            "process_cpu_seconds_total",
            "process_resident_memory_bytes"
        )
        
        $results = @{}
        foreach ($metric in $expectedMetrics) {
            if ($metricsContent -like "*$metric*") {
                Write-ColorOutput "✅ Metric: $metric" "Green"
                $results[$metric] = $true
            } else {
                Write-ColorOutput "❌ Metric: $metric (missing)" "Red"
                $results[$metric] = $false
            }
        }
        
        return $results
    }
    catch {
        Write-ColorOutput "❌ Failed to retrieve metrics: $($_.Exception.Message)" "Red"
        return @{ Error = $true }
    }
}

function Test-ConfigurationFeatures {
    Write-ColorOutput "`n⚙️  Testing Configuration Features..." "Cyan"
    
    try {
        # Test configuration status
        $statusResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/monitoringconfiguration/status" -TimeoutSec $TimeoutSeconds
        
        $results = @{}
        $results.ConfigValid = $statusResponse.isValid
        $results.MonitoringEnabled = $statusResponse.isMonitoringEnabled
        $results.EnabledFeatures = $statusResponse.enabledFeatures.Count
        
        if ($statusResponse.isValid) {
            Write-ColorOutput "✅ Configuration: Valid" "Green"
        } else {
            Write-ColorOutput "❌ Configuration: Invalid" "Red"
        }
        
        if ($statusResponse.isMonitoringEnabled) {
            Write-ColorOutput "✅ Monitoring: Enabled" "Green"
        } else {
            Write-ColorOutput "⚠️  Monitoring: Disabled" "Yellow"
        }
        
        Write-ColorOutput "📊 Enabled Features: $($statusResponse.enabledFeatures.Count)" "White"
        
        # Test feature toggles
        $featuresResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/monitoringconfiguration/features" -TimeoutSec $TimeoutSeconds
        
        $criticalFeatures = @("monitoring", "basic-metrics", "health-checks")
        foreach ($feature in $criticalFeatures) {
            if ($featuresResponse.$feature) {
                Write-ColorOutput "✅ Feature: $feature" "Green"
                $results["Feature_$feature"] = $true
            } else {
                Write-ColorOutput "⚠️  Feature: $feature (disabled)" "Yellow"
                $results["Feature_$feature"] = $false
            }
        }
        
        return $results
    }
    catch {
        Write-ColorOutput "❌ Failed to test configuration: $($_.Exception.Message)" "Red"
        return @{ Error = $true }
    }
}

function Test-AlertRules {
    Write-ColorOutput "`n🚨 Testing Alert Rules..." "Cyan"
    
    if ($SkipExternal) {
        Write-ColorOutput "⏭️  Skipping alert rules tests (SkipExternal flag)" "Yellow"
        return @{ Skipped = $true }
    }
    
    try {
        $rulesResponse = Invoke-RestMethod -Uri "http://localhost:9090/api/v1/rules" -TimeoutSec $TimeoutSeconds
        
        $results = @{}
        $totalRules = 0
        $activeRules = 0
        
        foreach ($group in $rulesResponse.data.groups) {
            foreach ($rule in $group.rules) {
                $totalRules++
                if ($rule.state -eq "firing" -or $rule.state -eq "pending") {
                    $activeRules++
                }
            }
        }
        
        $results.TotalRules = $totalRules
        $results.ActiveRules = $activeRules
        
        Write-ColorOutput "📋 Total Alert Rules: $totalRules" "White"
        Write-ColorOutput "🔥 Active Alerts: $activeRules" "White"
        
        if ($totalRules -gt 0) {
            Write-ColorOutput "✅ Alert Rules: Loaded" "Green"
        } else {
            Write-ColorOutput "⚠️  Alert Rules: None found" "Yellow"
        }
        
        return $results
    }
    catch {
        Write-ColorOutput "❌ Failed to check alert rules: $($_.Exception.Message)" "Red"
        return @{ Error = $true }
    }
}

function Run-QuickValidation {
    Write-ColorOutput "🚀 Running Quick Validation..." "Green"
    
    $results = @{}
    $results.MonitoringWorker = Test-MonitoringWorkerHealth
    
    if (-not $SkipExternal) {
        $results.Prometheus = Test-PrometheusHealth
    }
    
    return $results
}

function Run-FullValidation {
    Write-ColorOutput "🎯 Running Full Validation..." "Green"
    
    $results = @{}
    $results.MonitoringWorker = Test-MonitoringWorkerHealth
    $results.Metrics = Test-MetricsCollection
    $results.Configuration = Test-ConfigurationFeatures
    
    if (-not $SkipExternal) {
        $results.Prometheus = Test-PrometheusHealth
        $results.Alertmanager = Test-AlertmanagerHealth
        $results.Grafana = Test-GrafanaHealth
        $results.AlertRules = Test-AlertRules
    }
    
    return $results
}

function Show-ValidationSummary {
    param($Results)
    
    Write-ColorOutput "`n📋 Validation Summary" "Cyan"
    Write-ColorOutput "====================" "Cyan"
    
    $totalTests = 0
    $passedTests = 0
    $failedTests = 0
    $skippedTests = 0
    
    foreach ($category in $Results.Keys) {
        $categoryResults = $Results[$category]
        
        if ($categoryResults.Skipped) {
            $skippedTests++
            continue
        }
        
        foreach ($test in $categoryResults.Keys) {
            $totalTests++
            if ($categoryResults[$test] -eq $true) {
                $passedTests++
            } else {
                $failedTests++
            }
        }
    }
    
    Write-ColorOutput "📊 Test Results:" "White"
    Write-ColorOutput "   Total: $totalTests" "White"
    Write-ColorOutput "   Passed: $passedTests" "Green"
    Write-ColorOutput "   Failed: $failedTests" $(if ($failedTests -gt 0) { "Red" } else { "Green" })
    Write-ColorOutput "   Skipped: $skippedTests" "Yellow"
    
    if ($totalTests -gt 0) {
        $successRate = [math]::Round(($passedTests / $totalTests) * 100, 1)
        Write-ColorOutput "   Success Rate: $successRate%" $(if ($successRate -ge 90) { "Green" } elseif ($successRate -ge 70) { "Yellow" } else { "Red" })
        
        if ($successRate -ge 90) {
            Write-ColorOutput "`n🎉 Monitoring stack validation PASSED!" "Green"
            return $true
        } elseif ($successRate -ge 70) {
            Write-ColorOutput "`n⚠️  Monitoring stack validation PARTIAL - some issues detected" "Yellow"
            return $false
        } else {
            Write-ColorOutput "`n❌ Monitoring stack validation FAILED - significant issues detected" "Red"
            return $false
        }
    } else {
        Write-ColorOutput "`n⚠️  No tests were executed" "Yellow"
        return $false
    }
}

function Show-Help {
    Write-ColorOutput "Monitoring Stack Validation Script" "Cyan"
    Write-ColorOutput "==================================" "Cyan"
    Write-ColorOutput ""
    Write-ColorOutput "This script validates the complete monitoring implementation." "White"
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\validate-monitoring-stack.ps1 [OPTIONS]" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Options:" "Yellow"
    Write-ColorOutput "  -Quick          Run quick validation (MonitoringWorker + Prometheus only)" "White"
    Write-ColorOutput "  -Full           Run full validation (all components)" "White"
    Write-ColorOutput "  -SkipExternal   Skip external services (Prometheus, Alertmanager, Grafana)" "White"
    Write-ColorOutput "  -TimeoutSeconds Timeout for HTTP requests (default: 30)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" "Yellow"
    Write-ColorOutput "  .\validate-monitoring-stack.ps1 -Quick" "Gray"
    Write-ColorOutput "  .\validate-monitoring-stack.ps1 -Full" "Gray"
    Write-ColorOutput "  .\validate-monitoring-stack.ps1 -Full -SkipExternal" "Gray"
    Write-ColorOutput ""
    Write-ColorOutput "Prerequisites:" "Yellow"
    Write-ColorOutput "  • MonitoringWorker running on http://localhost:5000" "White"
    Write-ColorOutput "  • Prometheus running on http://localhost:9090 (unless -SkipExternal)" "White"
    Write-ColorOutput "  • Alertmanager running on http://localhost:9093 (unless -SkipExternal)" "White"
    Write-ColorOutput "  • Grafana running on http://localhost:3000 (unless -SkipExternal)" "White"
}

# Main execution
try {
    Write-ColorOutput "🔍 MonitoringWorker Stack Validation" "Green"
    Write-ColorOutput "====================================" "Green"
    Write-ColorOutput ""

    $results = @{}

    if ($Quick) {
        $results = Run-QuickValidation
    }
    elseif ($Full) {
        $results = Run-FullValidation
    }
    else {
        # Default to full validation
        $results = Run-FullValidation
    }

    $success = Show-ValidationSummary -Results $results

    if ($success) {
        exit 0
    } else {
        exit 1
    }
}
catch {
    Write-ColorOutput "❌ Validation script failed: $($_.Exception.Message)" "Red"
    Write-ColorOutput "Stack trace: $($_.ScriptStackTrace)" "Gray"
    exit 1
}
