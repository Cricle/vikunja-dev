# Vikunja MCP Admin - 快速开始指南

## 🚀 快速启动（3步）

### 步骤 1: 安装依赖

```bash
cd src/vikunja-mcp-admin
npm install
```

### 步骤 2: 启动 MCP 服务器

在另一个终端中：

```bash
cd src/VikunjaHook/VikunjaHook
dotnet run
```

等待服务器启动在 `http://localhost:5082`

### 步骤 3: 启动管理界面

```bash
npm run dev
```

或使用 PowerShell 脚本：

```powershell
.\start.ps1
```

管理界面将在 `http://localhost:3000` 打开

---

## 📋 页面导航

| 页面 | 路径 | 功能 |
|------|------|------|
| Dashboard | `/` | 服务器状态、统计、快速操作 |
| Configuration | `/config` | 服务器配置管理 |
| Tools | `/tools` | 工具列表、测试执行 |
| Sessions | `/sessions` | 会话管理、断开连接 |
| Logs | `/logs` | 日志查看、过滤、清除 |

---

## 📱 使用管理界面

### Dashboard
- 查看服务器状态和统计信息
- 快速访问所有功能
- 实时健康监控
- 内存使用显示
- 自动刷新（10秒）

### Configuration
1. 导航到侧边栏的 **Configuration**
2. 修改设置：
   - Vikunja 超时时间
   - MCP 服务器设置
   - CORS 源
   - 速率限制
3. 点击 **Save Configuration**

### Tools
- 查看所有 5 个注册的工具
- 查看 45+ 个子命令
- 测试工具功能
- 选择会话执行
- JSON 参数编辑

### Sessions
- 监控活跃的认证会话
- 查看会话详情
- 断开单个或所有会话
- 会话统计信息
- 自动刷新（5秒）

### Logs
- 实时查看服务器日志
- 按日志级别过滤
- 选择日志数量
- 清除日志
- 日志统计
- 自动刷新（5秒）

---

## 🔌 API 端点快速参考

### 会话管理
```
GET    /admin/sessions              # 获取所有会话
DELETE /admin/sessions/{id}         # 断开特定会话
DELETE /admin/sessions              # 断开所有会话
```

### 服务器统计
```
GET    /admin/stats                 # 获取服务器统计
```

### 工具执行
```
POST   /admin/tools/{tool}/{sub}    # 执行工具
Header: X-Session-Id: {sessionId}
Body: { "param": "value" }
```

### 日志管理
```
GET    /admin/logs?count=100&level=Info  # 获取日志
DELETE /admin/logs                       # 清除日志
```

---

## 🔧 配置技巧

### 添加 CORS 源

1. 进入 **Configuration** → **CORS Settings**
2. 点击 **Add Origin**
3. 输入源 URL（例如 `https://example.com`）
4. 点击 **Add**
5. 保存配置

### 调整速率限制

1. 进入 **Configuration** → **Rate Limiting**
2. 切换 **Enable Rate Limiting**
3. 设置 **Requests Per Minute**（默认：60）
4. 设置 **Requests Per Hour**（默认：1000）
5. 保存配置

---

## 💻 代码示例

### 获取会话
```typescript
const sessions = await adminApi.getSessions()
```

### 断开会话
```typescript
await adminApi.disconnectSession(sessionId)
```

### 执行工具
```typescript
const result = await adminApi.executeTool({
  toolName: 'vikunja_projects',
  subcommand: 'list',
  parameters: {},
  sessionId: 'xxx'
})
```

### 获取日志
```typescript
const logs = await adminApi.getLogs(100, 'Error')
```

---

## 🐛 故障排除

### 管理界面无法启动

**问题**: `npm run dev` 失败

**解决方案**:
```bash
# 清除 node_modules 并重新安装
rm -rf node_modules package-lock.json
npm install
```

### 无法连接到 MCP 服务器

**问题**: "Failed to fetch" 错误

**解决方案**:
1. 确保 MCP 服务器运行在 5082 端口
2. 检查服务器健康: `http://localhost:5082/mcp/health`
3. 验证 CORS 设置允许 `http://localhost:3000`

### 配置更改未保存

**问题**: 保存按钮不工作

**解决方案**:
- 当前配置仅存储在内存中
- 要持久化更改，需要手动更新 `appsettings.json`
- 未来版本将包含配置管理后端 API

---

## ⚠️ 常见错误

### 404 Not Found
```
原因: API 端点不存在或后端未运行
解决: 检查后端服务状态
```

### 401 Unauthorized
```
原因: 会话无效或已过期
解决: 重新创建会话
```

### 500 Internal Server Error
```
原因: 后端处理错误
解决: 查看后端日志
```

### CORS Error
```
原因: 跨域配置问题
解决: 检查 CORS 设置
```

---

## 🎯 生产部署

### 构建生产版本

```bash
npm run build
```

### 预览生产构建

```bash
npm run preview
```

或使用静态文件服务器：

```bash
npx serve dist
```

### 部署到 Web 服务器

1. 构建项目: `npm run build`
2. 将 `dist` 文件夹复制到 Web 服务器
3. 配置 Web 服务器：
   - 为所有路由提供 `index.html`（SPA 模式）
   - 将 `/api/*` 请求代理到 MCP 服务器
4. 更新 MCP 服务器的 CORS 设置以允许你的域名

### Nginx 配置示例

```nginx
server {
    listen 80;
    server_name admin.example.com;
    root /var/www/vikunja-mcp-admin;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api/ {
        proxy_pass http://localhost:5082/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

---

## 🧪 测试

### 测试 API
```powershell
.\test-api.ps1
```

### 测试前端
```bash
npm run dev
```

---

## 📚 更多文档

- [完整功能文档](./ADMIN_FEATURES.md) - 详细功能说明
- [使用示例](./EXAMPLES.md) - 实际使用案例
- [升级指南](./UPGRADE_GUIDE.md) - 版本升级说明

---

## 🆘 需要帮助？

1. 查看文档
2. 检查控制台错误（F12）
3. 查看后端日志
4. 提交 GitHub Issue

---

## 🎉 快速测试清单

- [ ] Dashboard 显示正常
- [ ] Sessions 管理正常
- [ ] Logs 查看正常
- [ ] Tools 测试正常
- [ ] 自动刷新工作正常
- [ ] 通知显示正常

---

**享受使用 Vikunja MCP 服务器管理！** 🎉
