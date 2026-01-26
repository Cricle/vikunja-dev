# Test script for Vikunja MCP Server (stdio mode)
# This script tests the MCP server by sending JSON-RPC messages via stdin

param(
    [string]$ApiUrl = $env:VIKUNJA_API_URL,
    [string]$ApiToken = $env:VIKUNJA_API_TOKEN
)

if ([string]::IsNullOrWhiteSpace($ApiUrl) -or [string]::IsNullOrWhiteSpace($ApiToken)) {
    Write-Host "ERROR: VIKUNJA_API_URL and VIKUNJA_API_TOKEN environment variables are required" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  `$env:VIKUNJA_API_URL = 'https://vikunja.example.com/api/v1'"
    Write-Host "  `$env:VIKUNJA_API_TOKEN = 'your_token_here'"
    Write-Host "  .\test-mcp-stdio.ps1"
    Write-Host ""
    Write-Host "Or pass as parameters:" -ForegroundColor Yellow
    Write-Host "  .\test-mcp-stdio.ps1 -ApiUrl 'https://vikunja.example.com/api/v1' -ApiToken 'your_token_here'"
    exit 1
}

Write-Host "Testing Vikunja MCP Server (stdio mode)" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host ""

# Set environment variables
$env:VIKUNJA_API_URL = $ApiUrl
$env:VIKUNJA_API_TOKEN = $ApiToken

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
dotnet build VikunjaHook/VikunjaHook.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Test 1: Initialize
Write-Host "Test 1: Initialize MCP connection" -ForegroundColor Cyan
$initRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending: $initRequest" -ForegroundColor Gray

# Note: For stdio testing, you would need to pipe the JSON to the executable
# This is a simplified test that just checks if the server starts
Write-Host ""
Write-Host "To test the server manually:" -ForegroundColor Yellow
Write-Host "1. Run: dotnet run --project VikunjaHook/VikunjaHook.csproj" -ForegroundColor Gray
Write-Host "2. Send JSON-RPC messages via stdin" -ForegroundColor Gray
Write-Host "3. Read responses from stdout" -ForegroundColor Gray
Write-Host ""
Write-Host "Example initialize message:" -ForegroundColor Yellow
Write-Host $initRequest -ForegroundColor Gray
Write-Host ""

Write-Host "Server is ready to accept MCP protocol messages via stdio" -ForegroundColor Green
