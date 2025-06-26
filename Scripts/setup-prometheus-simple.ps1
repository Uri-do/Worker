# Simple PowerShell script to set up Prometheus on Windows
param(
    [switch]$Download,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status
)

$PrometheusVersion = "2.47.0"
$PrometheusDir = ".\tools\prometheus"
$PrometheusExe = "$PrometheusDir\prometheus.exe"
$PrometheusConfig = ".\prometheus\prometheus.yml"

Write-Host "Prometheus Windows Setup" -ForegroundColor Green

function Test-PrometheusInstalled {
    return Test-Path $PrometheusExe
}

function Download-Prometheus {
    Write-Host "Downloading Prometheus $PrometheusVersion for Windows..." -ForegroundColor Yellow
    
    if (-not (Test-Path ".\tools")) {
        New-Item -ItemType Directory -Path ".\tools" | Out-Null
    }
    
    $downloadUrl = "https://github.com/prometheus/prometheus/releases/download/v$PrometheusVersion/prometheus-$PrometheusVersion.windows-amd64.zip"
    $zipFile = ".\tools\prometheus-$PrometheusVersion.zip"
    
    try {
        Write-Host "Downloading from: $downloadUrl" -ForegroundColor Cyan
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile -UseBasicParsing
        
        Write-Host "Extracting Prometheus..." -ForegroundColor Yellow
        Expand-Archive -Path $zipFile -DestinationPath ".\tools" -Force
        
        $extractedDir = ".\tools\prometheus-$PrometheusVersion.windows-amd64"
        if (Test-Path $extractedDir) {
            if (Test-Path $PrometheusDir) {
                Remove-Item $PrometheusDir -Recurse -Force
            }
            Rename-Item $extractedDir $PrometheusDir
        }
        
        Remove-Item $zipFile -Force
        
        Write-Host "Prometheus downloaded successfully!" -ForegroundColor Green
        Write-Host "Location: $PrometheusDir" -ForegroundColor Cyan
        
    } catch {
        Write-Host "Failed to download Prometheus: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

function Start-Prometheus {
    if (-not (Test-PrometheusInstalled)) {
        Write-Host "Prometheus not found. Run with -Download first." -ForegroundColor Red
        exit 1
    }
    
    if (-not (Test-Path $PrometheusConfig)) {
        Write-Host "Prometheus config not found: $PrometheusConfig" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Starting Prometheus..." -ForegroundColor Yellow
    Write-Host "Config: $PrometheusConfig" -ForegroundColor Cyan
    Write-Host "Web UI: http://localhost:9090" -ForegroundColor Cyan
    
    $arguments = @(
        "--config.file=$PrometheusConfig",
        "--storage.tsdb.path=.\data\prometheus",
        "--storage.tsdb.retention.time=15d",
        "--storage.tsdb.retention.size=1GB",
        "--web.console.libraries=$PrometheusDir\console_libraries",
        "--web.console.templates=$PrometheusDir\consoles",
        "--web.enable-lifecycle",
        "--web.enable-admin-api",
        "--log.level=info"
    )
    
    if (-not (Test-Path ".\data\prometheus")) {
        New-Item -ItemType Directory -Path ".\data\prometheus" -Force | Out-Null
    }
    
    Start-Process -FilePath $PrometheusExe -ArgumentList $arguments -WindowStyle Normal
    
    Write-Host "Prometheus started!" -ForegroundColor Green
    Write-Host "Waiting for startup..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:9090/-/healthy" -UseBasicParsing -TimeoutSec 10
        if ($response.StatusCode -eq 200) {
            Write-Host "Prometheus is healthy and ready!" -ForegroundColor Green
        }
    } catch {
        Write-Host "Prometheus may still be starting up. Check http://localhost:9090" -ForegroundColor Yellow
    }
}

function Stop-Prometheus {
    Write-Host "Stopping Prometheus..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "prometheus" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "Prometheus stopped!" -ForegroundColor Green
    } else {
        Write-Host "Prometheus is not running" -ForegroundColor Cyan
    }
}

function Show-Status {
    Write-Host "Prometheus Status" -ForegroundColor Cyan
    
    if (Test-PrometheusInstalled) {
        Write-Host "Prometheus is installed: $PrometheusExe" -ForegroundColor Green
    } else {
        Write-Host "Prometheus is not installed" -ForegroundColor Red
    }
    
    $processes = Get-Process -Name "prometheus" -ErrorAction SilentlyContinue
    if ($processes) {
        $pidList = $processes.Id -join ", "
        Write-Host "Prometheus is running (PID: $pidList)" -ForegroundColor Green
        Write-Host "Web UI: http://localhost:9090" -ForegroundColor Cyan
        Write-Host "Targets: http://localhost:9090/targets" -ForegroundColor Cyan
    } else {
        Write-Host "Prometheus is not running" -ForegroundColor Red
    }
    
    if (Test-Path $PrometheusConfig) {
        Write-Host "Config file exists: $PrometheusConfig" -ForegroundColor Green
    } else {
        Write-Host "Config file missing: $PrometheusConfig" -ForegroundColor Red
    }
}

# Main logic
if ($Download) {
    Download-Prometheus
} elseif ($Start) {
    Start-Prometheus
} elseif ($Stop) {
    Stop-Prometheus
} elseif ($Status) {
    Show-Status
} else {
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\setup-prometheus-simple.ps1 -Download   # Download Prometheus" -ForegroundColor White
    Write-Host "  .\setup-prometheus-simple.ps1 -Start      # Start Prometheus" -ForegroundColor White
    Write-Host "  .\setup-prometheus-simple.ps1 -Stop       # Stop Prometheus" -ForegroundColor White
    Write-Host "  .\setup-prometheus-simple.ps1 -Status     # Show status" -ForegroundColor White
    Write-Host ""
    Write-Host "Quick setup:" -ForegroundColor Cyan
    Write-Host "  1. .\setup-prometheus-simple.ps1 -Download" -ForegroundColor White
    Write-Host "  2. .\setup-prometheus-simple.ps1 -Start" -ForegroundColor White
    Write-Host "  3. Start MonitoringWorker: dotnet run" -ForegroundColor White
    Write-Host "  4. Visit http://localhost:9090" -ForegroundColor White
}
