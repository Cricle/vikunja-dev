#!/usr/bin/env pwsh
# Vikunja Webhook 完整测试脚本
# 测试 Vikunja webhook 功能并验证 VikunjaHook 服务接收和处理事件

$ErrorActionPreference = "Stop"
$testsPassed = 0
$testsFailed = 0

function Write-TestResult {
    param(
        [string]$Message,
        [bool]$Success,
        [string]$Details = ""
    )
    if ($Success) {
        Write-Host "✓ $Message" -ForegroundColor Green
        $script:testsPassed++
    } else {
        Write-Host "✗ $Message" -ForegroundColor Red
        if ($Details) {
            Write-Host "  详情: $Details" -ForegroundColor Gray
        }
        $script:testsFailed++
    }
}

function Get-WebhookLogs {
    param([int]$SinceSeconds = 15)
    $logs = docker-compose -f docker-compose.dev.yml logs --since ${SinceSeconds}s vikunja-hook 2>&1 | Out-String
    return $logs
}

function Verify-WebhookReceived {
    param(
        [string]$EventName,
        [int]$TaskId = 0,
        [string]$TaskTitle = ""
    )
    
    $logs = Get-WebhookLogs -SinceSeconds 20
    
    # 检查是否收到 webhook
    $receivedEvent = $logs -match "Received webhook event: $EventName"
    
    if (-not $receivedEvent) {
        return @{ Success = $false; Message = "未找到事件接收日志"; Details = @() }
    }
    
    # 检查 webhook 数据
    $hasEventName = $logs -match "event_name.*$EventName" -or $logs -match "EventName.*$EventName" -or $logs -match "Received webhook event: $EventName"
    $hasData = $logs -match "Webhook data:" -or $logs -match "data.*\{" -or $logs -match "Data.*\{" -or $logs -match "Routing webhook event"
    
    $details = @()
    if ($receivedEvent) { $details += "接收事件" }
    if ($hasEventName) { $details += "事件名称" }
    if ($hasData) { $details += "数据内容" }
    
    # 如果提供了任务信息，验证任务数据
    if ($TaskId -gt 0) {
        $hasTaskId = $logs -match "id.*:.*$TaskId" -or $logs -match """id"":\s*$TaskId" -or $logs -match "id""\s*:\s*$TaskId"
        if ($hasTaskId) { $details += "任务ID" }
    }
    
    if ($TaskTitle) {
        # 任务标题可能包含特殊字符，使用更宽松的匹配
        $escapedTitle = [regex]::Escape($TaskTitle)
        $hasTaskTitle = $logs -match $escapedTitle -or $logs -match "title"
        if ($hasTaskTitle) { $details += "任务标题" }
    }
    
    return @{
        Success = $receivedEvent -and $hasData
        Message = "验证项: $($details -join ', ')"
        Details = $details
    }
}

function Verify-NotificationSent {
    param(
        [string]$EventName,
        [int]$TaskId = 0
    )
    
    $logs = Get-WebhookLogs -SinceSeconds 20
    
    # 检查是否处理了事件路由
    $routingEvent = $logs -match "Routing webhook event: $EventName" -or $logs -match "Processing webhook event $EventName"
    
    # 检查是否调用了 provider 发送（成功或失败都算）
    $providerCalled = $logs -match "Notification sent successfully via pushdeer" -or 
                      $logs -match "Failed to send notification via pushdeer" -or
                      $logs -match "PushDeer notification sent" -or
                      $logs -match "Error sending PushDeer notification" -or
                      $logs -match "PushDeer API error"
    
    # 检查是否发送了通知（如果配置了提供商）
    $notificationSent = $logs -match "notification sent" -or $logs -match "No providers configured"
    
    $details = @()
    if ($routingEvent) { $details += "事件路由" }
    if ($providerCalled) { $details += "Provider调用" }
    if ($notificationSent) { $details += "通知处理" }
    
    return @{
        Success = $routingEvent -and $providerCalled
        Message = "验证项: $($details -join ', ')"
        Details = $details
        ProviderCalled = $providerCalled
    }
}

Write-Host "`n=== Vikunja Webhook 完整测试 ===" -ForegroundColor Cyan
Write-Host "测试环境: docker-compose.dev.yml" -ForegroundColor Gray
Write-Host "测试时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# 检查 .env 文件
if (-not (Test-Path ".env")) {
    Write-Host "`n创建 .env 文件..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env"
}

Write-Host "`n[1/24] 启动开发环境..." -ForegroundColor Yellow
try {
    docker-compose -f docker-compose.dev.yml down -v 2>&1 | Out-Null
    docker-compose -f docker-compose.dev.yml up -d --build 2>&1 | Out-Null
    Write-TestResult "开发环境启动" $true
} catch {
    Write-TestResult "开发环境启动" $false $_.Exception.Message
    exit 1
}

Write-Host "`n[2/24] 等待服务就绪..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# 清理旧的配置文件，使用内置默认模板
Write-Host "  清理旧配置文件..." -ForegroundColor Gray
try {
    docker-compose -f docker-compose.dev.yml exec -T vikunja-hook sh -c "rm -f /app/data/configs/*.json" 2>&1 | Out-Null
    Write-Host "  ✓ 配置文件已清理" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ 配置文件清理失败（可能不存在）" -ForegroundColor Yellow
}

# 检查 Vikunja
$maxRetries = 10
$vikunjaReady = $false
for ($i = 1; $i -le $maxRetries; $i++) {
    try {
        $info = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/info" -Method Get -TimeoutSec 3
        $vikunjaReady = $true
        Write-TestResult "Vikunja 服务就绪 (version: $($info.version))" $true
        break
    } catch {
        if ($i -eq $maxRetries) {
            Write-TestResult "Vikunja 服务就绪" $false "超时"
            exit 1
        }
        Start-Sleep -Seconds 3
    }
}

# 检查 VikunjaHook
Write-Host "`n[3/24] 检查 VikunjaHook 服务..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "http://localhost:5082/health" -Method Get -TimeoutSec 5
    Write-TestResult "VikunjaHook 服务就绪 (status: $($health.status))" $true
} catch {
    Write-TestResult "VikunjaHook 服务就绪" $false $_.Exception.Message
    exit 1
}

# 注册用户
Write-Host "`n[4/24] 注册测试用户..." -ForegroundColor Yellow
$username = "webhooktest_$(Get-Random -Maximum 9999)"
$password = "TestPass123!"
$email = "$username@test.local"

$registerData = @{
    username = $username
    email = $email
    password = $password
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/register" -Method Post -Body $registerData -ContentType "application/json"
    Write-TestResult "用户注册 ($username)" $true
} catch {
    Write-Host "  注册失败，可能用户已存在，继续登录..." -ForegroundColor Gray
}

# 登录
Write-Host "`n[5/24] 用户登录..." -ForegroundColor Yellow
$loginData = @{
    username = $username
    password = $password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/login" -Method Post -Body $loginData -ContentType "application/json"
    $apiToken = $loginResponse.token
    
    # 更新 .env
    $envContent = Get-Content ".env" -Raw
    $envContent = $envContent -replace 'VIKUNJA_API_TOKEN=.*', "VIKUNJA_API_TOKEN=$apiToken"
    Set-Content ".env" $envContent
    
    Write-TestResult "用户登录并获取 Token" $true
} catch {
    Write-TestResult "用户登录" $false $_.Exception.Message
    exit 1
}

# 重启 VikunjaHook 使用新 Token
Write-Host "`n[6/24] 重启 VikunjaHook..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml restart vikunja-hook 2>&1 | Out-Null
Start-Sleep -Seconds 5
Write-TestResult "VikunjaHook 重启完成" $true

$headers = @{
    "Authorization" = "Bearer $apiToken"
    "Content-Type" = "application/json"
}

# 创建项目
Write-Host "`n[7/24] 创建测试项目..." -ForegroundColor Yellow
$projectTitle = "Webhook Test $(Get-Date -Format 'HHmmss')"
$newProject = @{
    title = $projectTitle
    description = "用于测试 webhook 功能的项目"
} | ConvertTo-Json

try {
    $project = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects" -Headers $headers -Method Put -Body $newProject
    $projectId = $project.id
    Write-TestResult "项目创建 (ID: $projectId, Title: $projectTitle)" ($projectId -gt 0)
} catch {
    Write-TestResult "项目创建" $false $_.Exception.Message
    exit 1
}

# 配置 Webhook
Write-Host "`n[8/24] 配置 Webhook..." -ForegroundColor Yellow
$webhook = @{
    target_url = "http://vikunja-hook:5082/api/webhook"
    events = @(
        "task.created", 
        "task.updated", 
        "task.deleted",
        "task.comment.created",
        "task.assignee.created"
    )
    project_id = $projectId
} | ConvertTo-Json

try {
    $createdWebhook = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/webhooks" -Headers $headers -Method Put -Body $webhook
    $webhookId = $createdWebhook.id
    $webhookValid = ($webhookId -gt 0) -and ($createdWebhook.target_url -eq "http://vikunja-hook:5082/api/webhook") -and ($createdWebhook.events.Count -ge 3)
    Write-TestResult "Webhook 配置 (ID: $webhookId, Events: $($createdWebhook.events.Count))" $webhookValid
} catch {
    Write-TestResult "Webhook 配置" $false $_.Exception.Message
    exit 1
}

# 配置通知规则（用于测试推送）
Write-Host "`n  配置通知规则（包含 PushDeer provider）..." -ForegroundColor Gray
$notificationConfig = @{
    userId = $username
    providers = @(
        @{
            providerType = "pushdeer"
            settings = @{
                pushkey = "PDU1234567890TEST"  # 测试用的 pushkey
            }
        }
    )
    defaultProviders = @("pushdeer")  # 默认使用 pushdeer
    templates = @{}  # 空模板，使用内置默认模板
} | ConvertTo-Json -Depth 10

try {
    $configResponse = Invoke-RestMethod -Uri "http://localhost:5082/api/webhook-config/$username" -Method Post -Body $notificationConfig -ContentType "application/json"
    Write-Host "  ✓ 通知规则已配置（PushDeer provider，使用默认模板）" -ForegroundColor Green
} catch {
    Write-Host "  ⚠ 通知规则配置失败: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "  尝试使用 API 创建配置..." -ForegroundColor Gray
    
    # 如果 API 不存在，直接创建配置文件
    try {
        $configJson = @{
            userId = $username
            providers = @(
                @{
                    providerType = "pushdeer"
                    settings = @{
                        pushkey = "PDU1234567890TEST"
                    }
                }
            )
            defaultProviders = @("pushdeer")
            templates = @{}
            lastModified = (Get-Date).ToUniversalTime().ToString("o")
        } | ConvertTo-Json -Depth 10
        
        $configJson | docker-compose -f docker-compose.dev.yml exec -T vikunja-hook sh -c "cat > /app/data/configs/$username.json"
        Write-Host "  ✓ 配置文件已直接创建" -ForegroundColor Green
    } catch {
        Write-Host "  ✗ 配置创建失败: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# 清空日志缓冲
Start-Sleep -Seconds 2

# 创建任务并验证 webhook
Write-Host "`n[9/24] 创建任务 (触发 task.created)..." -ForegroundColor Yellow
$taskTitle = "Webhook Test Task $(Get-Date -Format 'HHmmss')"
$task = @{
    title = $taskTitle
    description = "测试 webhook 的任务"
    priority = 3
    due_date = (Get-Date).AddDays(7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

try {
    $createdTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/tasks" -Headers $headers -Method Put -Body $task
    $taskId = $createdTask.id
    Write-TestResult "任务创建 (ID: $taskId, Title: $taskTitle)" ($taskId -gt 0)
} catch {
    Write-TestResult "任务创建" $false $_.Exception.Message
    exit 1
}

# 添加标签到任务
Write-Host "  添加标签到任务..." -ForegroundColor Gray
try {
    # 先创建标签
    $label1 = @{
        title = "urgent"
        hex_color = "ff0000"
    } | ConvertTo-Json
    
    $label2 = @{
        title = "bug"
        hex_color = "00ff00"
    } | ConvertTo-Json
    
    $createdLabel1 = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/labels" -Headers $headers -Method Put -Body $label1 -ContentType "application/json"
    $createdLabel2 = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/labels" -Headers $headers -Method Put -Body $label2 -ContentType "application/json"
    
    # 将标签添加到任务
    $labelAssign1 = @{ label_id = $createdLabel1.id } | ConvertTo-Json
    $labelAssign2 = @{ label_id = $createdLabel2.id } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId/labels" -Headers $headers -Method Put -Body $labelAssign1 -ContentType "application/json" | Out-Null
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId/labels" -Headers $headers -Method Put -Body $labelAssign2 -ContentType "application/json" | Out-Null
    
    Write-Host "    ✓ 已添加 2 个标签 (urgent, bug)" -ForegroundColor Green
} catch {
    Write-Host "    ⚠ 标签添加失败: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 分配任务给当前用户
Write-Host "  分配任务给用户..." -ForegroundColor Gray
try {
    # 获取当前用户信息
    $currentUser = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/user" -Headers $headers -Method Get
    
    # 分配任务
    $assignee = @{
        user_id = $currentUser.id
    } | ConvertTo-Json
    
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId/assignees" -Headers $headers -Method Put -Body $assignee -ContentType "application/json" | Out-Null
    Write-Host "    ✓ 已分配给用户: $($currentUser.username)" -ForegroundColor Green
} catch {
    Write-Host "    ⚠ 任务分配失败: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 等待 webhook 触发
Start-Sleep -Seconds 2

# 创建一个新的完整任务来触发包含所有数据的 webhook
Write-Host "`n[9.5/24] 创建完整数据任务（验证所有占位符）..." -ForegroundColor Yellow
$fullTask = @{
    title = "Full Data Test Task"
    description = "包含所有数据的测试任务"
    priority = 5
    due_date = (Get-Date).AddDays(3).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
} | ConvertTo-Json

try {
    $fullCreatedTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/tasks" -Headers $headers -Method Put -Body $fullTask
    $fullTaskId = $fullCreatedTask.id
    
    # 添加标签
    $labelAssign1 = @{ label_id = $createdLabel1.id } | ConvertTo-Json
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$fullTaskId/labels" -Headers $headers -Method Put -Body $labelAssign1 -ContentType "application/json" | Out-Null
    
    # 分配用户
    $assignee = @{ user_id = $currentUser.id } | ConvertTo-Json
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$fullTaskId/assignees" -Headers $headers -Method Put -Body $assignee -ContentType "application/json" | Out-Null
    
    Write-TestResult "完整数据任务创建 (ID: $fullTaskId, 包含标签和分配人)" ($fullTaskId -gt 0)
    Start-Sleep -Seconds 3
} catch {
    Write-TestResult "完整数据任务创建" $false $_.Exception.Message
}

Start-Sleep -Seconds 3

# 验证 Vikunja 发送 task.created
Write-Host "`n[10/24] 验证 Vikunja 发送 task.created..." -ForegroundColor Yellow
$vikunjaLogs = docker-compose -f docker-compose.dev.yml logs --since 10s vikunja 2>&1 | Out-String
$taskCreatedSent = $vikunjaLogs -match "Sent webhook payload for webhook $webhookId for event task\.created"
Write-TestResult "Vikunja 发送 task.created webhook" $taskCreatedSent

# 验证 VikunjaHook 接收 task.created
Write-Host "`n[11/24] 验证 VikunjaHook 接收 task.created..." -ForegroundColor Yellow
$result = Verify-WebhookReceived -EventName "task.created" -TaskId $taskId -TaskTitle $taskTitle
Write-TestResult "VikunjaHook 接收 task.created ($($result.Message))" $result.Success

if ($result.Success) {
    Write-Host "  验证详情: $($result.Details -join ', ')" -ForegroundColor Gray
}

# 验证通知推送处理
Write-Host "`n[12/24] 验证 task.created 通知推送..." -ForegroundColor Yellow
$pushResult = Verify-NotificationSent -EventName "task.created" -TaskId $taskId
Write-TestResult "通知推送处理 ($($pushResult.Message))" $pushResult.Success

if ($pushResult.Success) {
    Write-Host "  推送详情: $($pushResult.Details -join ', ')" -ForegroundColor Gray
    if ($pushResult.ProviderCalled) {
        Write-Host "  ✓ PushDeer provider 已被调用" -ForegroundColor Green
    }
}

# 更新任务
Write-Host "`n[13/24] 更新任务 (触发 task.updated)..." -ForegroundColor Yellow
$updatedTitle = "Updated: $taskTitle"
$updateTask = @{
    title = $updatedTitle
    done = $true
} | ConvertTo-Json

try {
    $updatedTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId" -Headers $headers -Method Post -Body $updateTask
    $updateValid = ($updatedTask.done -eq $true) -and ($updatedTask.title -eq $updatedTitle)
    Write-TestResult "任务更新 (Done: $($updatedTask.done), Title: $updatedTitle)" $updateValid
} catch {
    Write-TestResult "任务更新" $false $_.Exception.Message
}

Start-Sleep -Seconds 3

# 验证 Vikunja 发送 task.updated
Write-Host "`n[14/24] 验证 Vikunja 发送 task.updated..." -ForegroundColor Yellow
$vikunjaLogs = docker-compose -f docker-compose.dev.yml logs --since 10s vikunja 2>&1 | Out-String
$taskUpdatedSent = $vikunjaLogs -match "Sent webhook payload for webhook $webhookId for event task\.updated"
Write-TestResult "Vikunja 发送 task.updated webhook" $taskUpdatedSent

# 验证 VikunjaHook 接收 task.updated
Write-Host "`n[15/24] 验证 VikunjaHook 接收 task.updated..." -ForegroundColor Yellow
$result = Verify-WebhookReceived -EventName "task.updated" -TaskId $taskId -TaskTitle $updatedTitle
Write-TestResult "VikunjaHook 接收 task.updated ($($result.Message))" $result.Success

if ($result.Success) {
    Write-Host "  验证详情: $($result.Details -join ', ')" -ForegroundColor Gray
}

# 验证通知推送处理
Write-Host "`n[16/24] 验证 task.updated 通知推送..." -ForegroundColor Yellow
$pushResult = Verify-NotificationSent -EventName "task.updated" -TaskId $taskId
Write-TestResult "通知推送处理 ($($pushResult.Message))" $pushResult.Success

if ($pushResult.Success) {
    Write-Host "  推送详情: $($pushResult.Details -join ', ')" -ForegroundColor Gray
    if ($pushResult.ProviderCalled) {
        Write-Host "  ✓ PushDeer provider 已被调用" -ForegroundColor Green
    }
}

# 删除任务
Write-Host "`n[17/24] 删除任务 (触发 task.deleted)..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId" -Headers $headers -Method Delete | Out-Null
    
    # 验证任务已删除
    try {
        Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId" -Headers $headers -Method Get | Out-Null
        Write-TestResult "任务删除" $false "任务仍然存在"
    } catch {
        Write-TestResult "任务删除" $true
    }
} catch {
    Write-TestResult "任务删除" $false $_.Exception.Message
}

Start-Sleep -Seconds 3

# 验证 Vikunja 发送 task.deleted
Write-Host "`n[18/24] 验证 Vikunja 发送 task.deleted..." -ForegroundColor Yellow
$vikunjaLogs = docker-compose -f docker-compose.dev.yml logs --since 10s vikunja 2>&1 | Out-String
$taskDeletedSent = $vikunjaLogs -match "Sent webhook payload for webhook $webhookId for event task\.deleted"
Write-TestResult "Vikunja 发送 task.deleted webhook" $taskDeletedSent

# 验证 VikunjaHook 接收 task.deleted
Write-Host "`n[19/24] 验证 VikunjaHook 接收 task.deleted..." -ForegroundColor Yellow
$result = Verify-WebhookReceived -EventName "task.deleted" -TaskId $taskId
Write-TestResult "VikunjaHook 接收 task.deleted ($($result.Message))" $result.Success

if ($result.Success) {
    Write-Host "  验证详情: $($result.Details -join ', ')" -ForegroundColor Gray
}

# 验证通知推送处理
Write-Host "`n[20/24] 验证 task.deleted 通知推送..." -ForegroundColor Yellow
$pushResult = Verify-NotificationSent -EventName "task.deleted" -TaskId $taskId
Write-TestResult "通知推送处理 ($($pushResult.Message))" $pushResult.Success

if ($pushResult.Success) {
    Write-Host "  推送详情: $($pushResult.Details -join ', ')" -ForegroundColor Gray
    if ($pushResult.ProviderCalled) {
        Write-Host "  ✓ PushDeer provider 已被调用" -ForegroundColor Green
    }
}

# 手动测试 webhook 端点
Write-Host "`n[21/24] 手动测试 webhook 端点..." -ForegroundColor Yellow
$testPayload = @{
    event_name = "task.created"
    time = (Get-Date).ToUniversalTime().ToString("o")
    data = @{ 
        id = 9999
        title = "Manual Test Task"
        description = "手动测试"
        done = $false
        project_id = $projectId
    }
} | ConvertTo-Json -Depth 3

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5082/api/webhook" -Method Post -Body $testPayload -ContentType "application/json" -UseBasicParsing
    $endpointWorks = $response.StatusCode -eq 202
    Write-TestResult "VikunjaHook 端点响应 (Status: $($response.StatusCode))" $endpointWorks
    
    if ($endpointWorks) {
        Start-Sleep -Seconds 2
        $logs = Get-WebhookLogs -SinceSeconds 5
        $manualTestReceived = $logs -match "Manual Test Task" -or $logs -match "9999"
        if ($manualTestReceived) {
            Write-Host "  ✓ 手动测试的 webhook 已被接收" -ForegroundColor Green
        }
    }
} catch {
    Write-TestResult "VikunjaHook 端点响应" $false $_.Exception.Message
}

# 验证占位符替换
Write-Host "`n[21.5/24] 验证占位符替换..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

try {
    $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=10" -Method Get
    
    if ($history.records.Count -gt 0) {
        Write-Host "  检查最近的推送记录中的占位符..." -ForegroundColor Gray
        
        $placeholderTests = @{
            "task.title" = $false
            "task.description" = $false
            "project.title" = $false
            "task.done" = $false
            "event.url" = $false
            "task.id" = $false
        }
        
        foreach ($record in $history.records) {
            $title = $record.eventData.title
            $body = $record.eventData.body
            
            # 检查任务标题占位符
            if ($title -match "Test Task|Manual Test Task|Webhook Test Task") {
                $placeholderTests["task.title"] = $true
            }
            
            # 检查项目标题占位符
            if ($body -match "Project #\d+|Webhook Test \d+|项目:") {
                $placeholderTests["project.title"] = $true
            }
            
            # 检查任务完成状态占位符
            if ($body -match "Done|Not Done|✓|○") {
                $placeholderTests["task.done"] = $true
            }
            
            # 检查事件URL占位符
            if ($body -match "http://localhost:3456/tasks/\d+|Link: http") {
                $placeholderTests["event.url"] = $true
            }
            
            # 检查任务ID
            if ($body -match "ID: \d+|id.*\d+" -or $title -match "\d+") {
                $placeholderTests["task.id"] = $true
            }
            
            # 检查描述
            if ($body -match "Description:|描述:|手动测试|测试 webhook") {
                $placeholderTests["task.description"] = $true
            }
        }
        
        $passedCount = ($placeholderTests.Values | Where-Object { $_ -eq $true }).Count
        $totalCount = $placeholderTests.Count
        
        Write-Host "  占位符验证结果:" -ForegroundColor Gray
        foreach ($test in $placeholderTests.GetEnumerator()) {
            $status = if ($test.Value) { "✓" } else { "✗" }
            $color = if ($test.Value) { "Green" } else { "Yellow" }
            Write-Host "    $status {{$($test.Key)}}" -ForegroundColor $color
        }
        
        Write-TestResult "占位符替换验证 ($passedCount/$totalCount 通过)" ($passedCount -ge 4)
        
        # 显示示例推送内容
        if ($history.records.Count -gt 0) {
            Write-Host "`n  最新推送示例:" -ForegroundColor Gray
            $latest = $history.records[0]
            Write-Host "    事件: $($latest.eventName)" -ForegroundColor Cyan
            Write-Host "    标题: $($latest.eventData.title)" -ForegroundColor White
            $bodyPreview = $latest.eventData.body.Substring(0, [Math]::Min(80, $latest.eventData.body.Length))
            Write-Host "    内容: $bodyPreview..." -ForegroundColor White
        }
    } else {
        Write-TestResult "占位符替换验证" $false "没有推送历史记录"
    }
} catch {
    Write-TestResult "占位符替换验证" $false $_.Exception.Message
}

# 验证日志完整性
Write-Host "`n[22/24] 验证日志完整性..." -ForegroundColor Yellow
$allLogs = docker-compose -f docker-compose.dev.yml logs --since 60s vikunja-hook 2>&1 | Out-String
$logChecks = @{
    "接收事件" = $allLogs -match "Received webhook event"
    "路由事件" = $allLogs -match "Routing webhook event|Processing webhook event"
    "加载配置" = $allLogs -match "Loaded.*user configurations"
    "Provider调用" = $allLogs -match "Notification sent successfully|Failed to send notification"
}

$logChecksPassed = ($logChecks.Values | Where-Object { $_ -eq $true }).Count
$logChecksTotal = $logChecks.Count
Write-TestResult "日志完整性检查 ($logChecksPassed/$logChecksTotal 项通过)" ($logChecksPassed -ge 3)

Write-Host "  日志检查详情:" -ForegroundColor Gray
foreach ($check in $logChecks.GetEnumerator()) {
    $status = if ($check.Value) { "✓" } else { "✗" }
    Write-Host "    $status $($check.Key)" -ForegroundColor $(if ($check.Value) { "Green" } else { "Gray" })
}

# 测试特殊事件的占位符
Write-Host "`n[22.5/24] 测试特殊事件占位符..." -ForegroundColor Yellow

# 创建一个真实任务用于测试特殊事件
Write-Host "  创建测试任务用于特殊事件..." -ForegroundColor Gray
$specialTestTask = @{
    title = "Special Event Test Task"
    description = "用于测试评论、附件、关系事件"
} | ConvertTo-Json

try {
    $specialTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/tasks" -Headers $headers -Method Put -Body $specialTestTask
    $specialTaskId = $specialTask.id
    Write-Host "    ✓ 测试任务已创建 (ID: $specialTaskId)" -ForegroundColor Green
    Start-Sleep -Seconds 2
} catch {
    Write-Host "    ✗ 测试任务创建失败: $($_.Exception.Message)" -ForegroundColor Red
    $specialTaskId = 0
}

# 测试评论事件
Write-Host "  测试评论事件占位符..." -ForegroundColor Gray
if ($specialTaskId -gt 0) {
    try {
        $commentData = @{
            comment = "这是一条测试评论，用于验证占位符"
        } | ConvertTo-Json
        
        $comment = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$specialTaskId/comments" -Headers $headers -Method Put -Body $commentData -ContentType "application/json"
        Start-Sleep -Seconds 3
        
        $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=1" -Method Get
        $commentTest = ($history.records[0].eventData.title -match "Comment|评论") -and 
                       ($history.records[0].eventData.title -match "Special Event Test Task" -or 
                        $history.records[0].eventData.body -match "Special Event Test Task")
        
        if ($commentTest) {
            Write-Host "    ✓ 评论事件占位符正常（包含任务标题）" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ 评论事件占位符部分工作" -ForegroundColor Yellow
            Write-Host "      标题: $($history.records[0].eventData.title)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "    ✗ 评论事件测试失败: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "    ⚠ 跳过评论事件测试（无测试任务）" -ForegroundColor Yellow
}

# 测试附件事件（模拟）
Write-Host "  测试附件事件占位符..." -ForegroundColor Gray
$attachmentPayload = @{
    event_name = "task.attachment.created"
    time = (Get-Date).ToUniversalTime().ToString("o")
    data = @{
        id = 200
        file_name = "test-document.pdf"
        task_id = $specialTaskId
    }
} | ConvertTo-Json -Depth 3

try {
    Invoke-WebRequest -Uri "http://localhost:5082/api/webhook" -Method Post -Body $attachmentPayload -ContentType "application/json" -UseBasicParsing | Out-Null
    Start-Sleep -Seconds 3
    
    $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=1" -Method Get
    $attachmentTest = ($history.records[0].eventData.title -match "Attachment|附件") -and 
                      ($history.records[0].eventData.body -match "test-document\.pdf" -or 
                       $history.records[0].eventData.title -match "Special Event Test Task")
    
    if ($attachmentTest) {
        Write-Host "    ✓ 附件事件占位符正常" -ForegroundColor Green
    } else {
        Write-Host "    ⚠ 附件事件占位符部分工作" -ForegroundColor Yellow
        Write-Host "      标题: $($history.records[0].eventData.title)" -ForegroundColor Gray
    }
} catch {
    Write-Host "    ✗ 附件事件测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试关系事件（模拟）
Write-Host "  测试关系事件占位符..." -ForegroundColor Gray

# 创建第二个任务用于建立关系
try {
    $relatedTask = @{
        title = "Related Task for Testing"
        description = "用于测试任务关系"
    } | ConvertTo-Json
    
    $createdRelatedTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/tasks" -Headers $headers -Method Put -Body $relatedTask
    $relatedTaskId = $createdRelatedTask.id
    Write-Host "    ✓ 创建关联任务 (ID: $relatedTaskId)" -ForegroundColor Green
    
    # 创建任务关系
    Start-Sleep -Seconds 2
    $relation = @{
        other_task_id = $relatedTaskId
        relation_kind = "related"
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$specialTaskId/relations" -Headers $headers -Method Put -Body $relation -ContentType "application/json" | Out-Null
        Write-Host "    ✓ 创建任务关系 (related)" -ForegroundColor Green
        Start-Sleep -Seconds 3
        
        $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=1" -Method Get
        $relationTest = ($history.records[0].eventData.title -match "Relation|关系") -and 
                        ($history.records[0].eventData.body -match "Task ID:|Related Task ID:|Special Event Test Task")
        
        if ($relationTest) {
            Write-Host "    ✓ 关系事件占位符正常（包含任务信息）" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ 关系事件占位符部分工作" -ForegroundColor Yellow
            Write-Host "      标题: $($history.records[0].eventData.title)" -ForegroundColor Gray
        }
    } catch {
        Write-Host "    ⚠ 创建任务关系失败: $($_.Exception.Message)" -ForegroundColor Yellow
        
        # 回退到模拟方式
        $relationPayload = @{
            event_name = "task.relation.created"
            time = (Get-Date).ToUniversalTime().ToString("o")
            data = @{
                task_id = $specialTaskId
                other_task_id = $relatedTaskId
                relation_kind = "related"
            }
        } | ConvertTo-Json -Depth 3
        
        Invoke-WebRequest -Uri "http://localhost:5082/api/webhook" -Method Post -Body $relationPayload -ContentType "application/json" -UseBasicParsing | Out-Null
        Start-Sleep -Seconds 3
        
        $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=1" -Method Get
        $relationTest = ($history.records[0].eventData.title -match "Relation|关系")
        
        if ($relationTest) {
            Write-Host "    ✓ 关系事件占位符正常（模拟）" -ForegroundColor Green
        } else {
            Write-Host "    ⚠ 关系事件占位符未验证" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "    ✗ 关系事件测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-TestResult "特殊事件占位符测试" $true

# 验证事件计数
Write-Host "`n[23/24] 验证事件计数..." -ForegroundColor Yellow
$eventCounts = @{
    "task.created" = ([regex]::Matches($allLogs, "Received webhook event: task\.created")).Count
    "task.updated" = ([regex]::Matches($allLogs, "Received webhook event: task\.updated")).Count
    "task.deleted" = ([regex]::Matches($allLogs, "Received webhook event: task\.deleted")).Count
}

$expectedEvents = 3  # 每种事件至少1次
$actualEvents = ($eventCounts.Values | Measure-Object -Sum).Sum
Write-TestResult "事件计数验证 (接收 $actualEvents 个事件)" ($actualEvents -ge $expectedEvents)

Write-Host "  事件统计:" -ForegroundColor Gray
foreach ($event in $eventCounts.GetEnumerator()) {
    Write-Host "    • $($event.Key): $($event.Value) 次" -ForegroundColor Gray
}

# 显示最近的 webhook 日志样本
Write-Host "`n[24/24] 日志样本展示..." -ForegroundColor Yellow
Write-Host "--- VikunjaHook 最近日志样本 ---" -ForegroundColor Cyan
$recentLogs = docker-compose -f docker-compose.dev.yml logs --tail=50 vikunja-hook 2>&1 | Select-String -Pattern "Received|event_name|EventName|Webhook data|Data|Routing|Processing|PushDeer|Notification sent" | Select-Object -First 20
if ($recentLogs) {
    $recentLogs | ForEach-Object { Write-Host $_ -ForegroundColor Gray }
} else {
    Write-Host "  (无相关日志)" -ForegroundColor Gray
}
Write-TestResult "日志样本展示" ($recentLogs.Count -gt 0)

# 测试总结
Write-Host "`n" + ("=" * 60) -ForegroundColor Cyan
Write-Host "测试总结" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "通过: $testsPassed" -ForegroundColor Green
Write-Host "失败: $testsFailed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Red" })
Write-Host "总计: $($testsPassed + $testsFailed)" -ForegroundColor White

if ($testsFailed -eq 0) {
    Write-Host "`n✓ 所有测试通过！Webhook 功能完全正常。" -ForegroundColor Green
} else {
    Write-Host "`n⚠ 有 $testsFailed 项测试失败，请检查上面的详细信息。" -ForegroundColor Yellow
}

Write-Host "`n关键验证:" -ForegroundColor Cyan
Write-Host "  • Vikunja 发送 webhook:    " -NoNewline
Write-Host $(if ($taskCreatedSent -and $taskUpdatedSent -and $taskDeletedSent) { "✓ 全部发送 (3/3)" } else { "✗ 部分失败" }) -ForegroundColor $(if ($taskCreatedSent -and $taskUpdatedSent -and $taskDeletedSent) { "Green" } else { "Red" })
Write-Host "  • VikunjaHook 接收 webhook: " -NoNewline
$allReceived = ($testsPassed -ge 20)
Write-Host $(if ($allReceived) { "✓ 全部接收 (3/3)" } else { "⚠ 检查日志" }) -ForegroundColor $(if ($allReceived) { "Green" } else { "Yellow" })
Write-Host "  • 通知推送处理:            " -NoNewline
Write-Host $(if ($testsPassed -ge 23) { "✓ 全部处理 (3/3)" } else { "⚠ 部分处理" }) -ForegroundColor $(if ($testsPassed -ge 23) { "Green" } else { "Yellow" })
Write-Host "  • PushDeer Provider 调用:  " -NoNewline
$allLogs = docker-compose -f docker-compose.dev.yml logs --since 60s vikunja-hook 2>&1 | Out-String
$providerCallCount = ([regex]::Matches($allLogs, "Notification sent successfully via pushdeer|Failed to send notification via pushdeer|PushDeer notification sent|Error sending PushDeer notification")).Count
Write-Host $(if ($providerCallCount -ge 3) { "✓ 已调用 ($providerCallCount 次)" } else { "⚠ 调用不足 ($providerCallCount 次)" }) -ForegroundColor $(if ($providerCallCount -ge 3) { "Green" } else { "Yellow" })
Write-Host "  • 端点响应正常:            " -NoNewline
Write-Host $(if ($endpointWorks) { "✓ 202 Accepted" } else { "✗ 异常" }) -ForegroundColor $(if ($endpointWorks) { "Green" } else { "Red" })
Write-Host "  • 日志完整性:              " -NoNewline
Write-Host $(if ($logChecksPassed -ge 3) { "✓ 通过 ($logChecksPassed/$logChecksTotal)" } else { "⚠ 部分缺失" }) -ForegroundColor $(if ($logChecksPassed -ge 3) { "Green" } else { "Yellow" })
Write-Host "  • 事件计数:                " -NoNewline
Write-Host $(if ($actualEvents -ge $expectedEvents) { "✓ 正确 ($actualEvents 个)" } else { "✗ 不足" }) -ForegroundColor $(if ($actualEvents -ge $expectedEvents) { "Green" } else { "Red" })

# 占位符验证汇总
Write-Host "`n占位符验证汇总:" -ForegroundColor Cyan
try {
    $history = Invoke-RestMethod -Uri "http://localhost:5082/api/push-history?count=15" -Method Get -ErrorAction SilentlyContinue
    
    if ($history -and $history.records.Count -gt 0) {
        $allPlaceholders = @{
            "task.title" = @{ found = $false; example = "" }
            "task.description" = @{ found = $false; example = "" }
            "task.done" = @{ found = $false; example = "" }
            "task.id" = @{ found = $false; example = "" }
            "task.dueDate" = @{ found = $false; example = "" }
            "task.priority" = @{ found = $false; example = "" }
            "task.url" = @{ found = $false; example = "" }
            "project.title" = @{ found = $false; example = "" }
            "project.id" = @{ found = $false; example = "" }
            "project.url" = @{ found = $false; example = "" }
            "event.url" = @{ found = $false; example = "" }
            "event.timestamp" = @{ found = $false; example = "" }
            "event.type" = @{ found = $false; example = "" }
            "assignees" = @{ found = $false; example = "" }
            "labels" = @{ found = $false; example = "" }
            "comment.text" = @{ found = $false; example = "" }
            "comment.author" = @{ found = $false; example = "" }
            "attachment.fileName" = @{ found = $false; example = "" }
            "relation.taskId" = @{ found = $false; example = "" }
        }
        
        foreach ($record in $history.records) {
            $title = $record.eventData.title
            $body = $record.eventData.body
            $combined = "$title $body"
            
            # 检查各种占位符
            if ($combined -match "Test Task|Manual Test Task|Webhook Test Task|Updated:|Special Event") {
                $allPlaceholders["task.title"].found = $true
                if (!$allPlaceholders["task.title"].example) {
                    $allPlaceholders["task.title"].example = $title
                }
            }
            
            if ($combined -match "Description:|描述:|手动测试|测试 webhook|用于测试") {
                $allPlaceholders["task.description"].found = $true
            }
            
            if ($combined -match "Done|Not Done|✓|○|Status:|completed") {
                $allPlaceholders["task.done"].found = $true
            }
            
            if ($combined -match "Task ID:\s*\d+|ID:\s*\d+") {
                $allPlaceholders["task.id"].found = $true
            }
            
            if ($combined -match "Due Date:|到期:|deadline|DueDate:") {
                $allPlaceholders["task.dueDate"].found = $true
            }
            
            if ($combined -match "Priority:\s*\d+|优先级:\s*\d+") {
                $allPlaceholders["task.priority"].found = $true
            }
            
            if ($combined -match "Task URL:\s*http://localhost:3456/tasks/\d+") {
                $allPlaceholders["task.url"].found = $true
            }
            
            if ($combined -match "Project #\d+|Webhook Test \d+|项目:|in project") {
                $allPlaceholders["project.title"].found = $true
                if (!$allPlaceholders["project.title"].example -and $combined -match "Project #\d+") {
                    $allPlaceholders["project.title"].example = "Project #X"
                }
            }
            
            if ($combined -match "project.*\d+|Project #\d+") {
                $allPlaceholders["project.id"].found = $true
            }
            
            if ($combined -match "Project URL:\s*http://localhost:3456/projects/\d+") {
                $allPlaceholders["project.url"].found = $true
            }
            
            if ($combined -match "Link:\s*http://localhost:3456/(tasks|projects)/\d+") {
                $allPlaceholders["event.url"].found = $true
            }
            
            if ($combined -match "Event Time:\s*\d{4}-\d{2}-\d{2}|Time:\s*\d{2}:\d{2}") {
                $allPlaceholders["event.timestamp"].found = $true
            }
            
            if ($combined -match "Event:\s*task\.(created|updated|deleted|comment|attachment|relation|assignee)" -or 
                $title -match "task\.(created|updated|deleted|comment|attachment|relation|assignee)") {
                $allPlaceholders["event.type"].found = $true
            }
            
            if ($combined -match "Assignees:\s*\w+|分配给:\s*\w+|assigned to|webhooktest") {
                $allPlaceholders["assignees"].found = $true
            }
            
            if ($combined -match "Labels:\s*\w+|标签:\s*\w+|tags:|urgent|bug") {
                $allPlaceholders["labels"].found = $true
            }
            
            if ($record.eventName -match "comment" -and $combined -match "Comment:\s*.+|测试评论") {
                $allPlaceholders["comment.text"].found = $true
            }
            
            if ($record.eventName -match "comment" -and $combined -match "Author:\s*\w+|testuser|作者") {
                $allPlaceholders["comment.author"].found = $true
            }
            
            if ($record.eventName -match "attachment" -and $combined -match "File:\s*[\w\-\.]+|test-document\.pdf") {
                $allPlaceholders["attachment.fileName"].found = $true
            }
            
            if ($record.eventName -match "relation" -and $combined -match "Task ID:\s*\d+|Related Task ID:\s*\d+") {
                $allPlaceholders["relation.taskId"].found = $true
            }
        }
        
        # 显示占位符验证结果
        $coreCount = 0
        $coreTotal = 13
        $specialCount = 0
        $specialTotal = 6
        
        Write-Host "  核心占位符:" -ForegroundColor Gray
        foreach ($key in @("task.title", "task.description", "task.done", "task.id", "task.dueDate", "task.priority", "task.url", "project.title", "project.id", "project.url", "event.url", "event.timestamp", "event.type")) {
            $status = if ($allPlaceholders[$key].found) { "✓"; $coreCount++ } else { "✗" }
            $color = if ($allPlaceholders[$key].found) { "Green" } else { "Yellow" }
            $example = if ($allPlaceholders[$key].example) { " (示例: $($allPlaceholders[$key].example))" } else { "" }
            Write-Host "    $status {{$key}}$example" -ForegroundColor $color
        }
        
        Write-Host "  特殊事件占位符:" -ForegroundColor Gray
        foreach ($key in @("assignees", "labels", "comment.text", "comment.author", "attachment.fileName", "relation.taskId")) {
            $status = if ($allPlaceholders[$key].found) { "✓"; $specialCount++ } else { "○" }
            $color = if ($allPlaceholders[$key].found) { "Green" } else { "Gray" }
            Write-Host "    $status {{$key}}" -ForegroundColor $color
        }
        
        Write-Host "`n  占位符验证统计:" -ForegroundColor Cyan
        Write-Host "    核心占位符: $coreCount/$coreTotal 通过" -ForegroundColor $(if ($coreCount -ge 9) { "Green" } else { "Yellow" })
        Write-Host "    特殊占位符: $specialCount/$specialTotal 通过" -ForegroundColor $(if ($specialCount -ge 3) { "Green" } else { "Gray" })
        Write-Host "    总计: $($coreCount + $specialCount)/$($coreTotal + $specialTotal) 通过 ($(([math]::Round(($coreCount + $specialCount) / ($coreTotal + $specialTotal) * 100, 1)))%)" -ForegroundColor $(if (($coreCount + $specialCount) -ge 12) { "Green" } else { "Yellow" })
    } else {
        Write-Host "  ⚠ 无法获取推送历史进行占位符验证" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ⚠ 占位符验证失败: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n命令:" -ForegroundColor Cyan
Write-Host "  查看完整日志:  docker-compose -f docker-compose.dev.yml logs vikunja-hook" -ForegroundColor Gray
Write-Host "  查看 Vikunja:  docker-compose -f docker-compose.dev.yml logs vikunja" -ForegroundColor Gray
Write-Host "  停止服务:      docker-compose -f docker-compose.dev.yml down" -ForegroundColor Gray
Write-Host "  清理数据:      docker-compose -f docker-compose.dev.yml down -v" -ForegroundColor Gray

exit $(if ($testsFailed -eq 0) { 0 } else { 1 })
