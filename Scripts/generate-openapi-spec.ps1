# Generate OpenAPI Specification for Frontend Team
# This script extracts the OpenAPI specification from the running MonitoringWorker service

param(
    [string]$BaseUrl = "http://localhost:5002",
    [string]$OutputPath = "docs/openapi-specification.json",
    [switch]$Pretty
)

Write-Host "MonitoringWorker OpenAPI Specification Generator" -ForegroundColor Green
Write-Host "===============================================" -ForegroundColor Green
Write-Host ""

# Function to test if service is running
function Test-ServiceRunning {
    param([string]$Url)
    
    try {
        $response = Invoke-WebRequest -Uri "$Url/api" -Method GET -TimeoutSec 5 -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

# Function to download OpenAPI specification
function Get-OpenApiSpec {
    param([string]$BaseUrl, [string]$OutputPath, [bool]$PrettyFormat)
    
    try {
        $specUrl = "$BaseUrl/swagger/v1/swagger.json"
        Write-Host "Downloading OpenAPI specification from: $specUrl" -ForegroundColor Cyan
        
        $response = Invoke-WebRequest -Uri $specUrl -Method GET -ErrorAction Stop
        $jsonContent = $response.Content
        
        if ($PrettyFormat) {
            # Pretty format the JSON
            $jsonObject = $jsonContent | ConvertFrom-Json
            $jsonContent = $jsonObject | ConvertTo-Json -Depth 100 -Compress:$false
        }
        
        # Ensure output directory exists
        $outputDir = Split-Path $OutputPath -Parent
        if (-not (Test-Path $outputDir)) {
            New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
        }
        
        # Save the specification
        $jsonContent | Out-File -FilePath $OutputPath -Encoding UTF8
        
        Write-Host "✅ OpenAPI specification saved to: $OutputPath" -ForegroundColor Green
        
        # Display basic info about the API
        $spec = $jsonContent | ConvertFrom-Json
        Write-Host ""
        Write-Host "API Information:" -ForegroundColor Yellow
        Write-Host "  Title: $($spec.info.title)" -ForegroundColor White
        Write-Host "  Version: $($spec.info.version)" -ForegroundColor White
        Write-Host "  Description: $($spec.info.description)" -ForegroundColor White
        
        if ($spec.paths) {
            $endpointCount = ($spec.paths | Get-Member -MemberType NoteProperty).Count
            Write-Host "  Endpoints: $endpointCount" -ForegroundColor White
        }
        
        if ($spec.components -and $spec.components.schemas) {
            $schemaCount = ($spec.components.schemas | Get-Member -MemberType NoteProperty).Count
            Write-Host "  Data Models: $schemaCount" -ForegroundColor White
        }
        
        return $true
    }
    catch {
        Write-Host "❌ Failed to download OpenAPI specification: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Function to generate additional documentation
function Generate-AdditionalDocs {
    param([string]$SpecPath)
    
    try {
        $spec = Get-Content $SpecPath | ConvertFrom-Json
        
        # Generate endpoint summary
        $endpointSummary = @"
# API Endpoints Summary
Generated from OpenAPI specification

## Available Endpoints

"@
        
        foreach ($path in $spec.paths.PSObject.Properties) {
            $endpointSummary += "`n### $($path.Name)`n"
            
            foreach ($method in $path.Value.PSObject.Properties) {
                $operation = $method.Value
                $summary = if ($operation.summary) { $operation.summary } else { "No description" }
                $endpointSummary += "- **$($method.Name.ToUpper())** - $summary`n"
            }
        }
        
        $summaryPath = "docs/api-endpoints-summary.md"
        $endpointSummary | Out-File -FilePath $summaryPath -Encoding UTF8
        Write-Host "✅ Endpoint summary saved to: $summaryPath" -ForegroundColor Green
        
        return $true
    }
    catch {
        Write-Host "⚠️ Could not generate additional documentation: $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Main execution
Write-Host "Checking if MonitoringWorker service is running..." -ForegroundColor Cyan

if (Test-ServiceRunning -Url $BaseUrl) {
    Write-Host "✅ Service is running at $BaseUrl" -ForegroundColor Green
    
    if (Get-OpenApiSpec -BaseUrl $BaseUrl -OutputPath $OutputPath -PrettyFormat $Pretty) {
        Write-Host ""
        Write-Host "Generating additional documentation..." -ForegroundColor Cyan
        Generate-AdditionalDocs -SpecPath $OutputPath | Out-Null
        
        Write-Host ""
        Write-Host "📋 Files Generated:" -ForegroundColor Yellow
        Write-Host "  - OpenAPI Specification: $OutputPath" -ForegroundColor White
        Write-Host "  - Endpoint Summary: docs/api-endpoints-summary.md" -ForegroundColor White
        Write-Host "  - Swagger Documentation: docs/SWAGGER_API_DOCUMENTATION_SUMMARY.md" -ForegroundColor White
        
        Write-Host ""
        Write-Host "🚀 Frontend Integration:" -ForegroundColor Yellow
        Write-Host "  1. Use the OpenAPI specification for code generation" -ForegroundColor White
        Write-Host "  2. Access Swagger UI at: $BaseUrl/swagger" -ForegroundColor White
        Write-Host "  3. Test endpoints directly in the browser" -ForegroundColor White
        Write-Host "  4. Generate client libraries using OpenAPI tools" -ForegroundColor White
        
        Write-Host ""
        Write-Host "✅ OpenAPI specification generation completed successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Failed to generate OpenAPI specification" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "❌ MonitoringWorker service is not running at $BaseUrl" -ForegroundColor Red
    Write-Host ""
    Write-Host "To start the service:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project MonitoringWorker.csproj" -ForegroundColor White
    Write-Host ""
    Write-Host "Alternative: Generate from static configuration" -ForegroundColor Yellow
    Write-Host "  The service includes comprehensive Swagger configuration" -ForegroundColor White
    Write-Host "  Start the service to access the full OpenAPI specification" -ForegroundColor White
    
    # Try to provide static information
    Write-Host ""
    Write-Host "📋 Static API Information Available:" -ForegroundColor Cyan
    Write-Host "  - Swagger Documentation: docs/SWAGGER_API_DOCUMENTATION_SUMMARY.md" -ForegroundColor White
    Write-Host "  - Frontend Guide: docs/frontend-worker-testing-guide.md" -ForegroundColor White
    Write-Host "  - API Changes: Frontend_API_Changes_Summary.md" -ForegroundColor White
    
    exit 1
}

Write-Host ""
Write-Host "🎉 Ready for frontend integration!" -ForegroundColor Green
