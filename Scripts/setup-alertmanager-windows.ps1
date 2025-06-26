# Alertmanager Setup Script for Windows
# This script downloads, configures, and manages Alertmanager for MonitoringWorker

param(
    [switch]$Download,
    [switch]$Start,
    [switch]$Stop,
    [switch]$Status,
    [switch]$Install,
    [switch]$Uninstall,
    [string]$Version = "0.26.0"
)

$ErrorActionPreference = "Stop"

# Configuration
$AlertmanagerVersion = $Version
$AlertmanagerUrl = "https://github.com/prometheus/alertmanager/releases/download/v$AlertmanagerVersion/alertmanager-$AlertmanagerVersion.windows-amd64.zip"
$AlertmanagerDir = "alertmanager"
$AlertmanagerExe = "$AlertmanagerDir\alertmanager.exe"
$AlertmanagerConfig = "alertmanager\alertmanager.yml"
$AlertmanagerPort = 9093

function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

function Test-AlertmanagerRunning {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$AlertmanagerPort/-/healthy" -TimeoutSec 5 -UseBasicParsing
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Download-Alertmanager {
    Write-ColorOutput "🔽 Downloading Alertmanager v$AlertmanagerVersion..." "Cyan"
    
    if (Test-Path $AlertmanagerDir) {
        Write-ColorOutput "⚠️  Alertmanager directory already exists. Removing..." "Yellow"
        Remove-Item -Path $AlertmanagerDir -Recurse -Force
    }

    $tempFile = "alertmanager-temp.zip"
    
    try {
        Write-ColorOutput "Downloading from: $AlertmanagerUrl" "Gray"
        Invoke-WebRequest -Uri $AlertmanagerUrl -OutFile $tempFile -UseBasicParsing
        
        Write-ColorOutput "📦 Extracting Alertmanager..." "Cyan"
        Expand-Archive -Path $tempFile -DestinationPath "." -Force
        
        # Rename extracted directory
        $extractedDir = "alertmanager-$AlertmanagerVersion.windows-amd64"
        if (Test-Path $extractedDir) {
            Rename-Item -Path $extractedDir -NewName $AlertmanagerDir
        }
        
        Remove-Item -Path $tempFile -Force
        
        Write-ColorOutput "✅ Alertmanager downloaded and extracted successfully!" "Green"
        
        # Copy configuration if it doesn't exist
        if (-not (Test-Path $AlertmanagerConfig)) {
            Write-ColorOutput "📋 Copying default configuration..." "Cyan"
            Copy-Item -Path "$AlertmanagerDir\alertmanager.yml" -Destination $AlertmanagerConfig -Force
        }
        
    }
    catch {
        Write-ColorOutput "❌ Failed to download Alertmanager: $($_.Exception.Message)" "Red"
        if (Test-Path $tempFile) {
            Remove-Item -Path $tempFile -Force
        }
        exit 1
    }
}

function Start-Alertmanager {
    if (Test-AlertmanagerRunning) {
        Write-ColorOutput "✅ Alertmanager is already running on port $AlertmanagerPort" "Green"
        return
    }

    if (-not (Test-Path $AlertmanagerExe)) {
        Write-ColorOutput "❌ Alertmanager executable not found. Please run with -Download first." "Red"
        exit 1
    }

    if (-not (Test-Path $AlertmanagerConfig)) {
        Write-ColorOutput "❌ Alertmanager configuration not found at $AlertmanagerConfig" "Red"
        exit 1
    }

    Write-ColorOutput "🚀 Starting Alertmanager..." "Cyan"
    
    $arguments = @(
        "--config.file=$AlertmanagerConfig",
        "--storage.path=alertmanager/data",
        "--web.listen-address=:$AlertmanagerPort",
        "--log.level=info"
    )
    
    try {
        $process = Start-Process -FilePath $AlertmanagerExe -ArgumentList $arguments -PassThru -WindowStyle Hidden
        
        # Wait for startup
        $timeout = 30
        $elapsed = 0
        
        Write-ColorOutput "⏳ Waiting for Alertmanager to start..." "Yellow"
        
        while ($elapsed -lt $timeout) {
            Start-Sleep -Seconds 2
            $elapsed += 2
            
            if (Test-AlertmanagerRunning) {
                Write-ColorOutput "✅ Alertmanager started successfully!" "Green"
                Write-ColorOutput "🌐 Web UI: http://localhost:$AlertmanagerPort" "Cyan"
                Write-ColorOutput "📊 API: http://localhost:$AlertmanagerPort/api/v1/status" "Cyan"
                return
            }
        }
        
        Write-ColorOutput "❌ Alertmanager failed to start within $timeout seconds" "Red"
        exit 1
    }
    catch {
        Write-ColorOutput "❌ Failed to start Alertmanager: $($_.Exception.Message)" "Red"
        exit 1
    }
}

function Stop-Alertmanager {
    Write-ColorOutput "🛑 Stopping Alertmanager..." "Cyan"
    
    $processes = Get-Process -Name "alertmanager" -ErrorAction SilentlyContinue
    
    if ($processes) {
        foreach ($process in $processes) {
            try {
                $process.Kill()
                $process.WaitForExit(10000)
                Write-ColorOutput "✅ Alertmanager process stopped (PID: $($process.Id))" "Green"
            }
            catch {
                Write-ColorOutput "⚠️  Failed to stop Alertmanager process (PID: $($process.Id))" "Yellow"
            }
        }
    }
    else {
        Write-ColorOutput "ℹ️  No Alertmanager processes found" "Gray"
    }
}

function Get-AlertmanagerStatus {
    Write-ColorOutput "📊 Alertmanager Status Check" "Cyan"
    Write-ColorOutput "================================" "Cyan"
    
    # Check if executable exists
    if (Test-Path $AlertmanagerExe) {
        Write-ColorOutput "✅ Executable: Found at $AlertmanagerExe" "Green"
    }
    else {
        Write-ColorOutput "❌ Executable: Not found" "Red"
    }
    
    # Check if configuration exists
    if (Test-Path $AlertmanagerConfig) {
        Write-ColorOutput "✅ Configuration: Found at $AlertmanagerConfig" "Green"
    }
    else {
        Write-ColorOutput "❌ Configuration: Not found" "Red"
    }
    
    # Check if running
    if (Test-AlertmanagerRunning) {
        Write-ColorOutput "✅ Service: Running on port $AlertmanagerPort" "Green"
        
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:$AlertmanagerPort/api/v1/status" -UseBasicParsing
            Write-ColorOutput "✅ API: Responding" "Green"
            Write-ColorOutput "ℹ️  Version: $($response.data.versionInfo.version)" "Gray"
        }
        catch {
            Write-ColorOutput "⚠️  API: Not responding" "Yellow"
        }
    }
    else {
        Write-ColorOutput "❌ Service: Not running" "Red"
    }
    
    # Check processes
    $processes = Get-Process -Name "alertmanager" -ErrorAction SilentlyContinue
    if ($processes) {
        Write-ColorOutput "📋 Running Processes:" "Cyan"
        foreach ($process in $processes) {
            Write-ColorOutput "   PID: $($process.Id), CPU: $($process.CPU), Memory: $([math]::Round($process.WorkingSet64/1MB, 2)) MB" "Gray"
        }
    }
    
    Write-ColorOutput "" "White"
    Write-ColorOutput "🌐 Web Interface: http://localhost:$AlertmanagerPort" "Cyan"
    Write-ColorOutput "📊 API Endpoint: http://localhost:$AlertmanagerPort/api/v1/status" "Cyan"
    Write-ColorOutput "📋 Configuration: $AlertmanagerConfig" "Cyan"
}

function Install-AlertmanagerService {
    Write-ColorOutput "🔧 Installing Alertmanager as Windows Service..." "Cyan"
    
    if (-not (Test-Path $AlertmanagerExe)) {
        Write-ColorOutput "❌ Alertmanager executable not found. Please run with -Download first." "Red"
        exit 1
    }
    
    # This would require additional tools like NSSM or sc.exe
    Write-ColorOutput "⚠️  Service installation requires additional setup." "Yellow"
    Write-ColorOutput "Consider using NSSM (Non-Sucking Service Manager) for Windows service installation." "Yellow"
    Write-ColorOutput "Download NSSM from: https://nssm.cc/download" "Cyan"
}

function Uninstall-AlertmanagerService {
    Write-ColorOutput "🗑️  Uninstalling Alertmanager service..." "Cyan"
    
    # Stop any running processes first
    Stop-Alertmanager
    
    Write-ColorOutput "✅ Alertmanager service uninstalled" "Green"
}

function Show-Help {
    Write-ColorOutput "Alertmanager Setup Script for Windows" "Cyan"
    Write-ColorOutput "=====================================" "Cyan"
    Write-ColorOutput ""
    Write-ColorOutput "Usage: .\setup-alertmanager-windows.ps1 [OPTIONS]" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Options:" "Yellow"
    Write-ColorOutput "  -Download     Download and extract Alertmanager" "White"
    Write-ColorOutput "  -Start        Start Alertmanager" "White"
    Write-ColorOutput "  -Stop         Stop Alertmanager" "White"
    Write-ColorOutput "  -Status       Show Alertmanager status" "White"
    Write-ColorOutput "  -Install      Install as Windows service" "White"
    Write-ColorOutput "  -Uninstall    Uninstall Windows service" "White"
    Write-ColorOutput "  -Version      Specify Alertmanager version (default: $AlertmanagerVersion)" "White"
    Write-ColorOutput ""
    Write-ColorOutput "Examples:" "Yellow"
    Write-ColorOutput "  .\setup-alertmanager-windows.ps1 -Download" "Gray"
    Write-ColorOutput "  .\setup-alertmanager-windows.ps1 -Start" "Gray"
    Write-ColorOutput "  .\setup-alertmanager-windows.ps1 -Status" "Gray"
    Write-ColorOutput "  .\setup-alertmanager-windows.ps1 -Download -Version 0.25.0" "Gray"
    Write-ColorOutput ""
    Write-ColorOutput "Configuration:" "Yellow"
    Write-ColorOutput "  Config file: $AlertmanagerConfig" "Gray"
    Write-ColorOutput "  Web UI: http://localhost:$AlertmanagerPort" "Gray"
    Write-ColorOutput "  API: http://localhost:$AlertmanagerPort/api/v1/status" "Gray"
}

# Main execution
try {
    if ($Download) {
        Download-Alertmanager
    }
    elseif ($Start) {
        Start-Alertmanager
    }
    elseif ($Stop) {
        Stop-Alertmanager
    }
    elseif ($Status) {
        Get-AlertmanagerStatus
    }
    elseif ($Install) {
        Install-AlertmanagerService
    }
    elseif ($Uninstall) {
        Uninstall-AlertmanagerService
    }
    else {
        Show-Help
    }
}
catch {
    Write-ColorOutput "❌ Script execution failed: $($_.Exception.Message)" "Red"
    exit 1
}
