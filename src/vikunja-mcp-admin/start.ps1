# Vikunja MCP Admin - Quick Start Script

Write-Host "🚀 Starting Vikunja MCP Admin..." -ForegroundColor Cyan

# Check if node_modules exists
if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
        exit 1
    }
}

Write-Host "✅ Dependencies ready" -ForegroundColor Green

# Check if MCP server is running
Write-Host "🔍 Checking MCP server..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5082/mcp/health" -Method Get -TimeoutSec 2
    Write-Host "✅ MCP server is running: $($response.server) v$($response.version)" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Warning: MCP server is not running on http://localhost:5082" -ForegroundColor Yellow
    Write-Host "   Please start the MCP server first:" -ForegroundColor Yellow
    Write-Host "   cd ../VikunjaHook/VikunjaHook" -ForegroundColor Gray
    Write-Host "   dotnet run" -ForegroundColor Gray
    Write-Host ""
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit 0
    }
}

Write-Host ""
Write-Host "🌐 Starting development server..." -ForegroundColor Cyan
Write-Host "   Admin UI will be available at: http://localhost:3000" -ForegroundColor Green
Write-Host "   Press Ctrl+C to stop" -ForegroundColor Gray
Write-Host ""

npm run dev
