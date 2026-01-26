# Vikunja MCP Admin 使用示例

## 目录
1. [Dashboard 使用示例](#dashboard-使用示例)
2. [Sessions 管理示例](#sessions-管理示例)
3. [Logs 查看示例](#logs-查看示例)
4. [Tools 测试示例](#tools-测试示例)
5. [API 调用示例](#api-调用示例)

---

## Dashboard 使用示例

### 场景 1: 监控服务器状态
```
1. 打开 Dashboard 页面
2. 查看服务器状态卡片
   - 状态: Healthy (绿色标识)
   - 服务器: VikunjaHook MCP Server
   - 版本: 1.0.0
   - 最后检查: 2024-01-15 10:30:45

3. 查看服务器统计
   - 运行时间: 2 days, 5 hours
   - 活跃会话: 3 / 5
   - 工具: 5 (45 个子命令)
   - 内存使用: 125.50 MB
```

### 场景 2: 使用快速操作
```
1. 点击"刷新状态"按钮
   → 所有数据自动更新
   → 显示成功通知

2. 点击"断开所有会话"
   → 弹出确认对话框
   → 确认后断开所有会话
   → 会话数变为 0

3. 启用自动刷新
   → 打开"Auto Refresh"开关
   → 每 10 秒自动刷新数据
```

---

## Sessions 管理示例

### 场景 1: 查看会话列表
```
会话列表显示：

┌─────────────────────────────────────────────────────────────────────┐
│ Session ID              │ API URL                    │ Auth Type │
├─────────────────────────────────────────────────────────────────────┤
│ 123e4567-e89b-12d3...   │ https://vikunja.io/api/v1  │ JWT       │
│ 创建: 2024-01-15 08:00  │ 最后访问: 2 min ago        │ 活跃      │
├─────────────────────────────────────────────────────────────────────┤
│ 987f6543-c21a-34b5...   │ https://tasks.example.com  │ ApiToken  │
│ 创建: 2024-01-14 15:30  │ 最后访问: 1 hour ago       │ 过期      │
└─────────────────────────────────────────────────────────────────────┘

统计信息：
- 总会话数: 2
- 活跃: 1
- 过期: 1
- JWT 认证: 1
```

### 场景 2: 断开单个会话
```
1. 找到要断开的会话
2. 点击该会话行的"Disconnect"按钮
3. 确认操作
4. 会话从列表中移除
5. 显示成功通知
```

### 场景 3: 批量管理会话
```
1. 点击顶部"Disconnect All"按钮
2. 确认对话框：
   "Are you sure you want to disconnect ALL sessions?"
3. 点击确认
4. 所有会话被断开
5. 列表显示"No Active Sessions"
```

---

## Logs 查看示例

### 场景 1: 查看所有日志
```
日志显示（深色主题）：

10:30:45 [INFO]    VikunjaHook MCP Server started successfully
10:30:46 [INFO]    Registered 5 MCP tools with 45 total subcommands
10:30:47 [DEBUG]   Configuration validated successfully
10:31:00 [INFO]    New session created: 123e4567-e89b-12d3...
10:31:15 [WARNING] Session 987f6543-c21a-34b5... is about to expire
10:32:00 [ERROR]   Failed to connect to Vikunja API: Connection timeout

统计信息：
- 总日志: 150
- 错误: 2
- 警告: 5
- 信息: 143
```

### 场景 2: 过滤错误日志
```
1. 选择日志级别: "Error"
2. 点击"Refresh"
3. 只显示错误日志：

10:32:00 [ERROR] Failed to connect to Vikunja API: Connection timeout
11:15:30 [ERROR] Authentication failed for session 456...
```

### 场景 3: 调整日志数量
```
1. 选择日志数量: "500"
2. 自动刷新日志
3. 显示最近 500 条日志
4. 可滚动查看
```

---

## Tools 测试示例

### 场景 1: 测试项目列表工具
```
1. 在工具列表中找到"vikunja_projects"
2. 点击"Test Tool"按钮
3. 测试面板打开

配置：
- 子命令: list
- 会话: 123e4567... (https://vikunja.io/api/v1)
- 参数: {}

4. 点击"Execute"按钮
5. 查看结果：

✓ Success
Execution time: 245ms

Result:
{
  "projects": [
    {
      "id": 1,
      "title": "Personal Tasks",
      "description": "My personal task list"
    },
    {
      "id": 2,
      "title": "Work Projects",
      "description": "Work-related tasks"
    }
  ],
  "count": 2
}
```

### 场景 2: 测试创建任务
```
1. 选择工具: vikunja_tasks
2. 选择子命令: create
3. 点击"Sample"加载示例参数
4. 编辑参数：

{
  "title": "Complete documentation",
  "description": "Write user guide for MCP Admin",
  "projectId": 1,
  "dueDate": "2024-01-20"
}

5. 点击"Execute"
6. 查看结果：

✓ Success
Execution time: 312ms

Result:
{
  "task": {
    "id": 42,
    "title": "Complete documentation",
    "projectId": 1,
    "created": "2024-01-15T10:45:00Z"
  }
}
```

### 场景 3: 处理错误
```
1. 输入无效的 JSON 参数：
{
  "title": "Test
  // 缺少闭合引号和括号
}

2. 显示错误：
❌ Invalid JSON format

3. 修正后执行，但 API 返回错误：

✗ Failed
Execution time: 150ms

Error:
Project with ID 999 not found
```

---

## API 调用示例

### JavaScript/TypeScript 示例

#### 1. 获取会话列表
```typescript
import { adminApi } from './services/api'

async function getSessions() {
  try {
    const sessions = await adminApi.getSessions()
    console.log('Sessions:', sessions)
    
    sessions.forEach(session => {
      console.log(`- ${session.sessionId}`)
      console.log(`  URL: ${session.apiUrl}`)
      console.log(`  Status: ${session.isExpired ? 'Expired' : 'Active'}`)
    })
  } catch (error) {
    console.error('Failed to get sessions:', error)
  }
}
```

#### 2. 获取服务器统计
```typescript
async function getStats() {
  try {
    const stats = await adminApi.getStats()
    
    console.log('Server:', stats.server.name)
    console.log('Uptime:', stats.server.uptime)
    console.log('Active Sessions:', stats.sessions.active)
    console.log('Memory:', (stats.memory.workingSet / 1024 / 1024).toFixed(2), 'MB')
  } catch (error) {
    console.error('Failed to get stats:', error)
  }
}
```

#### 3. 执行工具
```typescript
async function executeTool() {
  try {
    const result = await adminApi.executeTool({
      toolName: 'vikunja_projects',
      subcommand: 'list',
      parameters: {},
      sessionId: 'your-session-id'
    })
    
    if (result.success) {
      console.log('Result:', result.result)
    } else {
      console.error('Error:', result.error)
    }
  } catch (error) {
    console.error('Failed to execute tool:', error)
  }
}
```

#### 4. 获取日志
```typescript
async function getLogs() {
  try {
    const logs = await adminApi.getLogs(100, 'Error')
    
    logs.forEach(log => {
      console.log(`[${log.timestamp}] [${log.level}] ${log.message}`)
    })
  } catch (error) {
    console.error('Failed to get logs:', error)
  }
}
```

### cURL 示例

#### 1. 获取会话列表
```bash
curl http://localhost:5000/admin/sessions
```

#### 2. 断开会话
```bash
curl -X DELETE http://localhost:5000/admin/sessions/123e4567-e89b-12d3-a456-426614174000
```

#### 3. 获取服务器统计
```bash
curl http://localhost:5000/admin/stats
```

#### 4. 执行工具
```bash
curl -X POST http://localhost:5000/admin/tools/vikunja_projects/list \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: your-session-id" \
  -d '{}'
```

#### 5. 获取日志
```bash
# 获取最近 50 条日志
curl "http://localhost:5000/admin/logs?count=50"

# 只获取错误日志
curl "http://localhost:5000/admin/logs?count=100&level=Error"
```

#### 6. 清除日志
```bash
curl -X DELETE http://localhost:5000/admin/logs
```

### PowerShell 示例

#### 1. 获取会话列表
```powershell
$sessions = Invoke-RestMethod -Uri "http://localhost:5000/admin/sessions" -Method Get
$sessions.sessions | Format-Table sessionId, apiUrl, authType, isExpired
```

#### 2. 获取服务器统计
```powershell
$stats = Invoke-RestMethod -Uri "http://localhost:5000/admin/stats" -Method Get
Write-Host "Server: $($stats.server.name)"
Write-Host "Uptime: $($stats.server.uptime)"
Write-Host "Active Sessions: $($stats.sessions.active)"
```

#### 3. 执行工具
```powershell
$headers = @{
    "Content-Type" = "application/json"
    "X-Session-Id" = "your-session-id"
}
$body = @{} | ConvertTo-Json

$result = Invoke-RestMethod `
    -Uri "http://localhost:5000/admin/tools/vikunja_projects/list" `
    -Method Post `
    -Headers $headers `
    -Body $body

$result | ConvertTo-Json -Depth 10
```

---

## 实际使用场景

### 场景 1: 日常监控
```
1. 早上打开 Dashboard
2. 检查服务器状态和统计
3. 查看是否有异常会话
4. 检查错误日志
5. 启用自动刷新保持监控
```

### 场景 2: 故障排查
```
1. 用户报告功能异常
2. 打开 Logs 页面
3. 过滤 Error 级别日志
4. 找到相关错误信息
5. 检查对应的会话状态
6. 必要时断开异常会话
```

### 场景 3: 功能测试
```
1. 开发新功能后
2. 打开 Tools 页面
3. 选择相关工具
4. 输入测试参数
5. 执行并验证结果
6. 检查日志确认无错误
```

### 场景 4: 性能优化
```
1. 定期检查 Dashboard 统计
2. 监控内存使用趋势
3. 查看会话数量变化
4. 清理过期会话
5. 必要时重启服务
```

### 场景 5: 安全审计
```
1. 查看所有活跃会话
2. 检查 API URL 和认证类型
3. 识别可疑会话
4. 断开未授权会话
5. 查看相关日志记录
```

---

## 最佳实践

### 1. 会话管理
- ✅ 定期检查会话列表
- ✅ 及时清理过期会话
- ✅ 监控会话创建频率
- ❌ 避免创建过多会话

### 2. 日志管理
- ✅ 定期查看错误日志
- ✅ 使用级别过滤提高效率
- ✅ 重要日志及时备份
- ❌ 避免日志文件过大

### 3. 工具测试
- ✅ 使用有效的会话 ID
- ✅ 验证 JSON 参数格式
- ✅ 检查执行结果
- ❌ 避免在生产环境随意测试

### 4. 性能监控
- ✅ 关注内存使用趋势
- ✅ 监控会话数量
- ✅ 合理设置自动刷新间隔
- ❌ 避免过于频繁的刷新

---

## 故障排除

### 问题 1: 无法获取会话列表
```
症状: 会话列表显示为空或加载失败
解决:
1. 检查后端服务是否运行
2. 验证 API 端点配置
3. 查看浏览器控制台错误
4. 检查网络连接
```

### 问题 2: 工具执行失败
```
症状: 执行工具时返回错误
解决:
1. 确认会话 ID 有效
2. 验证参数格式正确
3. 检查工具名称和子命令
4. 查看详细错误消息
```

### 问题 3: 日志不显示
```
症状: 日志页面为空
解决:
1. 检查日志文件是否存在
2. 验证日志目录权限
3. 确认日志级别过滤
4. 尝试刷新页面
```

---

**更多示例和文档，请参考 `ADMIN_FEATURES.md`**
