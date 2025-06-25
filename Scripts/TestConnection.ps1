# Simple connection test script
param(
    [string]$Server = "192.168.166.11,1433",
    [string]$Database = "PopAI",
    [string]$Username = "conexusadmin",
    [string]$Password = "PWUi^g6~lxD"
)

Write-Host "Testing connection to SQL Server..." -ForegroundColor Yellow
Write-Host "Server: $Server" -ForegroundColor Cyan
Write-Host "Database: $Database" -ForegroundColor Cyan
Write-Host "Username: $Username" -ForegroundColor Cyan

# Test basic connectivity first
try {
    Write-Host "Testing network connectivity..." -ForegroundColor Yellow
    $tcpTest = Test-NetConnection -ComputerName "192.168.166.11" -Port 1433 -WarningAction SilentlyContinue
    if ($tcpTest.TcpTestSucceeded) {
        Write-Host "Network connectivity: SUCCESS" -ForegroundColor Green
    } else {
        Write-Host "Network connectivity: FAILED" -ForegroundColor Red
        Write-Host "Cannot reach server on port 1433" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Warning "Could not test network connectivity: $($_.Exception.Message)"
}

# Try different connection string formats
$connectionStrings = @(
    "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;TrustServerCertificate=true;",
    "Data Source=$Server;Initial Catalog=$Database;User ID=$Username;Password=$Password;TrustServerCertificate=true;",
    "Server=$Server;Database=$Database;User Id=$Username;Password=$Password;Encrypt=false;",
    "Data Source=$Server;Initial Catalog=$Database;User ID=$Username;Password=$Password;Encrypt=false;"
)

foreach ($i in 0..($connectionStrings.Length - 1)) {
    $connStr = $connectionStrings[$i]
    Write-Host "`nTrying connection string format $($i + 1)..." -ForegroundColor Yellow
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $connection.Open()
        
        Write-Host "SUCCESS! Connected to database." -ForegroundColor Green
        Write-Host "Server Version: $($connection.ServerVersion)" -ForegroundColor Green
        Write-Host "Database: $($connection.Database)" -ForegroundColor Green
        Write-Host "Connection String: $connStr" -ForegroundColor Cyan
        
        # Test a simple query
        $command = New-Object System.Data.SqlClient.SqlCommand("SELECT @@VERSION", $connection)
        $version = $command.ExecuteScalar()
        Write-Host "SQL Server Version: $version" -ForegroundColor Green
        
        $connection.Close()
        
        # Save working connection string
        $connStr | Out-File -FilePath "WorkingConnectionString.txt" -Encoding UTF8
        Write-Host "Working connection string saved to WorkingConnectionString.txt" -ForegroundColor Green
        
        exit 0
        
    } catch {
        Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    } finally {
        if ($connection -and $connection.State -eq 'Open') {
            $connection.Close()
        }
    }
}

Write-Host "`nAll connection attempts failed." -ForegroundColor Red
exit 1
