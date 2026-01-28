#!/usr/bin/env pwsh
# Frontend Integration Test Script

$ErrorActionPreference = "Stop"

Write-Host "=== Frontend Integration Test ===" -ForegroundColor Cyan

# Configuration
$SERVER_URL = "http://localhost:5082"
$MCP_ENDPOINT = "$SERVER_URL/mcp"
$HEALTH_ENDPOINT = "$SERVER_URL/health"

# Test counter
$testsPassed = 0
$testsFailed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    
    Write-Host "`nTesting: $Name" -ForegroundColor Yellow
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            UseBasicParsing = $true
        }
        
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-WebRequest @params
        
        if ($response.StatusCode -eq 200) {
            Write-Host "✓ PASSED: $Name (Status: $($response.StatusCode))" -ForegroundColor Green
            $script:testsPassed++
            return $response
        } else {
            Write-Host "✗ FAILED: $Name (Status: $($response.StatusCode))" -ForegroundColor Red
            $script:testsFailed++
            return $null
        }
    } catch {
        Write-Host "✗ FAILED: $Name - $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
        return $null
    }
}

# Wait for server to be ready
Write-Host "`nWaiting for server to be ready..." -ForegroundColor Cyan
$maxAttempts = 30
$attempt = 0

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri $HEALTH_ENDPOINT -UseBasicParsing -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-Host "Server is ready!" -ForegroundColor Green
            break
        }
    } catch {
        # Server not ready yet
    }
    
    $attempt++
    if ($attempt -ge $maxAttempts) {
        Write-Host "Server failed to start within timeout" -ForegroundColor Red
        exit 1
    }
    
    Start-Sleep -Seconds 1
}

# Test 1: Health Check
Test-Endpoint -Name "Health Check" -Url $HEALTH_ENDPOINT

# Test 2: Frontend Assets
Test-Endpoint -Name "Frontend Index" -Url "$SERVER_URL/"
# Note: Asset filenames have hashes, so we just verify the index loads correctly

# Test 3: MCP Tools List
$mcpHeaders = @{
    'Accept' = 'application/json, text/event-stream'
}

$toolsListBody = @{
    jsonrpc = '2.0'
    id = 1
    method = 'tools/list'
    params = @{}
} | ConvertTo-Json

$response = Test-Endpoint -Name "MCP Tools List" -Url $MCP_ENDPOINT -Method POST -Headers $mcpHeaders -Body $toolsListBody

if ($response) {
    Write-Host "  Response preview: $($response.Content.Substring(0, [Math]::Min(200, $response.Content.Length)))..." -ForegroundColor Gray
}

# Test 4: MCP Get Current User
$getCurrentUserBody = @{
    jsonrpc = '2.0'
    id = 2
    method = 'tools/call'
    params = @{
        name = 'get_current_user'
        arguments = @{}
    }
} | ConvertTo-Json -Depth 10

$response = Test-Endpoint -Name "MCP Get Current User" -Url $MCP_ENDPOINT -Method POST -Headers $mcpHeaders -Body $getCurrentUserBody

# Test 5: MCP List Projects
$listProjectsBody = @{
    jsonrpc = '2.0'
    id = 3
    method = 'tools/call'
    params = @{
        name = 'list_projects'
        arguments = @{}
    }
} | ConvertTo-Json -Depth 10

$response = Test-Endpoint -Name "MCP List Projects" -Url $MCP_ENDPOINT -Method POST -Headers $mcpHeaders -Body $listProjectsBody

# Test 6: MCP List Labels
$listLabelsBody = @{
    jsonrpc = '2.0'
    id = 4
    method = 'tools/call'
    params = @{
        name = 'list_labels'
        arguments = @{}
    }
} | ConvertTo-Json -Depth 10

$response = Test-Endpoint -Name "MCP List Labels" -Url $MCP_ENDPOINT -Method POST -Headers $mcpHeaders -Body $listLabelsBody

# Test 7: API Routes (should not be intercepted by SPA fallback)
Test-Endpoint -Name "API Health Endpoint" -Url "$SERVER_URL/health"
Test-Endpoint -Name "MCP Endpoint Accessibility" -Url $MCP_ENDPOINT -Method POST -Headers $mcpHeaders -Body $toolsListBody

# Summary
Write-Host "`n=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Tests Passed: $testsPassed" -ForegroundColor Green
Write-Host "Tests Failed: $testsFailed" -ForegroundColor $(if ($testsFailed -gt 0) { "Red" } else { "Green" })

if ($testsFailed -gt 0) {
    Write-Host "`nIntegration tests FAILED" -ForegroundColor Red
    exit 1
} else {
    Write-Host "`nAll integration tests PASSED" -ForegroundColor Green
    exit 0
}
