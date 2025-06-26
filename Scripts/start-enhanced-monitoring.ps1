# Enhanced Monitoring Stack Startup Script
# This script starts Prometheus, Alertmanager, and Grafana for MonitoringWorker monitoring

param(
    [switch]$SkipPrometheus,
    [switch]$SkipAlertmanager,
    [switch]$SkipGrafana,
    [switch]$Status,
    [switch]$Download,
    [switch]$Stop
)

$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-ServiceRunning {
    param([string]$Url, [int]$TimeoutSeconds = 5)
    try {
        $response = Invoke-WebRequest -Uri $Url -TimeoutSec $TimeoutSeconds -UseBasicParsing
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Start-EnhancedMonitoring {
    Write-ColorOutput "🚀 Enhanced MonitoringWorker Observability Stack" "Green"
    Write-ColorOutput "================================================" "Green"
    Write-ColorOutput ""

    # Download components if requested
    if ($Download) {
        Write-ColorOutput "📥 Downloading monitoring components..." "Cyan"
        
        if (-not $SkipPrometheus) {
            Write-ColorOutput "Downloading Prometheus..." "Yellow"
            & ".\scripts\setup-prometheus-windows.ps1" -Download
        }
        
        if (-not $SkipAlertmanager) {
            Write-ColorOutput "Downloading Alertmanager..." "Yellow"
            & ".\scripts\setup-alertmanager-windows.ps1" -Download
        }
        
        if (-not $SkipGrafana) {
            Write-ColorOutput "Downloading Grafana..." "Yellow"
            & ".\scripts\setup-grafana-windows.ps1" -Download
        }
        
        Write-ColorOutput "✅ All components downloaded!" "Green"
        Write-ColorOutput ""
    }

    # Start Prometheus
    if (-not $SkipPrometheus) {
        Write-ColorOutput "🔥 Starting Prometheus..." "Cyan"
        & ".\scripts\setup-prometheus-windows.ps1" -Start
        
        # Wait for Prometheus to be ready
        $timeout = 30
        $elapsed = 0
        Write-ColorOutput "⏳ Waiting for Prometheus to be ready..." "Yellow"
        
        while ($elapsed -lt $timeout) {
            if (Test-ServiceRunning "http://localhost:9090/-/ready") {
                Write-ColorOutput "✅ Prometheus is ready!" "Green"
                break
            }
            Start-Sleep -Seconds 2
            $elapsed += 2
        }
        
        if ($elapsed -ge $timeout) {
            Write-ColorOutput "⚠️  Prometheus may not be fully ready yet" "Yellow"
        }
        Write-ColorOutput ""
    }

    # Start Alertmanager
    if (-not $SkipAlertmanager) {
        Write-ColorOutput "🚨 Starting Alertmanager..." "Cyan"
        & ".\scripts\setup-alertmanager-windows.ps1" -Start
        
        # Wait for Alertmanager to be ready
        $timeout = 30
        $elapsed = 0
        Write-ColorOutput "⏳ Waiting for Alertmanager to be ready..." "Yellow"
        
        while ($elapsed -lt $timeout) {
            if (Test-ServiceRunning "http://localhost:9093/-/healthy") {
                Write-ColorOutput "✅ Alertmanager is ready!" "Green"
                break
            }
            Start-Sleep -Seconds 2
            $elapsed += 2
        }
        
        if ($elapsed -ge $timeout) {
            Write-ColorOutput "⚠️  Alertmanager may not be fully ready yet" "Yellow"
        }
        Write-ColorOutput ""
    }

    # Start Grafana
    if (-not $SkipGrafana) {
        Write-ColorOutput "📊 Starting Grafana..." "Cyan"
        & ".\scripts\setup-grafana-windows.ps1" -Start
        
        # Wait for Grafana to be ready
        $timeout = 60
        $elapsed = 0
        Write-ColorOutput "⏳ Waiting for Grafana to be ready..." "Yellow"
        
        while ($elapsed -lt $timeout) {
            if (Test-ServiceRunning "http://localhost:3000/api/health") {
                Write-ColorOutput "✅ Grafana is ready!" "Green"
                break
            }
            Start-Sleep -Seconds 3
            $elapsed += 3
        }
        
        if ($elapsed -ge $timeout) {
            Write-ColorOutput "⚠️  Grafana may not be fully ready yet" "Yellow"
        }
        Write-ColorOutput ""
    }

    # Display service URLs and status
    Show-ServiceStatus
    
    Write-ColorOutput "🎉 Enhanced monitoring stack is ready!" "Green"
    Write-ColorOutput ""
    Write-ColorOutput "📋 Next Steps:" "Cyan"
    Write-ColorOutput "  1. Start your MonitoringWorker: dotnet run" "White"
    Write-ColorOutput "  2. Check Prometheus targets: http://localhost:9090/targets" "White"
    Write-ColorOutput "  3. View alerts in Alertmanager: http://localhost:9093" "White"
    Write-ColorOutput "  4. Monitor dashboards in Grafana: http://localhost:3000" "White"
    Write-ColorOutput "  5. Test alerts by stopping MonitoringWorker" "White"
}

function Stop-EnhancedMonitoring {
    Write-ColorOutput "🛑 Stopping Enhanced Monitoring Stack..." "Cyan"
    Write-ColorOutput ""

    if (-not $SkipGrafana) {
        Write-ColorOutput "Stopping Grafana..." "Yellow"
        & ".\scripts\setup-grafana-windows.ps1" -Stop
    }

    if (-not $SkipAlertmanager) {
        Write-ColorOutput "Stopping Alertmanager..." "Yellow"
        & ".\scripts\setup-alertmanager-windows.ps1" -Stop
    }

    if (-not $SkipPrometheus) {
        Write-ColorOutput "Stopping Prometheus..." "Yellow"
        & ".\scripts\setup-prometheus-windows.ps1" -Stop
    }

    Write-ColorOutput "✅ Enhanced monitoring stack stopped!" "Green"
}

function Show-ServiceStatus {
    Write-ColorOutput "📊 Enhanced Monitoring Stack Status" "Cyan"
    Write-ColorOutput "====================================" "Cyan"
    Write-ColorOutput ""

    # Check Prometheus
    if (Test-ServiceRunning "http://localhost:9090/-/ready") {
        Write-ColorOutput "✅ Prometheus: Running on http://localhost:9090" "Green"
    } else {
        Write-ColorOutput "❌ Prometheus: Not running" "Red"
    }

    # Check Alertmanager
    if (Test-ServiceRunning "http://localhost:9093/-/healthy") {
        Write-ColorOutput "✅ Alertmanager: Running on http://localhost:9093" "Green"
    } else {
        Write-ColorOutput "❌ Alertmanager: Not running" "Red"
    }

    # Check Grafana
    if (Test-ServiceRunning "http://localhost:3000/api/health") {
        Write-ColorOutput "✅ Grafana: Running on http://localhost:3000" "Green"
    } else {
        Write-ColorOutput "❌ Grafana: Not running" "Red"
    }

    # Check MonitoringWorker
    if (Test-ServiceRunning "http://localhost:5000/health") {
        Write-ColorOutput "✅ MonitoringWorker: Running on http://localhost:5000" "Green"
    } else {
        Write-ColorOutput "⚠️  MonitoringWorker: Not running (start with 'dotnet run')" "Yellow"
    }

    Write-ColorOutput ""
    Write-ColorOutput "🔗 Service URLs:" "Cyan"
    Write-ColorOutput "  • MonitoringWorker Health: http://localhost:5000/health" "White"
    Write-ColorOutput "  • MonitoringWorker Metrics: http://localhost:5000/metrics" "White"
    Write-ColorOutput "  • MonitoringWorker API: http://localhost:5000/swagger" "White"
    Write-ColorOutput "  • Prometheus: http://localhost:9090" "White"
    Write-ColorOutput "  • Prometheus Targets: http://localhost:9090/targets" "White"
    Write-ColorOutput "  • Prometheus Alerts: http://localhost:9090/alerts" "White"
    Write-ColorOutput "  • Alertmanager: http://localhost:9093" "White"
    Write-ColorOutput "  • Alertmanager API: http://localhost:9093/api/v1/status" "White"
    Write-ColorOutput "  • Grafana: http://localhost:3000 (admin/admin123)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "📊 Dashboard URLs:" "Cyan"
    Write-ColorOutput "  • Executive Dashboard: http://localhost:3000/d/monitoring-worker-executive" "White"
    Write-ColorOutput "  • Operational Dashboard: http://localhost:3000/d/monitoring-worker-overview" "White"
    Write-ColorOutput "  • Correlation Dashboard: http://localhost:3000/d/monitoring-worker-correlation" "White"
    Write-ColorOutput "  • System Performance: http://localhost:3000/d/system-performance" "White"
}

function Show-Help {
    Write-ColorOutput "Enhanced Monitoring Stack for MonitoringWorker" "Cyan"
    Write-ColorOutput "==============================================" "Cyan"
    Write-ColorOutput ""
    Write-ColorOutput "This script manages Prometheus, Alertmanager, and Grafana for comprehensive monitoring." "White"
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\start-enhanced-monitoring.ps1 [OPTIONS]" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Options:" "Yellow"
    Write-ColorOutput "  -Download           Download all monitoring components" "White"
    Write-ColorOutput "  -Status             Show status of all services" "White"
    Write-ColorOutput "  -Stop               Stop all monitoring services" "White"
    Write-ColorOutput "  -SkipPrometheus     Skip Prometheus operations" "White"
    Write-ColorOutput "  -SkipAlertmanager   Skip Alertmanager operations" "White"
    Write-ColorOutput "  -SkipGrafana        Skip Grafana operations" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" "Yellow"
    Write-ColorOutput "  .\start-enhanced-monitoring.ps1 -Download" "Gray"
    Write-ColorOutput "  .\start-enhanced-monitoring.ps1" "Gray"
    Write-ColorOutput "  .\start-enhanced-monitoring.ps1 -Status" "Gray"
    Write-ColorOutput "  .\start-enhanced-monitoring.ps1 -Stop" "Gray"
    Write-ColorOutput "  .\start-enhanced-monitoring.ps1 -SkipGrafana" "Gray"
    Write-ColorOutput ""
    Write-ColorOutput "Features:" "Yellow"
    Write-ColorOutput "  • Prometheus metrics collection and alerting rules" "White"
    Write-ColorOutput "  • Alertmanager for intelligent alert routing" "White"
    Write-ColorOutput "  • Grafana dashboards with executive and operational views" "White"
    Write-ColorOutput "  • SLA monitoring and violation tracking" "White"
    Write-ColorOutput "  • Multi-channel notifications (email, Slack, Teams)" "White"
    Write-ColorOutput "  • Performance baselines and anomaly detection" "White"
}

# Main execution
try {
    if ($Status) {
        Show-ServiceStatus
    }
    elseif ($Stop) {
        Stop-EnhancedMonitoring
    }
    elseif ($Download -or (-not $SkipPrometheus -and -not $SkipAlertmanager -and -not $SkipGrafana)) {
        Start-EnhancedMonitoring
    }
    else {
        Show-Help
    }
}
catch {
    Write-ColorOutput "❌ Script execution failed: $($_.Exception.Message)" "Red"
    Write-ColorOutput "Stack trace: $($_.ScriptStackTrace)" "Gray"
    exit 1
}
