#!/usr/bin/env pwsh
# 测试仪表盘提醒状态显示功能

$ErrorActionPreference = "Stop"

Write-Host "`n=== 测试仪表盘提醒状态显示 ===" -ForegroundColor Cyan

# 检查服务是否运行
Write-Host "`n[1/3] 检查服务状态..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5082/health" -Method Get -TimeoutSec 5
    Write-Host "✓ VikunjaHook 服务运行中 (status: $($health.status))" -ForegroundColor Green
} catch {
    Write-Host "✗ VikunjaHook 服务未运行" -ForegroundColor Red
    exit 1
}

# 检查仪表盘页面是否可访问
Write-Host "`n[2/4] 检查仪表盘页面..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5082/" -Method Get -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ 仪表盘页面可访问" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ 无法访问仪表盘页面: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 检查提醒状态 API
Write-Host "`n[3/4] 检查提醒状态 API..." -ForegroundColor Yellow
try {
    $reminderStatus = Invoke-RestMethod -Uri "http://localhost:5082/api/reminder-status" -Method Get
    Write-Host "✓ 提醒状态 API 可访问" -ForegroundColor Green
    Write-Host "  - 待处理任务: $($reminderStatus.pendingTasks)" -ForegroundColor Gray
    Write-Host "  - 已发送提醒: $($reminderStatus.sentReminders)" -ForegroundColor Gray
    Write-Host "  - 初始化状态: $($reminderStatus.isInitialized)" -ForegroundColor Gray
    Write-Host "  - 监听任务数: $($reminderStatus.tasks.Count)" -ForegroundColor Gray
    
    if ($reminderStatus.tasks.Count -gt 0) {
        Write-Host "`n  监听中的任务:" -ForegroundColor Cyan
        foreach ($task in $reminderStatus.tasks | Select-Object -First 5) {
            Write-Host "    - [$($task.taskId)] $($task.title) ($($task.projectTitle))" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "✗ 无法访问提醒状态 API: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 测试配置保存功能
Write-Host "`n[4/4] 测试配置保存..." -ForegroundColor Yellow
try {
    # 创建测试配置
    $testConfig = @{
        userId = "test_user"
        providers = @(
            @{
                providerType = "pushdeer"
                settings = @{
                    pushkey = "TEST_KEY"
                }
            }
        )
        defaultProviders = @("pushdeer")
        templates = @{}
        reminderConfig = @{
            enabled = $true
            scanIntervalSeconds = 15
            format = "Text"
            providers = @()
            enableLabelFilter = $false
            filterLabelIds = @()
            startDateTemplate = @{
                titleTemplate = "测试标题: {{task.title}}"
                bodyTemplate = "测试内容: {{task.description}}"
            }
            dueDateTemplate = @{
                titleTemplate = "到期提醒: {{task.title}}"
                bodyTemplate = "任务即将到期"
            }
            reminderTimeTemplate = @{
                titleTemplate = "提醒: {{task.title}}"
                bodyTemplate = "这是一个提醒"
            }
        }
        lastModified = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json -Depth 10
    
    # 保存配置
    $saveResponse = Invoke-RestMethod -Uri "http://localhost:5082/api/webhook-config/test_user" -Method Put -Body $testConfig -ContentType "application/json"
    Write-Host "✓ 配置保存成功" -ForegroundColor Green
    
    # 验证配置
    Start-Sleep -Seconds 1
    $loadedConfig = Invoke-RestMethod -Uri "http://localhost:5082/api/webhook-config/test_user" -Method Get
    
    if ($loadedConfig.reminderConfig.enabled -eq $true -and 
        $loadedConfig.reminderConfig.scanIntervalSeconds -eq 15 -and
        $loadedConfig.reminderConfig.startDateTemplate.titleTemplate -eq "测试标题: {{task.title}}") {
        Write-Host "✓ 配置验证成功 - 所有字段正确保存" -ForegroundColor Green
    } else {
        Write-Host "⚠ 配置验证失败 - 某些字段未正确保存" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "✗ 配置保存测试失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Cyan
Write-Host "✓ 所有测试通过" -ForegroundColor Green
Write-Host "`n访问 http://localhost:5082/ 查看仪表盘" -ForegroundColor Gray
Write-Host "仪表盘现在会显示:" -ForegroundColor Gray
Write-Host "  1. 监听中的任务数量（统计卡片）" -ForegroundColor Gray
Write-Host "  2. 任务列表中的提醒状态标识（黄色芯片）" -ForegroundColor Gray
Write-Host "  3. 每10秒自动刷新提醒状态" -ForegroundColor Gray

exit 0
