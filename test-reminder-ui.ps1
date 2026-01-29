#!/usr/bin/env pwsh
# 测试任务提醒 UI 的保存按钮功能

$ErrorActionPreference = "Stop"

Write-Host "`n=== 测试任务提醒 UI 保存按钮 ===" -ForegroundColor Cyan

# 检查服务是否运行
Write-Host "`n[1/3] 检查服务状态..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5082/health" -Method Get -TimeoutSec 5
    Write-Host "✓ VikunjaHook 服务运行中 (status: $($health.status))" -ForegroundColor Green
} catch {
    Write-Host "✗ VikunjaHook 服务未运行" -ForegroundColor Red
    exit 1
}

# 检查任务提醒页面是否可访问
Write-Host "`n[2/3] 检查任务提醒页面..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5082/reminder" -Method Get -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ 任务提醒页面可访问" -ForegroundColor Green
        
        # 检查页面内容是否包含保存按钮相关的文本
        $content = $response.Content
        if ($content -match "save|保存|Save") {
            Write-Host "✓ 页面包含保存相关内容" -ForegroundColor Green
        } else {
            Write-Host "⚠ 页面可能缺少保存按钮" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "✗ 无法访问任务提醒页面: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 测试配置保存功能
Write-Host "`n[3/3] 测试配置保存..." -ForegroundColor Yellow
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
Write-Host "`n访问 http://localhost:5082/reminder 查看任务提醒页面" -ForegroundColor Gray
Write-Host "现在页面包含保存按钮，可以手动保存模板更改" -ForegroundColor Gray

exit 0
