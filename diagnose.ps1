#!/usr/bin/env pwsh
# 诊断脚本 - 检查常见问题

Write-Host "🔍 Vikunja Webhook 系统诊断" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# 检查 .NET SDK
Write-Host "1. 检查 .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host "   ✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ .NET SDK 未安装" -ForegroundColor Red
}
Write-Host ""

# 检查 Node.js
Write-Host "2. 检查 Node.js..." -ForegroundColor Yellow
try {
    $nodeVersion = node --version
    Write-Host "   ✅ Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ Node.js 未安装" -ForegroundColor Red
}
Write-Host ""

# 检查 npm
Write-Host "3. 检查 npm..." -ForegroundColor Yellow
try {
    $npmVersion = npm --version
    Write-Host "   ✅ npm: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "   ❌ npm 未安装" -ForegroundColor Red
}
Write-Host ""

# 检查环境变量
Write-Host "4. 检查环境变量..." -ForegroundColor Yellow
if ($env:VIKUNJA_API_URL) {
    Write-Host "   ✅ VIKUNJA_API_URL: $env:VIKUNJA_API_URL" -ForegroundColor Green
} else {
    Write-Host "   ❌ VIKUNJA_API_URL 未设置" -ForegroundColor Red
    Write-Host "      设置方法: `$env:VIKUNJA_API_URL = 'https://your-vikunja.com/api/v1'" -ForegroundColor Gray
}

if ($env:VIKUNJA_API_TOKEN) {
    Write-Host "   ✅ VIKUNJA_API_TOKEN: 已设置" -ForegroundColor Green
} else {
    Write-Host "   ❌ VIKUNJA_API_TOKEN 未设置" -ForegroundColor Red
    Write-Host "      设置方法: `$env:VIKUNJA_API_TOKEN = 'your_token'" -ForegroundColor Gray
}
Write-Host ""

# 检查项目编译
Write-Host "5. 检查项目编译..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build src/VikunjaHook/VikunjaHook/VikunjaHook.csproj --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ 项目编译成功" -ForegroundColor Green
    } else {
        Write-Host "   ❌ 项目编译失败" -ForegroundColor Red
        Write-Host "   错误信息:" -ForegroundColor Red
        $buildResult | Select-String -Pattern "error" | ForEach-Object { Write-Host "      $_" -ForegroundColor Red }
    }
} catch {
    Write-Host "   ❌ 编译检查失败: $_" -ForegroundColor Red
}
Write-Host ""

# 检查前端依赖
Write-Host "6. 检查前端依赖..." -ForegroundColor Yellow
if (Test-Path "src/VikunjaHook/VikunjaHook/wwwroot/node_modules") {
    Write-Host "   ✅ 前端依赖已安装" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  前端依赖未安装" -ForegroundColor Yellow
    Write-Host "      安装方法:" -ForegroundColor Gray
    Write-Host "        cd src/VikunjaHook/VikunjaHook/wwwroot" -ForegroundColor Gray
    Write-Host "        npm install" -ForegroundColor Gray
}
Write-Host ""

# 检查前端构建
Write-Host "7. 检查前端构建..." -ForegroundColor Yellow
if (Test-Path "src/VikunjaHook/VikunjaHook/wwwroot/dist") {
    Write-Host "   ✅ 前端已构建" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  前端未构建" -ForegroundColor Yellow
    Write-Host "      构建方法:" -ForegroundColor Gray
    Write-Host "        cd src/VikunjaHook/VikunjaHook/wwwroot" -ForegroundColor Gray
    Write-Host "        npm run build" -ForegroundColor Gray
}
Write-Host ""

# 检查数据目录
Write-Host "8. 检查数据目录..." -ForegroundColor Yellow
if (Test-Path "data/configs") {
    Write-Host "   ✅ 数据目录存在" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  数据目录不存在（首次运行时会自动创建）" -ForegroundColor Yellow
}
Write-Host ""

# 检查端口占用
Write-Host "9. 检查端口 5000..." -ForegroundColor Yellow
try {
    $port5000 = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
    if ($port5000) {
        Write-Host "   ⚠️  端口 5000 已被占用" -ForegroundColor Yellow
        Write-Host "      进程: $($port5000.OwningProcess)" -ForegroundColor Gray
    } else {
        Write-Host "   ✅ 端口 5000 可用" -ForegroundColor Green
    }
} catch {
    Write-Host "   ℹ️  无法检查端口状态" -ForegroundColor Gray
}
Write-Host ""

# 总结
Write-Host "=============================" -ForegroundColor Cyan
Write-Host "📋 诊断完成" -ForegroundColor Cyan
Write-Host ""
Write-Host "如果所有检查都通过，你可以运行：" -ForegroundColor White
Write-Host "  .\setup-and-run.ps1 -VikunjaUrl 'URL' -VikunjaToken 'TOKEN'" -ForegroundColor Green
Write-Host ""
Write-Host "或查看详细文档：" -ForegroundColor White
Write-Host "  如何运行.md" -ForegroundColor Green
Write-Host ""
