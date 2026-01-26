# Vikunja MCP Admin 快速参考

## 🚀 快速启动

```bash
cd src/vikunja-mcp-admin
npm install
npm run dev
```

访问: http://localhost:5173

---

## 📋 页面导航

| 页面 | 路径 | 功能 |
|------|------|------|
| Dashboard | `/` | 服务器状态、统计、快速操作 |
| Sessions | `/sessions` | 会话管理、断开连接 |
| Logs | `/logs` | 日志查看、过滤、清除 |
| Tools | `/tools` | 工具列表、测试执行 |
| Configuration | `/config` | 服务器配置管理 |

---

## 🔌 API 端点

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

## 💻 代码示例

### 获取会话
```typescript
const sessions = await adminApi.getSessions()
```

### 断开会话
```typescript
await adminApi.disconnectSession(sessionId)
```

### 获取统计
```typescript
const stats = await adminApi.getStats()
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

## 🎨 UI 组件

### 按钮状态
```vue
<VaButton :loading="loading">Action</VaButton>
```

### 通知
```typescript
notify({
  message: 'Success!',
  color: 'success'
})
```

### 加载遮罩
```vue
<VaInnerLoading :loading="loading">
  <!-- content -->
</VaInnerLoading>
```

---

## 🔧 配置

### 环境变量 (.env)
```env
VITE_API_BASE_URL=/api
```

### Vite 代理 (vite.config.ts)
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

---

## 📊 Dashboard 功能

- ✅ 服务器状态监控
- ✅ 实时统计信息
- ✅ 内存使用显示
- ✅ 会话数量统计
- ✅ 快速操作按钮
- ✅ 自动刷新（10秒）

---

## 👥 Sessions 功能

- ✅ 会话列表显示
- ✅ 状态指示器
- ✅ 单个断开
- ✅ 批量断开
- ✅ 相对时间
- ✅ 自动刷新（5秒）

---

## 📝 Logs 功能

- ✅ 级别过滤
- ✅ 数量选择
- ✅ 颜色标识
- ✅ 清除日志
- ✅ 统计信息
- ✅ 自动刷新（5秒）

---

## 🛠️ Tools 功能

- ✅ 工具列表
- ✅ 子命令选择
- ✅ 参数编辑
- ✅ JSON 验证
- ✅ 执行测试
- ✅ 结果显示

---

## 🎯 快捷键

| 操作 | 快捷键 |
|------|--------|
| 刷新页面 | `Ctrl + R` |
| 打开控制台 | `F12` |
| 搜索 | `Ctrl + F` |

---

## 🐛 调试技巧

### 查看网络请求
```
F12 → Network → XHR
```

### 查看控制台日志
```
F12 → Console
```

### 查看 Vue DevTools
```
安装 Vue DevTools 扩展
F12 → Vue
```

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

## 📦 依赖包

```json
{
  "vue": "^3.x",
  "vuestic-ui": "^1.x",
  "axios": "^1.x",
  "vue-router": "^4.x",
  "pinia": "^2.x"
}
```

---

## 🔒 安全提示

- ⚠️ 生产环境需要身份验证
- ⚠️ 限制 Admin API 访问
- ⚠️ 使用 HTTPS
- ⚠️ 定期更新依赖
- ⚠️ 备份重要数据

---

## 📈 性能优化

### 自动刷新间隔建议
- Dashboard: 10-30 秒
- Sessions: 5-15 秒
- Logs: 5-10 秒

### 日志数量建议
- 日常使用: 100 条
- 故障排查: 200-500 条

### 内存监控
- 正常: < 200 MB
- 警告: 200-500 MB
- 危险: > 500 MB

---

## 🔗 相关链接

- [完整功能文档](./ADMIN_FEATURES.md)
- [升级指南](./UPGRADE_GUIDE.md)
- [使用示例](./EXAMPLES.md)
- [快速启动](./QUICKSTART.md)

---

## 📞 获取帮助

1. 查看文档
2. 检查控制台错误
3. 查看后端日志
4. 提交 GitHub Issue

---

## 🎉 快速测试

### 测试 API
```powershell
.\test-api.ps1
```

### 测试前端
```bash
npm run dev
```

### 构建生产版本
```bash
npm run build
```

---

**版本: 2.0.0** | **更新日期: 2024-01-XX**
