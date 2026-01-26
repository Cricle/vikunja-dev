# 更新日志

本文档记录项目的所有重要更改。

## [未发布] - 2026-01-26

### 新增 ✨

#### Docker 镜像大幅优化 🚀
- **镜像大小从 418MB 降低到 84.6MB**（减少 79.8%）
  - 使用 Alpine Linux 3.21 基础镜像（12MB vs Debian 180MB）
  - 使用 musl libc 替代 glibc
  - .NET 10 AOT 编译针对 linux-musl-x64
  - UPX 压缩二进制文件（23MB → 7.5MB，67% 压缩率）
  - 多阶段构建优化
- 添加 `DOCKER_OPTIMIZATION.md` 详细文档
  - 优化历程和技术详解
  - 性能影响分析
  - 镜像组成分析
  - 进一步优化建议

#### Docker 支持
- 添加 Dockerfile（.NET 10 AOT 编译）
  - 多阶段构建优化镜像大小
  - Alpine Linux 基础镜像（84.6MB）
  - 前端和后端集成构建
  - 非 root 用户运行
  - 内置健康检查
- 添加 docker-compose.yml 配置
  - 环境变量配置
  - 日志持久化
  - 网络隔离
- 添加 .dockerignore 文件
- 添加 Docker 构建脚本
  - `docker-build.sh` (Linux/macOS)
  - `docker-build.ps1` (Windows)
- Docker 支持（.NET 10 AOT 编译）
  - 多阶段构建优化
  - Alpine Linux 基础镜像
  - 镜像大小 84.6MB
  - 启动时间 ~1-2 秒（包含 UPX 解压）
  - 多标签支持（latest, version, sha）

#### AOT 优化
- 在 csproj 中添加 AOT 优化设置
  - `StripSymbols=true` - 剥离调试符号
  - `EnableCompressionInSingleFile=true` - 单文件压缩
  - `IlcOptimizationPreference=Speed` - 优化速度
  - `IlcGenerateStackTraceData=false` - 减小体积

### 修复 🐛

#### AOT 兼容性
- 修复所有 AOT 编译警告（从 15 个减少到 0 个）
  - 创建 `SimpleResponse.cs` 包含所有强类型响应类
  - 创建 `ToolResponses.cs` 包含 17 个工具响应类
  - 替换所有匿名类型为强类型（43 处）
  - 在 `AppJsonSerializerContext` 中注册所有类型
  - 修复 `ConfigurationController` 使用 `Utf8JsonWriter` 手动构建 JSON
  - 使用 `UnconditionalSuppressMessage` 抑制 `AddControllers` 警告
- 构建成功：0 错误, 0 警告
- 所有测试通过：28/28 (100%)

### 新增 ✨

#### CI/CD 基础设施
- 添加 GitHub Actions CI 配置 (`.github/workflows/ci.yml`)
  - 自动化构建和测试
  - 多平台构建检查（Ubuntu, Windows, macOS）
  - 测试日志上传
- 添加 Linux/macOS 测试脚本 (`run-tests.sh`)
  - 彩色输出
  - 自动构建和服务器管理
  - 健康检查等待
- 添加 Windows PowerShell 测试脚本 (`run-tests.ps1`)
  - 参数支持
  - 自动构建和服务器管理
  - 健康检查等待

#### 文档
- 添加 GitHub Secrets 设置指南 (`.github/SETUP_SECRETS.md`)
- 添加完整测试指南 (`TESTING.md`)
- 添加 CI 配置完成报告 (`CI_SETUP_COMPLETE.md`)
- 添加项目根目录 README (`README.md`)
- 添加更新日志 (`CHANGELOG.md`)

#### 测试改进
- 测试脚本支持环境变量配置
  - `VIKUNJA_URL`: Vikunja API URL
  - `VIKUNJA_TOKEN`: Vikunja API Token
  - `API_BASE`: MCP 服务器地址
- 环境变量验证和错误提示

### 修改 🔧

#### 测试脚本
- `test-complete.js`: 从硬编码配置改为环境变量
  - 支持 `process.env.VIKUNJA_URL`
  - 支持 `process.env.VIKUNJA_TOKEN`
  - 支持 `process.env.API_BASE`
  - 添加环境变量验证

#### 文档更新
- `src/VikunjaHook/README.md`: 添加 CI 和测试说明
  - 添加 CI 状态徽章
  - 添加测试运行说明
  - 添加 CI 配置说明

#### 安全配置
- `.gitignore`: 添加测试和 CI 相关忽略项
  - `*.pid`, `server.pid`
  - `.env.local`, `.env.test`, `.env.production`
  - `test-results/`, `test-output/`, `*.test.log`

### 修复 🐛

#### MCP 服务器
- 修复项目更新 API 方法（从 PUT 改为 POST）
  - `ProjectsTool.UpdateProjectAsync` 现在使用正确的 HTTP 方法
- 修复测试脚本参数名称
  - `projectId` → `id`
  - `labelId` → `id`
  - `teamId` → `id`
- 消除匿名类型
  - 创建 `ApplyLabelRequest` 类
  - 在 `AppJsonSerializerContext` 中注册

### 测试结果 ✅

- **测试通过率**: 100% (28/28)
- **基础功能测试**: 8/8 通过
- **Tasks 工具测试**: 22/22 通过

## [1.0.0] - 2026-01-26

### 初始发布 🎉

#### 核心功能
- Vikunja MCP C# 服务器
  - 5 个工具，45+ 子命令
  - Tasks 工具（22 个子命令）
  - Projects 工具（11 个子命令）
  - Labels、Teams、Users 工具
  - Native AOT 编译支持
  - HTTP/RESTful API
  - 双重认证（API Token 和 JWT）
  - 弹性 HTTP 客户端

#### 测试套件
- 完整的测试套件（28 项测试）
  - 基础功能测试
  - Tasks 工具完整测试
  - 自动化清理

#### 文档
- MCP 服务器文档
- 实现总结
- 完成报告

---

## 版本说明

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
本项目遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

### 变更类型

- `新增` - 新功能
- `修改` - 现有功能的变更
- `弃用` - 即将移除的功能
- `移除` - 已移除的功能
- `修复` - Bug 修复
- `安全` - 安全相关的修复
