# Vikunja MCP Server

[![CI - Build and Test](https://github.com/Cricle/vikunja-dev/actions/workflows/ci.yml/badge.svg)](https://github.com/Cricle/vikunja-dev/actions/workflows/ci.yml)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Docker Image](https://img.shields.io/badge/Docker-28MB-2496ED)](https://hub.docker.com/)

一个用 C# 和 .NET 10 AOT 构建的高性能 Model Context Protocol (MCP) 服务器，用于 Vikunja 任务管理系统。

## ✨ 特性

- 🚀 **极致性能**: .NET 10 Native AOT 编译，启动时间 < 2 秒
- 📦 **超小镜像**: Docker 镜像仅 28MB（从 418MB 优化 93.3%）
- 🛠️ **完整工具集**: 5 个工具，45+ 子命令
- 🔐 **双重认证**: 支持 API Token 和 JWT
- 🎯 **Minimal API**: 轻量级架构，无冗余依赖
- 🐳 **生产就绪**: 遵循微软官方最佳实践

## 📊 工具列表

| 工具 | 子命令数 | 功能 |
|------|---------|------|
| **tasks** | 22 | 完整的任务管理（CRUD、批量操作、分配、评论、标签、提醒、关系） |
| **projects** | 11 | 项目管理和层级操作 |
| **labels** | 5 | 标签管理 |
| **teams** | 3 | 团队管理 |
| **users** | 4 | 用户管理 |

## 🚀 快速开始

### 使用 Docker（推荐）

```bash
# 拉取镜像
docker pull ghcr.io/cricle/vikunja-mcp-server:latest

# 运行
docker run -d -p 5082:5082 \
  -e VIKUNJA_API_URL=https://your-vikunja.com/api/v1 \
  -e VIKUNJA_API_TOKEN=your-token \
  ghcr.io/cricle/vikunja-mcp-server:latest
```

### 使用 Docker Compose

```yaml
version: '3.8'
services:
  vikunja-mcp:
    image: ghcr.io/cricle/vikunja-mcp-server:latest
    ports:
      - "5082:5082"
    environment:
      - VIKUNJA_API_URL=https://your-vikunja.com/api/v1
      - VIKUNJA_API_TOKEN=your-token
    restart: unless-stopped
```

### 本地运行

```bash
# 克隆仓库
git clone https://github.com/Cricle/vikunja-dev.git
cd vikunja-dev

# 运行
cd src/VikunjaHook/VikunjaHook
dotnet run
```

服务器将在 `http://localhost:5082` 启动。

## 📖 API 使用

### 认证

```bash
curl -X POST http://localhost:5082/mcp/auth \
  -H "Content-Type: application/json" \
  -d '{
    "apiUrl": "https://your-vikunja.com/api/v1",
    "apiToken": "your-api-token"
  }'
```

响应：
```json
{
  "sessionId": "abc123...",
  "authType": "ApiToken"
}
```

### 创建任务

```bash
curl -X POST http://localhost:5082/mcp/tools/tasks/create \
  -H "Authorization: Bearer <session-id>" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "title": "My new task",
    "description": "Task description",
    "priority": 3
  }'
```

### 列出任务

```bash
curl -X POST http://localhost:5082/mcp/tools/tasks/list \
  -H "Authorization: Bearer <session-id>" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "page": 1,
    "perPage": 50
  }'
```

### 获取工具列表

```bash
curl http://localhost:5082/mcp/tools
```

### 健康检查

```bash
curl http://localhost:5082/health
```

## 🐳 Docker 镜像优化

我们的 Docker 镜像经过极致优化：

| 指标 | 初始版本 | 最终版本 | 改进 |
|------|----------|----------|------|
| 镜像大小 | 418MB | **28MB** | **-93.3%** |
| 二进制大小 | 81MB | **5.1MB** | **-93.7%** |
| 基础镜像 | Debian 180MB | Alpine 23MB | -87.2% |
| 依赖库 | 7 个 | **1 个** | -85.7% |

### 优化技术

- ✅ 使用官方 `sdk:10.0-alpine-aot`（预装 AOT 工具）
- ✅ 使用 `runtime-deps:10.0-alpine`（包含所有运行时依赖）
- ✅ UPX 压缩二进制（15MB → 5.1MB）
- ✅ BuildKit 缓存加速构建
- ✅ 最小化依赖（仅保留 AI 抽象库）
- ✅ 非 root 用户运行

详见 [Docker 优化文档](DOCKER_SIZE_REDUCTION_SUMMARY.md)

## 🏗️ 架构

```
vikunja-mcp-server/
├── Minimal API          # 轻量级 HTTP 端点
├── MCP Server           # Model Context Protocol 实现
├── Tools                # 5 个工具，45+ 子命令
│   ├── TasksTool       # 任务管理
│   ├── ProjectsTool    # 项目管理
│   ├── LabelsTool      # 标签管理
│   ├── TeamsTool       # 团队管理
│   └── UsersTool       # 用户管理
├── Services             # 核心服务
│   ├── AuthenticationManager
│   ├── VikunjaClientFactory
│   └── ToolRegistry
└── Webhook Handler      # Vikunja Webhook 处理
```

## 🧪 测试

### 运行测试

```bash
# Linux/macOS
./run-tests.sh "https://your-vikunja.com/api/v1" "your-token"

# Windows
.\run-tests.ps1 -VikunjaUrl "https://your-vikunja.com/api/v1" -VikunjaToken "your-token"
```

### 测试覆盖

- ✅ 基础功能测试 (8 项)
- ✅ Tasks 工具测试 (22 项)
- ✅ 批量操作测试
- ✅ 任务关系测试
- ✅ 评论和标签测试

**当前测试通过率: 100% (28/28)**

## 🔧 开发

### 前置要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (可选)

### 构建

```bash
cd src/VikunjaHook
dotnet build
```

### 发布

```bash
# Windows x64
dotnet publish -c Release -r win-x64

# Linux x64
dotnet publish -c Release -r linux-x64

# macOS ARM64
dotnet publish -c Release -r osx-arm64
```

### Docker 构建

```bash
docker build -t vikunja-mcp-server:latest .
```

## 📊 性能指标

- **启动时间**: ~1-2 秒（包含 UPX 解压）
- **内存占用**: ~30-50MB
- **请求延迟**: <100ms（本地网络）
- **镜像大小**: 28MB
- **二进制大小**: 5.1MB

## 🛠️ 技术栈

- **.NET 10**: Native AOT 编译
- **Alpine Linux**: 轻量级基础镜像
- **Minimal API**: 无 Controllers 开销
- **UPX**: 二进制压缩
- **BuildKit**: Docker 构建缓存
- **Microsoft.Extensions.AI.Abstractions**: 唯一的第三方依赖

## 📝 端点列表

### MCP 端点
- `POST /mcp/auth` - 认证
- `POST /mcp/request` - MCP 请求
- `GET /mcp/info` - 服务器信息
- `GET /mcp/tools` - 工具列表
- `POST /mcp/tools/{tool}/{subcommand}` - 执行工具
- `GET /mcp/health` - MCP 健康检查

### Webhook 端点
- `POST /webhook/vikunja` - Vikunja Webhook
- `GET /webhook/vikunja/events` - 支持的事件列表

### Admin 端点
- `GET /admin/sessions` - 会话列表
- `DELETE /admin/sessions/{id}` - 断开会话
- `DELETE /admin/sessions` - 断开所有会话
- `GET /admin/stats` - 服务器统计
- `POST /admin/tools/{tool}/{subcommand}` - 测试工具

### 通用端点
- `GET /health` - 健康检查

## 📚 文档

- [Docker 优化详解](DOCKER_SIZE_REDUCTION_SUMMARY.md)
- [Docker 优化技术](DOCKER_OPTIMIZATION.md)
- [更新日志](CHANGELOG.md)
- [MCP 服务器文档](src/VikunjaHook/README.md)
- [Webhook 处理指南](src/VikunjaHook/WEBHOOK_HANDLER_GUIDE.md)

## 🤝 贡献

欢迎贡献！请随时提交 Pull Request。

### 贡献流程

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

## 📄 许可证

MIT License

## 🔗 相关链接

- [Vikunja 官网](https://vikunja.io/)
- [Vikunja API 文档](https://vikunja.io/docs/api-tokens/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [.NET 文档](https://docs.microsoft.com/en-us/dotnet/)

## 💬 支持

如有问题或建议，请：
- 提交 [Issue](https://github.com/Cricle/vikunja-dev/issues)
- 查看 [文档](DOCKER_SIZE_REDUCTION_SUMMARY.md)

---

**Made with ❤️ using .NET 10 AOT**
