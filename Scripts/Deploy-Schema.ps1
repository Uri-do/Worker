# PowerShell script to deploy MonitoringWorker database schema
param(
    [string]$ConnectionString = "Server=192.168.166.11,1433;Database=PopAI;User Id=conexusadmin;Password=PWUi^g6~lxD;TrustServerCertificate=true;",
    [string]$SqlScriptPath = "Scripts/MonitoringWorker_Schema_Simple.sql",
    [switch]$WhatIf = $false
)

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARNING" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $color
}

function Test-DatabaseConnection {
    param([string]$ConnectionString)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        $command = New-Object System.Data.SqlClient.SqlCommand("SELECT @@VERSION, DB_NAME()", $connection)
        $reader = $command.ExecuteReader()
        
        if ($reader.Read()) {
            $version = $reader.GetString(0)
            $database = $reader.GetString(1)
            $reader.Close()
            
            Write-Log "Connected to database: $database" "SUCCESS"
            Write-Log "SQL Server version: $($version.Split("`n")[0])" "INFO"
            
            $connection.Close()
            return $true
        }
        
        $reader.Close()
        $connection.Close()
        return $false
        
    } catch {
        Write-Log "Connection failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Execute-SqlScript {
    param([string]$ConnectionString, [string]$ScriptPath)
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Log "SQL script not found: $ScriptPath" "ERROR"
        return $false
    }
    
    try {
        $scriptContent = Get-Content $ScriptPath -Raw
        
        if ($WhatIf) {
            Write-Log "WHAT-IF: Would execute SQL script: $ScriptPath" "INFO"
            Write-Log "Script size: $($scriptContent.Length) characters" "INFO"
            return $true
        }
        
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        
        # Split script by GO statements and execute each batch
        $batches = $scriptContent -split '\r?\nGO\r?\n'
        $batchCount = 0

        foreach ($batch in $batches) {
            $batch = $batch.Trim()
            if ([string]::IsNullOrWhiteSpace($batch)) {
                continue
            }

            $batchCount++
            Write-Log "Executing batch $batchCount..." "INFO"

            $command = New-Object System.Data.SqlClient.SqlCommand($batch, $connection)
            $command.CommandTimeout = 300

            try {
                $result = $command.ExecuteNonQuery()
                Write-Log "Batch $batchCount completed successfully" "SUCCESS"
            } catch {
                Write-Log "Error in batch $batchCount`: $($_.Exception.Message)" "ERROR"
                throw
            }
        }

        $connection.Close()
        
        Write-Log "SQL script executed successfully" "SUCCESS"
        return $true
        
    } catch {
        Write-Log "Error executing SQL script: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Main execution
Write-Log "MonitoringWorker Database Schema Deployment" "INFO"
Write-Log "=============================================" "INFO"

if ($WhatIf) {
    Write-Log "Running in WHAT-IF mode - no changes will be made" "WARNING"
}

# Test database connection
Write-Log "Testing database connection..." "INFO"
if (-not (Test-DatabaseConnection -ConnectionString $ConnectionString)) {
    Write-Log "Database connection failed. Aborting deployment." "ERROR"
    exit 1
}

# Execute schema script
Write-Log "Executing MonitoringWorker schema script..." "INFO"
if (Execute-SqlScript -ConnectionString $ConnectionString -ScriptPath $SqlScriptPath) {
    Write-Log "Schema deployment completed successfully" "SUCCESS"
} else {
    Write-Log "Schema deployment failed" "ERROR"
    exit 1
}

Write-Log "MonitoringWorker database schema deployment completed!" "SUCCESS"
