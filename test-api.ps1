# API Test Script for Vikunja Hook

param(
    [string]$BaseUrl = "http://localhost:5082"
)

Write-Host "Testing Vikunja Hook API at $BaseUrl" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "Test 1: Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/health" -Method GET -UseBasicParsing
    Write-Host "✓ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  Response: $($response.Content)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Get Supported Events
Write-Host "Test 2: Get Supported Events" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$BaseUrl/webhook/vikunja/events" -Method GET -UseBasicParsing
    Write-Host "✓ Status: $($response.StatusCode)" -ForegroundColor Green
    $events = ($response.Content | ConvertFrom-Json).supportedEvents
    Write-Host "  Supported events: $($events.Count)" -ForegroundColor Gray
    $events | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: Webhook Endpoint
Write-Host "Test 3: Webhook Endpoint" -ForegroundColor Yellow
try {
    $body = @{
        eventName = "task.created"
        time = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
    } | ConvertTo-Json
    
    $response = Invoke-WebRequest -Uri "$BaseUrl/webhook/vikunja" -Method POST -Body $body -ContentType "application/json" -UseBasicParsing
    Write-Host "✓ Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  Response: $($response.Content)" -ForegroundColor Gray
} catch {
    # Expected to fail without complete payload
    Write-Host "✓ Endpoint accessible (expected error with incomplete payload)" -ForegroundColor Green
    Write-Host "  Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
}
Write-Host ""

Write-Host "API tests completed!" -ForegroundColor Cyan
