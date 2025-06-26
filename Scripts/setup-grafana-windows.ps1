# PowerShell script to set up Grafana natively on Windows
param(
    [switch]$Download,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status
)

$GrafanaVersion = "10.1.0"
$GrafanaDir = ".\tools\grafana"
$GrafanaExe = "$GrafanaDir\bin\grafana-server.exe"
$GrafanaConfig = "$GrafanaDir\conf\custom.ini"

Write-Host "Grafana Windows Setup" -ForegroundColor Green

function Test-GrafanaInstalled {
    return Test-Path $GrafanaExe
}

function Download-Grafana {
    Write-Host "Downloading Grafana $GrafanaVersion for Windows..." -ForegroundColor Yellow
    
    if (-not (Test-Path ".\tools")) {
        New-Item -ItemType Directory -Path ".\tools" | Out-Null
    }
    
    $downloadUrl = "https://dl.grafana.com/oss/release/grafana-$GrafanaVersion.windows-amd64.zip"
    $zipFile = ".\tools\grafana-$GrafanaVersion.zip"
    
    try {
        Write-Host "Downloading from: $downloadUrl" -ForegroundColor Cyan
        Invoke-WebRequest -Uri $downloadUrl -OutFile $zipFile -UseBasicParsing
        
        Write-Host "Extracting Grafana..." -ForegroundColor Yellow
        Expand-Archive -Path $zipFile -DestinationPath ".\tools" -Force
        
        $extractedDir = ".\tools\grafana-$GrafanaVersion"
        if (Test-Path $extractedDir) {
            if (Test-Path $GrafanaDir) {
                Remove-Item $GrafanaDir -Recurse -Force
            }
            Copy-Item $extractedDir $GrafanaDir -Recurse -Force
        }
        
        Remove-Item $zipFile -Force
        
        # Create custom configuration
        Setup-GrafanaConfig
        
        Write-Host "Grafana downloaded successfully!" -ForegroundColor Green
        Write-Host "Location: $GrafanaDir" -ForegroundColor Cyan
        
    } catch {
        Write-Host "Failed to download Grafana: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
}

function Setup-GrafanaConfig {
    Write-Host "Creating Grafana configuration..." -ForegroundColor Yellow
    
    $configContent = @"
[server]
http_port = 3000
domain = localhost
root_url = http://localhost:3000/

[database]
type = sqlite3
path = grafana.db

[security]
admin_user = admin
admin_password = admin123
secret_key = SW2YcwTIb9zpOOhoPsMm

[users]
allow_sign_up = false
allow_org_create = false
auto_assign_org = true
auto_assign_org_role = Viewer

[auth.anonymous]
enabled = false

[analytics]
reporting_enabled = false
check_for_updates = false

[log]
mode = console file
level = info

[paths]
data = .\data\grafana
logs = .\data\grafana\logs
plugins = .\data\grafana\plugins
provisioning = .\grafana\provisioning
"@

    # Ensure config directory exists
    $configDir = Split-Path $GrafanaConfig -Parent
    if (-not (Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }
    
    # Write configuration
    $configContent | Out-File -FilePath $GrafanaConfig -Encoding UTF8
    
    # Create data directories
    if (-not (Test-Path ".\data\grafana")) {
        New-Item -ItemType Directory -Path ".\data\grafana" -Force | Out-Null
        New-Item -ItemType Directory -Path ".\data\grafana\logs" -Force | Out-Null
        New-Item -ItemType Directory -Path ".\data\grafana\plugins" -Force | Out-Null
    }
}

function Start-Grafana {
    if (-not (Test-GrafanaInstalled)) {
        Write-Host "Grafana not found. Run with -Download first." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Starting Grafana..." -ForegroundColor Yellow
    Write-Host "Config: $GrafanaConfig" -ForegroundColor Cyan
    Write-Host "Web UI: http://localhost:3000" -ForegroundColor Cyan
    Write-Host "Login: admin / admin123" -ForegroundColor Cyan
    
    # Set working directory to Grafana directory
    $originalLocation = Get-Location
    Set-Location $GrafanaDir
    
    try {
        # Start Grafana with custom config
        $arguments = @(
            "--config=$GrafanaConfig",
            "--homepath=.",
            "--packaging=zip"
        )
        
        Start-Process -FilePath ".\bin\grafana-server.exe" -ArgumentList $arguments -WindowStyle Normal
        
        Write-Host "Grafana started!" -ForegroundColor Green
        Write-Host "Waiting for startup..." -ForegroundColor Yellow
        Start-Sleep -Seconds 10
        
        # Test if Grafana is responding
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:3000/api/health" -UseBasicParsing -TimeoutSec 15
            if ($response.StatusCode -eq 200) {
                Write-Host "Grafana is healthy and ready!" -ForegroundColor Green
                Write-Host "Visit: http://localhost:3000" -ForegroundColor Cyan
                Write-Host "Login: admin / admin123" -ForegroundColor Cyan
            }
        } catch {
            Write-Host "Grafana may still be starting up. Check http://localhost:3000" -ForegroundColor Yellow
        }
    } finally {
        Set-Location $originalLocation
    }
}

function Stop-Grafana {
    Write-Host "Stopping Grafana..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name "grafana-server" -ErrorAction SilentlyContinue
    if ($processes) {
        $processes | Stop-Process -Force
        Write-Host "Grafana stopped!" -ForegroundColor Green
    } else {
        Write-Host "Grafana is not running" -ForegroundColor Cyan
    }
}

function Show-Status {
    Write-Host "Grafana Status" -ForegroundColor Cyan
    
    if (Test-GrafanaInstalled) {
        Write-Host "Grafana is installed: $GrafanaExe" -ForegroundColor Green
    } else {
        Write-Host "Grafana is not installed" -ForegroundColor Red
    }
    
    $processes = Get-Process -Name "grafana-server" -ErrorAction SilentlyContinue
    if ($processes) {
        $pidList = $processes.Id -join ", "
        Write-Host "Grafana is running (PID: $pidList)" -ForegroundColor Green
        Write-Host "Web UI: http://localhost:3000" -ForegroundColor Cyan
        Write-Host "Login: admin / admin123" -ForegroundColor Cyan
    } else {
        Write-Host "Grafana is not running" -ForegroundColor Red
    }
    
    if (Test-Path $GrafanaConfig) {
        Write-Host "Config file exists: $GrafanaConfig" -ForegroundColor Green
    } else {
        Write-Host "Config file missing: $GrafanaConfig" -ForegroundColor Red
    }
}

# Main logic
if ($Download) {
    Download-Grafana
} elseif ($Start) {
    Start-Grafana
} elseif ($Stop) {
    Stop-Grafana
} elseif ($Status) {
    Show-Status
} else {
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\setup-grafana-windows.ps1 -Download   # Download Grafana" -ForegroundColor White
    Write-Host "  .\setup-grafana-windows.ps1 -Start      # Start Grafana" -ForegroundColor White
    Write-Host "  .\setup-grafana-windows.ps1 -Stop       # Stop Grafana" -ForegroundColor White
    Write-Host "  .\setup-grafana-windows.ps1 -Status     # Show status" -ForegroundColor White
    Write-Host ""
    Write-Host "Quick setup:" -ForegroundColor Cyan
    Write-Host "  1. .\setup-grafana-windows.ps1 -Download" -ForegroundColor White
    Write-Host "  2. .\setup-grafana-windows.ps1 -Start" -ForegroundColor White
    Write-Host "  3. Visit http://localhost:3000 (admin/admin123)" -ForegroundColor White
    Write-Host "  4. Add Prometheus data source: http://localhost:9090" -ForegroundColor White
}
