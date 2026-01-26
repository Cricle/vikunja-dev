# Vikunja MCP Admin Web 界面 - 完整功能文档

## 概述

Vikunja MCP Admin Web 界面已完全更新，提供全面的服务器管理和控制功能。

## 更新内容

### 1. 类型定义 (`src/types/index.ts`)

新增以下类型定义：

- **Session**: 会话详细信息
  - `sessionId`: 会话 ID
  - `apiUrl`: Vikunja API URL
  - `authType`: 认证类型
  - `created`: 创建时间
  - `lastAccessed`: 最后访问时间
  - `isExpired`: 是否过期

- **ServerStats**: 服务器统计信息
  - `server`: 服务器信息（名称、版本、运行时间）
  - `sessions`: 会话统计（总数、活跃数）
  - `tools`: 工具统计（总数、子命令数）
  - `memory`: 内存使用情况

- **LogEntry**: 日志条目
  - `timestamp`: 时间戳
  - `level`: 日志级别
  - `message`: 日志消息

- **ToolExecutionRequest**: 工具执行请求
- **ToolExecutionResult**: 工具执行结果

### 2. API 服务 (`src/services/api.ts`)

新增 `adminApi` 对象，包含以下方法：

#### 会话管理
- `getSessions()`: 获取所有会话
- `disconnectSession(sessionId)`: 断开特定会话
- `disconnectAllSessions()`: 断开所有会话

#### 服务器统计
- `getStats()`: 获取服务器统计信息（内存、会话、工具等）

#### 工具执行
- `executeTool(request)`: 执行工具命令（用于测试）
  - 支持指定会话 ID
  - 支持自定义参数

#### 日志管理
- `getLogs(count, level)`: 获取日志
  - `count`: 日志数量（默认 100）
  - `level`: 日志级别过滤（可选）
- `clearLogs()`: 清除所有日志

### 3. Dashboard 页面 (`src/views/Dashboard.vue`)

#### 新增功能
- **实时服务器统计**
  - 服务器运行时间
  - 活跃会话数 / 总会话数
  - 工具数量和子命令数
  - 内存使用情况（工作集）

- **快速操作按钮**
  - 刷新状态
  - 配置服务器
  - 查看工具
  - 管理会话
  - 断开所有会话
  - 清除日志

- **自动刷新功能**
  - 可选择每 10 秒自动刷新
  - 开关控制

- **加载状态指示器**
  - 所有操作都有加载动画
  - 成功/失败通知

### 4. Sessions 页面 (`src/views/Sessions.vue`)

#### 新增功能
- **会话列表显示**
  - Session ID（代码格式）
  - API URL
  - 认证类型（带颜色标识）
  - 状态（活跃/过期）
  - 创建时间（绝对时间 + 相对时间）
  - 最后访问时间（绝对时间 + 相对时间）

- **会话操作**
  - 单个会话断开按钮
  - 批量断开所有会话
  - 刷新会话列表

- **会话统计**
  - 总会话数
  - 活跃会话数
  - 过期会话数
  - JWT 认证会话数

- **自动刷新**
  - 每 5 秒自动刷新
  - 开关控制

- **响应式设计**
  - 移动端友好的表格布局
  - 相对时间显示（如 "5 min ago"）

### 5. Logs 页面 (`src/views/Logs.vue`)

#### 新增功能
- **日志过滤**
  - 按级别过滤（All, Debug, Info, Warning, Error）
  - 日志数量选择（50, 100, 200, 500）

- **日志显示**
  - 时间戳（HH:mm:ss 格式）
  - 日志级别（带颜色标识）
  - 日志消息
  - 级别颜色边框（Error=红色, Warning=黄色, Info=蓝色, Debug=灰色）

- **日志操作**
  - 刷新日志
  - 清除所有日志
  - 自动刷新（每 5 秒）

- **日志统计**
  - 总日志数
  - 错误数量
  - 警告数量
  - 信息数量

- **深色主题日志容器**
  - 代码风格显示
  - 最大高度 600px，可滚动
  - 悬停高亮

### 6. Tools 页面 (`src/views/Tools.vue`)

#### 新增功能
- **工具列表**
  - 工具名称和描述
  - 子命令列表（可点击）
  - 子命令数量标识
  - 测试工具按钮

- **工具测试面板**
  - **配置区域**
    - 子命令选择器
    - 会话选择器（可选）
    - JSON 参数编辑器
    - JSON 格式验证
    - 示例参数加载

  - **执行控制**
    - 执行按钮
    - 清除参数按钮
    - 加载示例按钮

  - **结果显示**
    - 成功/失败状态标识
    - 执行时间显示
    - 结果内容（JSON 格式化）
    - 错误消息显示

- **交互功能**
  - 点击子命令快速选择
  - 选中工具高亮显示
  - 实时 JSON 验证
  - 结果自动格式化

## 技术特性

### 1. 错误处理
- 所有 API 调用都有 try-catch 错误处理
- 用户友好的错误消息
- 控制台详细错误日志

### 2. 加载状态
- 所有异步操作都有加载指示器
- 按钮加载动画
- 全局加载遮罩（VaInnerLoading）

### 3. 通知系统
- 使用 Vuestic UI Toast 组件
- 成功操作显示绿色通知
- 失败操作显示红色通知
- 自动消失

### 4. 响应式设计
- 移动端优化布局
- 断点适配（xs, md, lg）
- 灵活的网格系统
- 触摸友好的交互

### 5. TypeScript 类型安全
- 完整的类型定义
- 类型推断
- 编译时类型检查

### 6. 自动刷新
- Dashboard: 10 秒间隔
- Sessions: 5 秒间隔
- Logs: 5 秒间隔
- 可开关控制
- 组件卸载时自动清理

## API 端点映射

### 后端端点 (AdminController.cs)
```
GET    /admin/sessions              -> 获取所有会话
DELETE /admin/sessions/{sessionId}  -> 断开特定会话
DELETE /admin/sessions              -> 断开所有会话
GET    /admin/stats                 -> 获取服务器统计
POST   /admin/tools/{tool}/{sub}    -> 执行工具（需要 X-Session-Id header）
GET    /admin/logs                  -> 获取日志（参数: count, level）
DELETE /admin/logs                  -> 清除日志
```

### 前端 API 服务
```typescript
adminApi.getSessions()
adminApi.disconnectSession(sessionId)
adminApi.disconnectAllSessions()
adminApi.getStats()
adminApi.executeTool({ toolName, subcommand, parameters, sessionId })
adminApi.getLogs(count, level)
adminApi.clearLogs()
```

## 使用说明

### 1. 启动开发服务器
```bash
cd src/vikunja-mcp-admin
npm install
npm run dev
```

### 2. 访问界面
打开浏览器访问 `http://localhost:5173`

### 3. 配置代理
确保 `vite.config.ts` 中配置了正确的后端代理：
```typescript
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true
    }
  }
}
```

### 4. 环境变量
创建 `.env` 文件（参考 `.env.example`）：
```
VITE_API_BASE_URL=/api
```

## 界面截图说明

### Dashboard
- 顶部：服务器状态卡片（状态、服务器名、版本、最后检查时间）
- 中部：服务器统计（运行时间、会话、工具、内存）
- 底部：快速统计卡片和操作按钮

### Sessions
- 顶部：操作按钮（刷新、断开所有）
- 中部：会话表格（详细信息、状态、操作）
- 底部：会话统计卡片

### Logs
- 顶部：过滤控制（级别、数量、自动刷新、操作）
- 中部：日志显示（深色主题、颜色标识）
- 底部：日志统计

### Tools
- 顶部：工具卡片列表
- 底部：工具测试面板（配置 + 结果）

## 注意事项

1. **会话管理**: 断开会话操作不可逆，请谨慎操作
2. **日志清除**: 清除日志会删除所有历史记录
3. **工具测试**: 需要有效的会话 ID 才能执行工具
4. **自动刷新**: 长时间开启可能增加服务器负载
5. **内存显示**: 内存使用量以 MB 为单位显示

## 后续改进建议

1. 添加会话详情弹窗
2. 日志导出功能
3. 工具执行历史记录
4. 实时 WebSocket 更新
5. 用户权限管理
6. 配置备份和恢复
7. 性能监控图表
8. 告警和通知系统

## 技术栈

- **前端框架**: Vue 3 (Composition API)
- **UI 组件库**: Vuestic UI
- **HTTP 客户端**: Axios
- **类型系统**: TypeScript
- **构建工具**: Vite
- **路由**: Vue Router
- **状态管理**: Pinia

## 贡献

欢迎提交 Issue 和 Pull Request！
