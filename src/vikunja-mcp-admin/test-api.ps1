# Vikunja MCP Admin API 测试脚本
# 用于测试所有 Admin API 端点

$baseUrl = "http://localhost:5000"
$apiBase = "$baseUrl/admin"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Vikunja MCP Admin API 测试" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# 测试服务器健康状态
Write-Host "1. 测试服务器健康状态..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/api/mcp/health" -Method Get
    Write-Host "✓ 服务器状态: $($health.status)" -ForegroundColor Green
    Write-Host "  版本: $($health.version)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 服务器健康检查失败: $_" -ForegroundColor Red
}
Write-Host ""

# 测试获取会话列表
Write-Host "2. 测试获取会话列表..." -ForegroundColor Yellow
try {
    $sessions = Invoke-RestMethod -Uri "$apiBase/sessions" -Method Get
    Write-Host "✓ 会话数量: $($sessions.count)" -ForegroundColor Green
    if ($sessions.sessions.Count -gt 0) {
        Write-Host "  会话列表:" -ForegroundColor Gray
        foreach ($session in $sessions.sessions) {
            Write-Host "    - ID: $($session.sessionId)" -ForegroundColor Gray
            Write-Host "      URL: $($session.apiUrl)" -ForegroundColor Gray
            Write-Host "      类型: $($session.authType)" -ForegroundColor Gray
            Write-Host "      状态: $(if ($session.isExpired) { '过期' } else { '活跃' })" -ForegroundColor Gray
        }
    } else {
        Write-Host "  当前没有活跃会话" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ 获取会话列表失败: $_" -ForegroundColor Red
}
Write-Host ""

# 测试获取服务器统计
Write-Host "3. 测试获取服务器统计..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$apiBase/stats" -Method Get
    Write-Host "✓ 服务器统计:" -ForegroundColor Green
    Write-Host "  服务器名称: $($stats.server.name)" -ForegroundColor Gray
    Write-Host "  版本: $($stats.server.version)" -ForegroundColor Gray
    Write-Host "  运行时间: $($stats.server.uptime)" -ForegroundColor Gray
    Write-Host "  会话总数: $($stats.sessions.total)" -ForegroundColor Gray
    Write-Host "  活跃会话: $($stats.sessions.active)" -ForegroundColor Gray
    Write-Host "  工具数量: $($stats.tools.total)" -ForegroundColor Gray
    Write-Host "  子命令数: $($stats.tools.subcommands)" -ForegroundColor Gray
    $memoryMB = [math]::Round($stats.memory.workingSet / 1MB, 2)
    Write-Host "  内存使用: $memoryMB MB" -ForegroundColor Gray
} catch {
    Write-Host "✗ 获取服务器统计失败: $_" -ForegroundColor Red
}
Write-Host ""

# 测试获取工具列表
Write-Host "4. 测试获取工具列表..." -ForegroundColor Yellow
try {
    $tools = Invoke-RestMethod -Uri "$baseUrl/api/mcp/tools" -Method Get
    Write-Host "✓ 工具数量: $($tools.count)" -ForegroundColor Green
    if ($tools.tools.Count -gt 0) {
        Write-Host "  工具列表:" -ForegroundColor Gray
        foreach ($tool in $tools.tools) {
            Write-Host "    - $($tool.name): $($tool.subcommands.Count) 个子命令" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "✗ 获取工具列表失败: $_" -ForegroundColor Red
}
Write-Host ""

# 测试获取日志
Write-Host "5. 测试获取日志..." -ForegroundColor Yellow
try {
    $logs = Invoke-RestMethod -Uri "$apiBase/logs?count=10" -Method Get
    Write-Host "✓ 日志数量: $($logs.count)" -ForegroundColor Green
    if ($logs.logs.Count -gt 0) {
        Write-Host "  最近的日志:" -ForegroundColor Gray
        foreach ($log in $logs.logs | Select-Object -First 5) {
            $color = switch ($log.level) {
                "ERROR" { "Red" }
                "WARNING" { "Yellow" }
                "INFO" { "Cyan" }
                default { "Gray" }
            }
            Write-Host "    [$($log.timestamp)] [$($log.level)] $($log.message)" -ForegroundColor $color
        }
    } else {
        Write-Host "  当前没有日志记录" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ 获取日志失败: $_" -ForegroundColor Red
}
Write-Host ""

# 测试工具执行（需要会话 ID）
Write-Host "6. 测试工具执行..." -ForegroundColor Yellow
if ($sessions.sessions.Count -gt 0) {
    $sessionId = $sessions.sessions[0].sessionId
    Write-Host "  使用会话 ID: $sessionId" -ForegroundColor Gray
    
    try {
        $headers = @{
            "Content-Type" = "application/json"
            "X-Session-Id" = $sessionId
        }
        $body = @{} | ConvertTo-Json
        
        $result = Invoke-RestMethod -Uri "$apiBase/tools/vikunja_projects/list" -Method Post -Headers $headers -Body $body
        Write-Host "✓ 工具执行成功" -ForegroundColor Green
        Write-Host "  工具: $($result.tool)" -ForegroundColor Gray
        Write-Host "  子命令: $($result.subcommand)" -ForegroundColor Gray
        Write-Host "  成功: $($result.success)" -ForegroundColor Gray
    } catch {
        Write-Host "✗ 工具执行失败: $_" -ForegroundColor Red
        Write-Host "  这可能是因为会话无效或工具不存在" -ForegroundColor Gray
    }
} else {
    Write-Host "⊘ 跳过工具执行测试（没有可用会话）" -ForegroundColor Gray
}
Write-Host ""

# 测试配置 API
Write-Host "7. 测试获取配置..." -ForegroundColor Yellow
try {
    $config = Invoke-RestMethod -Uri "$baseUrl/api/configuration" -Method Get
    Write-Host "✓ 配置信息:" -ForegroundColor Green
    Write-Host "  MCP 服务器名称: $($config.mcp.serverName)" -ForegroundColor Gray
    Write-Host "  最大并发连接: $($config.mcp.maxConcurrentConnections)" -ForegroundColor Gray
    Write-Host "  速率限制: $($config.rateLimit.enabled)" -ForegroundColor Gray
    if ($config.rateLimit.enabled) {
        Write-Host "    每分钟: $($config.rateLimit.requestsPerMinute)" -ForegroundColor Gray
        Write-Host "    每小时: $($config.rateLimit.requestsPerHour)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ 获取配置失败: $_" -ForegroundColor Red
}
Write-Host ""

# 总结
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "测试完成！" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "提示：" -ForegroundColor Yellow
Write-Host "- 如果某些测试失败，请确保后端服务器正在运行" -ForegroundColor Gray
Write-Host "- 工具执行测试需要有效的会话 ID" -ForegroundColor Gray
Write-Host "- 可以通过 Web 界面创建会话后再次运行此脚本" -ForegroundColor Gray
Write-Host ""
Write-Host "访问 Web 界面: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""
