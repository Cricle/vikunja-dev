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

Write-Host "=== Vikunja MCP Server (stdio mode) Test ===" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host ""

# Set environment variables
$env:VIKUNJA_API_URL = $ApiUrl
$env:VIKUNJA_API_TOKEN = $ApiToken

# Build the project
Write-Host "Building project..." -ForegroundColor Yellow
$buildPath = Join-Path $PSScriptRoot "VikunjaHook"
dotnet build "$buildPath/VikunjaHook.csproj" -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Prepare test messages
$messages = @(
    @{
        name = "Initialize"
        message = @{
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
        }
    },
    @{
        name = "List Tools"
        message = @{
            jsonrpc = "2.0"
            id = 2
            method = "tools/list"
            params = @{}
        }
    }
)

Write-Host "Starting MCP server..." -ForegroundColor Cyan
Write-Host ""

# Start the MCP server process
$exePath = Join-Path $buildPath "bin/Release/net10.0/VikunjaHook.exe"
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $exePath
$psi.Arguments = "--mcp"
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.RedirectStandardOutput = $true
$psi.RedirectStandardError = $true
$psi.CreateNoWindow = $true
$psi.EnvironmentVariables["VIKUNJA_API_URL"] = $ApiUrl
$psi.EnvironmentVariables["VIKUNJA_API_TOKEN"] = $ApiToken

$process = New-Object System.Diagnostics.Process
$process.StartInfo = $psi

# Event handlers for output
$outputBuilder = New-Object System.Text.StringBuilder
$errorBuilder = New-Object System.Text.StringBuilder

$outputHandler = {
    if ($EventArgs.Data) {
        [void]$outputBuilder.AppendLine($EventArgs.Data)
    }
}

$errorHandler = {
    if ($EventArgs.Data) {
        [void]$errorBuilder.AppendLine($EventArgs.Data)
    }
}

$process.add_OutputDataReceived($outputHandler)
$process.add_ErrorDataReceived($errorHandler)

try {
    # Start the process
    [void]$process.Start()
    $process.BeginOutputReadLine()
    $process.BeginErrorReadLine()
    
    Write-Host "Server started (PID: $($process.Id))" -ForegroundColor Green
    Write-Host ""
    
    # Wait a moment for server to initialize
    Start-Sleep -Seconds 2
    
    # Send test messages
    foreach ($test in $messages) {
        Write-Host "Test: $($test.name)" -ForegroundColor Yellow
        $json = $test.message | ConvertTo-Json -Depth 10 -Compress
        Write-Host "Sending: $json" -ForegroundColor Gray
        
        $process.StandardInput.WriteLine($json)
        $process.StandardInput.Flush()
        
        # Wait for response
        Start-Sleep -Seconds 1
        
        Write-Host ""
    }
    
    # Wait a bit more for responses
    Start-Sleep -Seconds 2
    
    # Display output
    Write-Host "=== Server Output ===" -ForegroundColor Cyan
    $output = $outputBuilder.ToString()
    if ($output) {
        Write-Host $output -ForegroundColor White
    } else {
        Write-Host "(No output)" -ForegroundColor Gray
    }
    Write-Host ""
    
    Write-Host "=== Server Errors ===" -ForegroundColor Cyan
    $errors = $errorBuilder.ToString()
    if ($errors) {
        Write-Host $errors -ForegroundColor Yellow
    } else {
        Write-Host "(No errors)" -ForegroundColor Gray
    }
    Write-Host ""
    
    Write-Host "✓ MCP server test completed" -ForegroundColor Green
    Write-Host ""
    Write-Host "Note: For interactive testing, run:" -ForegroundColor Yellow
    Write-Host "  dotnet run --project VikunjaHook/VikunjaHook.csproj -- --mcp" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Then send JSON-RPC messages via stdin" -ForegroundColor Gray
    
} finally {
    # Cleanup
    if (-not $process.HasExited) {
        Write-Host "Stopping server..." -ForegroundColor Yellow
        $process.Kill()
        $process.WaitForExit(5000)
    }
    $process.Dispose()
}

