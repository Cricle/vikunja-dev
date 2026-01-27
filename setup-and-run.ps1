#!/usr/bin/env pwsh
# Setup and Run Script for Vikunja Webhook Notification System

param(
    [switch]$SkipBuild,
    [switch]$DevMode,
    [string]$VikunjaUrl,
    [string]$VikunjaToken
)

Write-Host "🔔 Vikunja Webhook Notification System - Setup & Run" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK found: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 10 SDK." -ForegroundColor Red
    exit 1
}

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js not found. Please install Node.js 18+." -ForegroundColor Red
    exit 1
}

# Check npm
try {
    $npmVersion = npm --version
    Write-Host "✅ npm found: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ npm not found. Please install npm." -ForegroundColor Red
    exit 1
}

Write-Host ""

# Set environment variables
if ($VikunjaUrl) {
    $env:VIKUNJA_API_URL = $VikunjaUrl
    Write-Host "✅ VIKUNJA_API_URL set to: $VikunjaUrl" -ForegroundColor Green
}

if ($VikunjaToken) {
    $env:VIKUNJA_API_TOKEN = $VikunjaToken
    Write-Host "✅ VIKUNJA_API_TOKEN set" -ForegroundColor Green
}

# Check if environment variables are set
if (-not $env:VIKUNJA_API_URL) {
    Write-Host "⚠️  VIKUNJA_API_URL not set. Please set it:" -ForegroundColor Yellow
    Write-Host '   $env:VIKUNJA_API_URL="https://your-vikunja.com/api/v1"' -ForegroundColor Gray
    Write-Host "   Or use: .\setup-and-run.ps1 -VikunjaUrl 'https://your-vikunja.com/api/v1'" -ForegroundColor Gray
    exit 1
}

if (-not $env:VIKUNJA_API_TOKEN) {
    Write-Host "⚠️  VIKUNJA_API_TOKEN not set. Please set it:" -ForegroundColor Yellow
    Write-Host '   $env:VIKUNJA_API_TOKEN="your_token_here"' -ForegroundColor Gray
    Write-Host "   Or use: .\setup-and-run.ps1 -VikunjaToken 'your_token_here'" -ForegroundColor Gray
    exit 1
}

Write-Host ""

# Build frontend
if (-not $SkipBuild) {
    Write-Host "Building frontend..." -ForegroundColor Yellow
    
    $wwwrootPath = "src/VikunjaHook/VikunjaHook/wwwroot"
    
    if (-not (Test-Path "$wwwrootPath/node_modules")) {
        Write-Host "Installing npm dependencies..." -ForegroundColor Yellow
        Push-Location $wwwrootPath
        npm install
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ npm install failed" -ForegroundColor Red
            Pop-Location
            exit 1
        }
        Pop-Location
        Write-Host "✅ npm dependencies installed" -ForegroundColor Green
    } else {
        Write-Host "✅ npm dependencies already installed" -ForegroundColor Green
    }
    
    Write-Host "Building frontend assets..." -ForegroundColor Yellow
    Push-Location $wwwrootPath
    
    if ($DevMode) {
        Write-Host "Starting frontend in development mode..." -ForegroundColor Yellow
        Start-Process -FilePath "npm" -ArgumentList "run", "dev" -NoNewWindow
        Write-Host "✅ Frontend dev server starting on http://localhost:5173" -ForegroundColor Green
    } else {
        npm run build
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Frontend build failed" -ForegroundColor Red
            Pop-Location
            exit 1
        }
        Write-Host "✅ Frontend built successfully" -ForegroundColor Green
    }
    
    Pop-Location
} else {
    Write-Host "⏭️  Skipping frontend build" -ForegroundColor Gray
}

Write-Host ""

# Create data directory if it doesn't exist
$dataDir = "data/configs"
if (-not (Test-Path $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
    Write-Host "✅ Created data directory: $dataDir" -ForegroundColor Green
}

Write-Host ""
Write-Host "Starting backend..." -ForegroundColor Yellow

# Run backend
$projectPath = "src/VikunjaHook/VikunjaHook/VikunjaHook.csproj"

if ($DevMode) {
    Write-Host "Running in development mode with hot reload..." -ForegroundColor Yellow
    dotnet watch run --project $projectPath
} else {
    dotnet run --project $projectPath
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Backend failed to start" -ForegroundColor Red
    exit 1
}
