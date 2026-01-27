# Vikunja Integration Test Script
# This script:
# 1. Starts docker-compose services
# 2. Waits for Vikunja to be ready
# 3. Registers a test user
# 4. Gets API token
# 5. Runs MCP tools tests
# 6. Runs API tests
# 7. Cleans up

param(
    [switch]$SkipCleanup,
    [switch]$KeepServices
)

$ErrorActionPreference = "Stop"

Write-Host "=== Vikunja Integration Test ===" -ForegroundColor Cyan
Write-Host ""

# Function to check if a service is ready
function Wait-ForService {
    param(
        [string]$Url,
        [string]$ServiceName,
        [int]$MaxAttempts = 60,
        [int]$DelaySeconds = 2
    )
    
    Write-Host "Waiting for $ServiceName to be ready..." -ForegroundColor Yellow
    $attempt = 0
    
    while ($attempt -lt $MaxAttempts) {
        try {
            $response = Invoke-WebRequest -Uri $Url -Method GET -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Host "✓ $ServiceName is ready!" -ForegroundColor Green
                return $true
            }
        }
        catch {
            # Service not ready yet
        }
        
        $attempt++
        Write-Host "  Attempt $attempt/$MaxAttempts..." -ForegroundColor Gray
        Start-Sleep -Seconds $DelaySeconds
    }
    
    Write-Host "✗ $ServiceName failed to start" -ForegroundColor Red
    return $false
}

# Step 1: Start docker-compose services
Write-Host "Step 1: Starting docker-compose services..." -ForegroundColor Cyan
try {
    docker-compose down -v 2>$null
    docker-compose up -d db cache minio vikunja
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start docker-compose services"
    }
    
    Write-Host "✓ Services started" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to start services: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Wait for Vikunja to be ready
Write-Host "Step 2: Waiting for Vikunja..." -ForegroundColor Cyan
if (-not (Wait-ForService -Url "http://localhost:8080/health" -ServiceName "Vikunja" -MaxAttempts 60)) {
    Write-Host "✗ Vikunja failed to start. Checking logs..." -ForegroundColor Red
    docker-compose logs vikunja
    docker-compose down -v
    exit 1
}

Write-Host ""

# Step 3: Register test user
Write-Host "Step 3: Registering test user..." -ForegroundColor Cyan
$username = "testuser_$(Get-Date -Format 'yyyyMMddHHmmss')"
$email = "test_$(Get-Date -Format 'yyyyMMddHHmmss')@example.com"
$password = "TestPassword123!"

try {
    $registerBody = @{
        username = $username
        email = $email
        password = $password
    } | ConvertTo-Json

    $registerResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $registerBody `
        -ErrorAction Stop

    Write-Host "✓ User registered: $username" -ForegroundColor Green
    
    # Login to get token
    $loginBody = @{
        username = $username
        password = $password
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    if (-not $loginResponse.token) {
        throw "No token received from login"
    }

    $apiToken = $loginResponse.token
    Write-Host "✓ API Token obtained" -ForegroundColor Green
}
catch {
    Write-Host "✗ Failed to register/login user: $_" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
    docker-compose logs vikunja
    if (-not $KeepServices) {
        docker-compose down -v
    }
    exit 1
}

Write-Host ""

# Step 4: Run MCP Tools Tests
Write-Host "Step 4: Running MCP Tools Tests..." -ForegroundColor Cyan
$env:VIKUNJA_API_URL = "http://localhost:8080/api/v1"
$env:VIKUNJA_API_TOKEN = $apiToken

try {
    Push-Location src/VikunjaHook
    $testResult = pwsh -File test-mcp-tools.ps1
    $testExitCode = $LASTEXITCODE
    Pop-Location

    if ($testExitCode -ne 0) {
        throw "MCP Tools tests failed with exit code $testExitCode"
    }

    Write-Host "✓ MCP Tools tests passed" -ForegroundColor Green
}
catch {
    Write-Host "✗ MCP Tools tests failed: $_" -ForegroundColor Red
    if (-not $KeepServices) {
        docker-compose down -v
    }
    exit 1
}

Write-Host ""

# Step 5: Build and start VikunjaHook
Write-Host "Step 5: Building and starting VikunjaHook..." -ForegroundColor Cyan
try {
    # Build the project
    Push-Location src/VikunjaHook
    dotnet build -c Release --no-restore 2>&1 | Out-Null
    Pop-Location

    # Start VikunjaHook in background
    $vikunjaHookProcess = Start-Process -FilePath "dotnet" `
        -ArgumentList "run --project src/VikunjaHook/VikunjaHook/VikunjaHook.csproj -c Release --no-build --urls http://localhost:5082" `
        -PassThru `
        -NoNewWindow `
        -RedirectStandardOutput "vikunja-hook-output.log" `
        -RedirectStandardError "vikunja-hook-error.log"

    Write-Host "✓ VikunjaHook started (PID: $($vikunjaHookProcess.Id))" -ForegroundColor Green

    # Wait for VikunjaHook to be ready
    if (-not (Wait-ForService -Url "http://localhost:5082/health" -ServiceName "VikunjaHook" -MaxAttempts 30)) {
        Write-Host "✗ VikunjaHook failed to start" -ForegroundColor Red
        Get-Content "vikunja-hook-error.log" -ErrorAction SilentlyContinue
        Stop-Process -Id $vikunjaHookProcess.Id -Force -ErrorAction SilentlyContinue
        if (-not $KeepServices) {
            docker-compose down -v
        }
        exit 1
    }
}
catch {
    Write-Host "✗ Failed to start VikunjaHook: $_" -ForegroundColor Red
    if (-not $KeepServices) {
        docker-compose down -v
    }
    exit 1
}

Write-Host ""

# Step 6: Run API Tests
Write-Host "Step 6: Running API Tests..." -ForegroundColor Cyan
try {
    $apiTestResult = pwsh -File test-api.ps1
    $apiTestExitCode = $LASTEXITCODE

    if ($apiTestExitCode -ne 0) {
        throw "API tests failed with exit code $apiTestExitCode"
    }

    Write-Host "✓ API tests passed" -ForegroundColor Green
}
catch {
    Write-Host "✗ API tests failed: $_" -ForegroundColor Red
    Stop-Process -Id $vikunjaHookProcess.Id -Force -ErrorAction SilentlyContinue
    if (-not $KeepServices) {
        docker-compose down -v
    }
    exit 1
}

Write-Host ""

# Step 7: Cleanup
Write-Host "Step 7: Cleanup..." -ForegroundColor Cyan

# Stop VikunjaHook
if ($vikunjaHookProcess -and -not $vikunjaHookProcess.HasExited) {
    Stop-Process -Id $vikunjaHookProcess.Id -Force -ErrorAction SilentlyContinue
    Write-Host "✓ VikunjaHook stopped" -ForegroundColor Green
}

# Clean up log files
Remove-Item "vikunja-hook-output.log" -ErrorAction SilentlyContinue
Remove-Item "vikunja-hook-error.log" -ErrorAction SilentlyContinue

# Stop docker-compose services
if (-not $KeepServices) {
    docker-compose down -v
    Write-Host "✓ Docker services stopped" -ForegroundColor Green
}
else {
    Write-Host "⚠ Docker services kept running (use -KeepServices flag)" -ForegroundColor Yellow
    Write-Host "  API Token: $apiToken" -ForegroundColor Yellow
    Write-Host "  Username: $username" -ForegroundColor Yellow
    Write-Host "  Password: $password" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Integration Test Complete ===" -ForegroundColor Green
Write-Host "✓ All tests passed!" -ForegroundColor Green
Write-Host ""

exit 0
