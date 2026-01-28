#!/usr/bin/env pwsh
# Webhook 功能测试脚本

Write-Host "=== Vikunja Webhook 功能测试 ===" -ForegroundColor Cyan

# 检查 .env 文件
if (-not (Test-Path ".env")) {
    Write-Host "错误: .env 文件不存在，请先创建并配置 VIKUNJA_API_TOKEN" -ForegroundColor Red
    exit 1
}

# 读取 API Token
$envContent = Get-Content ".env" -Raw
if ($envContent -match 'VIKUNJA_API_TOKEN=(.+)') {
    $apiToken = $matches[1].Trim()
    if ($apiToken -eq "your_vikunja_api_token_here" -or [string]::IsNullOrWhiteSpace($apiToken)) {
        Write-Host "错误: 请在 .env 文件中配置有效的 VIKUNJA_API_TOKEN" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "错误: .env 文件中未找到 VIKUNJA_API_TOKEN" -ForegroundColor Red
    exit 1
}

Write-Host "`n步骤 1: 启动服务..." -ForegroundColor Yellow
docker-compose up -d

Write-Host "`n等待服务启动 (30秒)..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

Write-Host "`n步骤 2: 检查服务状态..." -ForegroundColor Yellow
docker-compose ps

Write-Host "`n步骤 3: 检查 Vikunja 健康状态..." -ForegroundColor Yellow
try {
    $vikunjaHealth = Invoke-RestMethod -Uri "http://localhost:8080/health" -Method Get -TimeoutSec 5
    Write-Host "✓ Vikunja 服务正常" -ForegroundColor Green
} catch {
    Write-Host "✗ Vikunja 服务未就绪: $_" -ForegroundColor Red
    Write-Host "`n查看 Vikunja 日志:" -ForegroundColor Yellow
    docker-compose logs --tail=20 vikunja
    exit 1
}

Write-Host "`n步骤 4: 检查 VikunjaHook 健康状态..." -ForegroundColor Yellow
try {
    $hookHealth = Invoke-RestMethod -Uri "http://localhost:5082/health" -Method Get -TimeoutSec 5
    Write-Host "✓ VikunjaHook 服务正常" -ForegroundColor Green
} catch {
    Write-Host "✗ VikunjaHook 服务未就绪: $_" -ForegroundColor Red
    Write-Host "`n查看 VikunjaHook 日志:" -ForegroundColor Yellow
    docker-compose logs --tail=20 vikunja-hook
    exit 1
}

Write-Host "`n步骤 5: 获取项目列表..." -ForegroundColor Yellow
$headers = @{
    "Authorization" = "Bearer $apiToken"
    "Content-Type" = "application/json"
}

try {
    $projects = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects" -Headers $headers -Method Get
    if ($projects.Count -eq 0) {
        Write-Host "警告: 没有找到项目，创建测试项目..." -ForegroundColor Yellow
        $newProject = @{
            title = "Webhook Test Project"
            description = "用于测试 webhook 功能"
        } | ConvertTo-Json
        
        $project = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects" -Headers $headers -Method Post -Body $newProject
        $projectId = $project.id
        Write-Host "✓ 创建测试项目: $projectId" -ForegroundColor Green
    } else {
        $projectId = $projects[0].id
        Write-Host "✓ 使用现有项目: $projectId" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ 获取项目失败: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n步骤 6: 配置 Webhook..." -ForegroundColor Yellow
$webhookUrl = "http://vikunja-hook:5082/api/webhook"
$webhook = @{
    target_url = $webhookUrl
    events = @("task.created", "task.updated", "task.deleted")
    project_id = $projectId
} | ConvertTo-Json

try {
    $createdWebhook = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/webhooks" -Headers $headers -Method Post -Body $webhook
    $webhookId = $createdWebhook.id
    Write-Host "✓ Webhook 创建成功: $webhookId" -ForegroundColor Green
    Write-Host "  URL: $webhookUrl" -ForegroundColor Gray
    Write-Host "  Events: task.created, task.updated, task.deleted" -ForegroundColor Gray
} catch {
    Write-Host "✗ 创建 Webhook 失败: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n步骤 7: 创建测试任务..." -ForegroundColor Yellow
$task = @{
    title = "Webhook Test Task - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    description = "这是一个用于测试 webhook 的任务"
    project_id = $projectId
} | ConvertTo-Json

try {
    $createdTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/projects/$projectId/tasks" -Headers $headers -Method Put -Body $task
    $taskId = $createdTask.id
    Write-Host "✓ 任务创建成功: $taskId" -ForegroundColor Green
    Write-Host "  标题: $($createdTask.title)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 创建任务失败: $_" -ForegroundColor Red
    exit 1
}

Write-Host "`n等待 webhook 处理 (5秒)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "`n步骤 8: 更新任务..." -ForegroundColor Yellow
$updateTask = @{
    title = "Updated Webhook Test Task - $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    done = $true
} | ConvertTo-Json

try {
    $updatedTask = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId" -Headers $headers -Method Post -Body $updateTask
    Write-Host "✓ 任务更新成功" -ForegroundColor Green
    Write-Host "  新标题: $($updatedTask.title)" -ForegroundColor Gray
    Write-Host "  状态: 已完成" -ForegroundColor Gray
} catch {
    Write-Host "✗ 更新任务失败: $_" -ForegroundColor Red
}

Write-Host "`n等待 webhook 处理 (5秒)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "`n步骤 9: 查看 VikunjaHook 日志..." -ForegroundColor Yellow
Write-Host "--- 最近的 webhook 事件 ---" -ForegroundColor Cyan
docker-compose logs --tail=50 vikunja-hook | Select-String -Pattern "webhook|task|event" -Context 0,2

Write-Host "`n步骤 10: 删除测试任务..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/tasks/$taskId" -Headers $headers -Method Delete | Out-Null
    Write-Host "✓ 任务删除成功" -ForegroundColor Green
} catch {
    Write-Host "✗ 删除任务失败: $_" -ForegroundColor Red
}

Write-Host "`n等待 webhook 处理 (5秒)..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

Write-Host "`n步骤 11: 再次查看日志..." -ForegroundColor Yellow
Write-Host "--- 完整的 webhook 事件日志 ---" -ForegroundColor Cyan
docker-compose logs --tail=100 vikunja-hook | Select-String -Pattern "webhook|task|event" -Context 0,2

Write-Host "`n步骤 12: 清理 Webhook..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "http://localhost:8080/api/v1/webhooks/$webhookId" -Headers $headers -Method Delete | Out-Null
    Write-Host "✓ Webhook 删除成功" -ForegroundColor Green
} catch {
    Write-Host "✗ 删除 Webhook 失败: $_" -ForegroundColor Red
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Cyan
Write-Host "`n总结:" -ForegroundColor Yellow
Write-Host "1. 创建了 webhook 监听 task.created, task.updated, task.deleted 事件" -ForegroundColor White
Write-Host "2. 创建了测试任务，触发 task.created 事件" -ForegroundColor White
Write-Host "3. 更新了任务，触发 task.updated 事件" -ForegroundColor White
Write-Host "4. 删除了任务，触发 task.deleted 事件" -ForegroundColor White
Write-Host "`n请检查上面的日志，确认 webhook 事件是否被正确接收和处理。" -ForegroundColor Yellow
Write-Host "`n如需停止服务，运行: docker-compose down" -ForegroundColor Gray
