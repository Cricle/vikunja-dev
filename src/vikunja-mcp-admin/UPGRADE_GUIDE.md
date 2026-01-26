# Vikunja MCP Admin 升级指南

## 更新概述

本次更新为 Vikunja MCP Admin Web 界面添加了完整的服务器管理和控制功能。

## 更新的文件列表

### 1. 核心文件
- ✅ `src/types/index.ts` - 新增类型定义
- ✅ `src/services/api.ts` - 新增 adminApi 服务
- ✅ `src/views/Dashboard.vue` - 完全重写，新增统计和快速操作
- ✅ `src/views/Sessions.vue` - 完全重写，新增会话管理功能
- ✅ `src/views/Logs.vue` - 完全重写，新增日志管理功能
- ✅ `src/views/Tools.vue` - 完全重写，新增工具测试功能

### 2. 文档文件
- ✅ `ADMIN_FEATURES.md` - 完整功能文档
- ✅ `UPGRADE_GUIDE.md` - 本升级指南

## 新增功能清单

### Dashboard 页面
- [x] 实时服务器统计信息
- [x] 内存使用监控
- [x] 会话数量统计
- [x] 工具数量统计
- [x] 快速操作按钮
- [x] 自动刷新功能（10秒）
- [x] 断开所有会话
- [x] 清除日志

### Sessions 页面
- [x] 显示所有会话详细信息
- [x] 会话状态指示器（活跃/过期）
- [x] 单个会话断开功能
- [x] 批量断开所有会话
- [x] 相对时间显示
- [x] 会话统计卡片
- [x] 自动刷新功能（5秒）

### Logs 页面
- [x] 日志级别过滤（Debug, Info, Warning, Error）
- [x] 日志数量选择（50, 100, 200, 500）
- [x] 清除日志功能
- [x] 自动刷新开关（5秒）
- [x] 日志级别颜色标识
- [x] 深色主题日志显示
- [x] 日志统计卡片

### Tools 页面
- [x] 工具测试功能
- [x] 参数输入表单（JSON）
- [x] JSON 格式验证
- [x] 执行结果显示
- [x] 会话选择器
- [x] 示例参数加载
- [x] 执行时间显示
- [x] 错误消息显示

## 安装步骤

### 1. 确保后端已更新
确保 `AdminController.cs` 已包含所有必要的端点：
- GET /admin/sessions
- DELETE /admin/sessions/{sessionId}
- DELETE /admin/sessions
- GET /admin/stats
- POST /admin/tools/{toolName}/{subcommand}
- GET /admin/logs
- DELETE /admin/logs

### 2. 安装依赖
```bash
cd src/vikunja-mcp-admin
npm install
```

### 3. 配置环境变量
创建或更新 `.env` 文件：
```env
VITE_API_BASE_URL=/api
```

### 4. 启动开发服务器
```bash
npm run dev
```

### 5. 构建生产版本
```bash
npm run build
```

## 验证更新

### 1. 检查 Dashboard
- [ ] 访问 http://localhost:5173
- [ ] 确认服务器状态显示正常
- [ ] 确认统计信息显示正常
- [ ] 测试刷新按钮
- [ ] 测试自动刷新开关

### 2. 检查 Sessions
- [ ] 访问 Sessions 页面
- [ ] 确认会话列表显示
- [ ] 测试断开单个会话
- [ ] 测试刷新功能
- [ ] 测试自动刷新

### 3. 检查 Logs
- [ ] 访问 Logs 页面
- [ ] 测试日志级别过滤
- [ ] 测试日志数量选择
- [ ] 测试刷新功能
- [ ] 测试清除日志（谨慎）

### 4. 检查 Tools
- [ ] 访问 Tools 页面
- [ ] 点击工具卡片
- [ ] 选择子命令
- [ ] 输入参数（JSON）
- [ ] 测试执行功能
- [ ] 查看执行结果

## API 端点测试

使用以下命令测试后端 API：

```bash
# 获取会话列表
curl http://localhost:5000/admin/sessions

# 获取服务器统计
curl http://localhost:5000/admin/stats

# 获取日志
curl "http://localhost:5000/admin/logs?count=50&level=Info"

# 断开会话（替换 {sessionId}）
curl -X DELETE http://localhost:5000/admin/sessions/{sessionId}

# 执行工具（需要会话 ID）
curl -X POST http://localhost:5000/admin/tools/vikunja_projects/list \
  -H "Content-Type: application/json" \
  -H "X-Session-Id: your-session-id" \
  -d '{}'
```

## 常见问题

### Q1: API 调用失败，返回 404
**A**: 确保后端服务器正在运行，并且 AdminController 已正确注册。

### Q2: 会话列表为空
**A**: 需要先通过 MCP 客户端创建会话，或者使用 Vikunja API 认证。

### Q3: 工具执行失败
**A**: 确保选择了有效的会话 ID，并且参数格式正确。

### Q4: 日志不显示
**A**: 检查后端日志目录是否存在，以及是否有日志文件。

### Q5: 自动刷新不工作
**A**: 检查浏览器控制台是否有错误，确保组件正确挂载。

## 回滚步骤

如果需要回滚到之前的版本：

```bash
# 1. 使用 Git 回滚
git checkout HEAD~1 -- src/vikunja-mcp-admin/

# 2. 重新安装依赖
cd src/vikunja-mcp-admin
npm install

# 3. 重启开发服务器
npm run dev
```

## 性能优化建议

1. **自动刷新间隔**
   - Dashboard: 建议 10-30 秒
   - Sessions: 建议 5-15 秒
   - Logs: 建议 5-10 秒

2. **日志数量**
   - 默认 100 条足够日常使用
   - 大量日志时使用级别过滤

3. **会话管理**
   - 定期清理过期会话
   - 避免创建过多会话

## 安全注意事项

1. **生产环境**
   - 添加身份验证
   - 限制 Admin API 访问
   - 使用 HTTPS

2. **操作权限**
   - 断开会话操作应谨慎
   - 清除日志会删除所有记录
   - 工具执行可能影响数据

3. **日志管理**
   - 定期备份重要日志
   - 设置日志轮转策略
   - 监控日志文件大小

## 技术支持

如有问题，请：
1. 查看 `ADMIN_FEATURES.md` 详细文档
2. 检查浏览器控制台错误
3. 查看后端日志
4. 提交 GitHub Issue

## 更新日志

### v2.0.0 (2024-01-XX)
- ✨ 新增完整的会话管理功能
- ✨ 新增服务器统计监控
- ✨ 新增日志管理功能
- ✨ 新增工具测试功能
- 🎨 改进 UI/UX 设计
- 🐛 修复已知问题
- 📝 完善文档

## 下一步计划

- [ ] 添加 WebSocket 实时更新
- [ ] 添加性能监控图表
- [ ] 添加用户权限管理
- [ ] 添加配置备份功能
- [ ] 添加告警通知系统
- [ ] 添加日志导出功能
- [ ] 添加工具执行历史
- [ ] 添加 API 文档集成

---

**祝使用愉快！** 🎉
