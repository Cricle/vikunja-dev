# Vikunja Development Tools

[![CI - Build and Test](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Node.js Version](https://img.shields.io/badge/Node.js-20.x-339933)](https://nodejs.org/)

这个仓库包含 Vikunja 任务管理系统的开发工具和扩展。

## 项目组成

### 1. Vikunja MCP C# Server
**路径**: `src/VikunjaHook/`

一个用 C# 和 ASP.NET Core 构建的 Model Context Protocol (MCP) 服务器，支持 Native AOT 编译。

**特性**:
- ✅ 5 个工具，45+ 子命令
- ✅ Tasks 工具（22 个子命令）：完整的任务管理
- ✅ Projects 工具（11 个子命令）：项目管理和层级操作
- ✅ Labels、Teams、Users 工具
- ✅ Native AOT 编译支持
- ✅ HTTP/RESTful API
- ✅ 双重认证（API Token 和 JWT）
- ✅ 弹性 HTTP 客户端（Polly 重试和熔断）

[查看详细文档 →](src/VikunjaHook/README.md)

### 2. Vikunja MCP Admin
**路径**: `src/vikunja-mcp-admin/`

一个用 Vue 3 + TypeScript 构建的 Web 管理界面。

[查看详细文档 →](src/vikunja-mcp-admin/README.md)

## 快速开始

### 前置要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20.x](https://nodejs.org/)
- 一个运行中的 Vikunja 实例

### 部署方式

#### 方式 1: Docker 部署（推荐）

使用 Docker 快速部署，支持 .NET 10 AOT 编译：

```bash
# 使用 Docker Compose
docker-compose up -d

# 或使用 Docker 命令
docker build -t vikunja-mcp:latest .
docker run -d -p 5082:5082 --name vikunja-mcp-server vikunja-mcp:latest
```

#### 方式 2: 本地运行

```bash
cd src/VikunjaHook/VikunjaHook
dotnet run
```

服务器将在 `http://localhost:5082` 启动。

### 运行测试

#### 使用自动化脚本（推荐）

**Linux/macOS:**
```bash
chmod +x run-tests.sh
./run-tests.sh "https://your-vikunja.com/api/v1" "tk_your_token"
```

**Windows PowerShell:**
```powershell
.\run-tests.ps1 -VikunjaUrl "https://your-vikunja.com/api/v1" -VikunjaToken "tk_your_token"
```

#### 使用环境变量

```bash
# Linux/macOS
export VIKUNJA_URL="https://your-vikunja.com/api/v1"
export VIKUNJA_TOKEN="tk_your_token"
./run-tests.sh

# Windows PowerShell
$env:VIKUNJA_URL="https://your-vikunja.com/api/v1"
$env:VIKUNJA_TOKEN="tk_your_token"
.\run-tests.ps1
```

[查看完整测试指南 →](TESTING.md)

## 测试覆盖

完整测试套件包含 **28 项测试**，覆盖所有核心功能：

- ✅ 基础功能测试 (8 项)
- ✅ Tasks 工具测试 (22 项)
  - 任务 CRUD 操作
  - 批量操作
  - 任务分配
  - 评论功能
  - 标签功能
  - 提醒功能
  - 任务关系

**当前测试通过率: 100% (28/28)**

## 持续集成

项目配置了 GitHub Actions CI，会在每次推送和 PR 时自动运行：

- ✅ 多平台构建检查 (Ubuntu, Windows, macOS)
- ✅ 完整的测试套件
- ✅ 构建警告检查

### 设置 CI

在 GitHub 仓库中配置以下 secrets：
- `VIKUNJA_URL`: Vikunja API URL（包含 `/api/v1`）
- `VIKUNJA_TOKEN`: Vikunja API Token

[查看 Secrets 设置指南 →](.github/SETUP_SECRETS.md)

## 项目结构

```
vikunja-dev/
├── .github/
│   ├── workflows/
│   │   └── ci.yml                    # GitHub Actions CI 配置
│   └── SETUP_SECRETS.md              # Secrets 设置指南
├── src/
│   ├── VikunjaHook/                  # MCP C# 服务器
│   │   ├── VikunjaHook/              # 主项目
│   │   │   ├── Mcp/                  # MCP 实现
│   │   │   │   ├── Models/           # 数据模型
│   │   │   │   ├── Services/         # 核心服务
│   │   │   │   └── Tools/            # MCP 工具
│   │   │   └── Program.cs            # 入口点
│   │   └── VikunjaHook.sln           # 解决方案文件
│   └── vikunja-mcp-admin/            # Web 管理界面
├── docker-compose.yml                # Docker Compose 配置
├── Dockerfile                        # Docker 镜像构建
├── run-tests.sh                      # Linux/macOS 测试脚本
├── run-tests.ps1                     # Windows 测试脚本
├── test-complete.js                  # 完整测试套件
├── CHANGELOG.md                      # 更新日志
└── README.md                         # 本文档
```

## API 使用示例

### 认证

```bash
curl -X POST http://localhost:5082/mcp/auth \
  -H "Content-Type: application/json" \
  -d '{
    "apiUrl": "https://your-vikunja.com/api/v1",
    "apiToken": "your-api-token"
  }'
```

### 创建任务

```bash
curl -X POST http://localhost:5082/mcp/tools/tasks/create \
  -H "Authorization: Bearer your-session-id" \
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
  -H "Authorization: Bearer your-session-id" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "page": 1,
    "perPage": 50
  }'
```

[查看更多 API 示例 →](src/VikunjaHook/README.md#usage-examples)

## 开发

### 构建项目

```bash
# MCP 服务器
cd src/VikunjaHook
dotnet build

# Web 管理界面
cd src/vikunja-mcp-admin
npm install
npm run dev
```

### 发布生产版本

```bash
# Windows x64
dotnet publish -c Release -r win-x64

# Linux x64
dotnet publish -c Release -r linux-x64

# macOS ARM64
dotnet publish -c Release -r osx-arm64
```

## 性能

- **启动时间**: ~50ms (Native AOT)
- **内存占用**: ~30-50MB
- **测试执行**: ~30-45 秒（28 项测试）
- **请求延迟**: <100ms (本地网络)

## 文档

- [MCP 服务器文档](src/VikunjaHook/README.md)
- [测试指南](TESTING.md)
- [CI 配置报告](CI_SETUP_COMPLETE.md)
- [Secrets 设置指南](.github/SETUP_SECRETS.md)
- [实现总结](src/VikunjaHook/IMPLEMENTATION_SUMMARY.md)
- [完成报告](FINAL_COMPLETION_REPORT.md)

## 贡献

欢迎贡献！请随时提交 Pull Request。

### 贡献流程

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 代码规范

- 遵循 C# 编码规范
- 添加适当的注释
- 确保所有测试通过
- 更新相关文档

## 许可证

[Your License Here]

## 相关链接

- [Vikunja 官网](https://vikunja.io/)
- [Vikunja API 文档](https://vikunja.io/docs/api-tokens/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [.NET 文档](https://docs.microsoft.com/en-us/dotnet/)

## 支持

如有问题或建议，请：
- 提交 [Issue](https://github.com/YOUR_USERNAME/YOUR_REPO/issues)
- 查看 [文档](TESTING.md)
- 参考 [故障排查指南](TESTING.md#故障排查)

---

**注意**: 请将 `YOUR_USERNAME/YOUR_REPO` 替换为你的实际 GitHub 用户名和仓库名。
