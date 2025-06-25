@echo off
echo Cleaning up existing processes...

REM Kill any existing dotnet processes
taskkill /f /im "dotnet.exe" >nul 2>&1
taskkill /f /im "MonitoringWorker.exe" >nul 2>&1

echo Waiting for processes to terminate...
timeout /t 3 /nobreak >nul

echo Building application...
dotnet build MonitoringWorker.sln

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    pause
    exit /b 1
)

echo Starting MonitoringWorker...
echo Application will be available at:
echo   - http://localhost:5002
echo   - https://localhost:5003
echo   - Swagger: http://localhost:5002/swagger
echo.
echo Press Ctrl+C to stop the application
echo.

dotnet run
