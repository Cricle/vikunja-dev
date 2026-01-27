#!/usr/bin/env pwsh
# Demo Setup Script - Shows how to run the system

Write-Host "🔔 Vikunja Webhook Notification System - Demo Setup" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📋 步骤 1: 设置环境变量" -ForegroundColor Yellow
Write-Host ""
Write-Host "你需要设置以下环境变量：" -ForegroundColor White
Write-Host ""
Write-Host '  $env:VIKUNJA_API_URL = "https://your-vikunja.com/api/v1"' -ForegroundColor Green
Write-Host '  $env:VIKUNJA_API_TOKEN = "your_token_here"' -ForegroundColor Green
Write-Host ""
Write-Host "如何获取 Vikunja API Token：" -ForegroundColor White
Write-Host "  1. 登录你的 Vikunja 实例" -ForegroundColor Gray
Write-Host "  2. 进入 Settings → API Tokens" -ForegroundColor Gray
Write-Host "  3. 创建新的 token" -ForegroundColor Gray
Write-Host "  4. 复制 token" -ForegroundColor Gray
Write-Host ""

Write-Host "📋 步骤 2: 安装前端依赖" -ForegroundColor Yellow
Write-Host ""
Write-Host "  cd src/VikunjaHook/VikunjaHook/wwwroot" -ForegroundColor Green
Write-Host "  npm install" -ForegroundColor Green
Write-Host ""

Write-Host "📋 步骤 3: 构建前端" -ForegroundColor Yellow
Write-Host ""
Write-Host "  npm run build" -ForegroundColor Green
Write-Host ""

Write-Host "📋 步骤 4: 运行后端" -ForegroundColor Yellow
Write-Host ""
Write-Host "  cd ../../../.." -ForegroundColor Green
Write-Host "  dotnet run --project src/VikunjaHook/VikunjaHook/VikunjaHook.csproj" -ForegroundColor Green
Write-Host ""

Write-Host "📋 步骤 5: 访问 Web 界面" -ForegroundColor Yellow
Write-Host ""
Write-Host "  打开浏览器访问: http://localhost:5000" -ForegroundColor Green
Write-Host ""

Write-Host "🚀 或者使用自动化脚本（推荐）：" -ForegroundColor Cyan
Write-Host ""
Write-Host "  .\setup-and-run.ps1 -VikunjaUrl 'https://your-vikunja.com/api/v1' -VikunjaToken 'your_token'" -ForegroundColor Green
Write-Host ""

Write-Host "📚 更多信息请查看：" -ForegroundColor Yellow
Write-Host "  - QUICK_START.md - 快速开始指南" -ForegroundColor Gray
Write-Host "  - SETUP_CHECKLIST.md - 设置检查清单" -ForegroundColor Gray
Write-Host "  - WEBHOOK_NOTIFICATION_SYSTEM.md - 完整文档" -ForegroundColor Gray
Write-Host ""

# Check if user wants to proceed
Write-Host "是否要继续设置？(需要 Vikunja API URL 和 Token)" -ForegroundColor Yellow
Write-Host "按 Enter 继续，或 Ctrl+C 退出..." -ForegroundColor Gray
Read-Host

# Ask for Vikunja URL
Write-Host ""
Write-Host "请输入 Vikunja API URL (例如: https://vikunja.example.com/api/v1):" -ForegroundColor Yellow
$vikunjaUrl = Read-Host "URL"

if ([string]::IsNullOrWhiteSpace($vikunjaUrl)) {
    Write-Host "❌ URL 不能为空" -ForegroundColor Red
    exit 1
}

# Ask for Vikunja Token
Write-Host ""
Write-Host "请输入 Vikunja API Token:" -ForegroundColor Yellow
$vikunjaToken = Read-Host "Token" -AsSecureString
$vikunjaTokenPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($vikunjaToken))

if ([string]::IsNullOrWhiteSpace($vikunjaTokenPlain)) {
    Write-Host "❌ Token 不能为空" -ForegroundColor Red
    exit 1
}

# Set environment variables
$env:VIKUNJA_API_URL = $vikunjaUrl
$env:VIKUNJA_API_TOKEN = $vikunjaTokenPlain

Write-Host ""
Write-Host "✅ 环境变量已设置" -ForegroundColor Green
Write-Host ""

# Run the setup script
Write-Host "🚀 启动自动化设置脚本..." -ForegroundColor Cyan
Write-Host ""

& ".\setup-and-run.ps1"
