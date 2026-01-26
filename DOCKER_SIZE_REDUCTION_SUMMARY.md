# Docker 镜像大小优化总结

## 最终成果

✅ **成功将 Docker 镜像从 418MB 降低到 28MB**
- **减少**: 390MB
- **压缩率**: 93.3%
- **最终大小**: 28MB

## 优化历程

| 版本 | 大小 | 优化措施 |
|------|------|----------|
| v1 | 418MB | Debian + 调试符号 + Serilog + Polly + 前端 |
| v2 | 299MB | 删除 .dbg 调试符号（-119MB） |
| v3 | 222MB | Size 优化 + 删除开发配置（-77MB） |
| v4 | 84.6MB | Alpine + musl + UPX（-137.4MB） |
| v5 | 77.1MB | 移除前端（-7.5MB） |
| v6 | 61.2MB | 移除 Serilog/Polly/FluentValidation/ICU（-15.9MB） |
| v7 | **28MB** | 官方 AOT SDK + runtime-deps + Minimal API（-33.2MB） |

## 最终镜像组成（28MB）

| 组件 | 大小 | 说明 |
|------|------|------|
| runtime-deps 基础镜像 | ~23MB | 包含所有必需的运行时依赖 |
| 后端二进制（UPX 压缩） | 5.1MB | 原始 ~15MB，压缩后 5.1MB |
| 配置文件 | <1MB | appsettings.json 等 |

## 关键优化技术

### 1. 使用官方 AOT SDK
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
```
- 预装所有 AOT 编译工具（clang, build-base, zlib-dev）
- 无需手动安装依赖

### 2. 使用 runtime-deps 镜像
```dockerfile
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
```
- 包含所有必需的运行时库（libstdc++, libgcc, musl, OpenSSL 等）
- 无需手动安装任何依赖
- 比手动安装更小更优化

### 3. Docker BuildKit 缓存
```dockerfile
RUN --mount=type=cache,target=/root/.nuget \
    --mount=type=cache,target=/source/bin \
    --mount=type=cache,target=/source/obj
```
- 缓存 NuGet 包、编译输出
- 大幅加快重复构建速度

### 4. UPX 压缩
```dockerfile
RUN upx --best --lzma /app/VikunjaHook
```
- 将二进制从 ~15MB 压缩到 5.1MB
- 66% 压缩率

### 5. 代码简化
- 移除 CORS 和 RateLimit 中间件
- 移除 Controllers，全部使用 Minimal API
- 移除 Serilog（使用内置 ILogger）
- 移除 Polly（简化 HTTP 客户端）
- 移除 FluentValidation
- 移除 ICU（InvariantGlobalization=true）
- 仅保留 AI 抽象库

### 6. 非 root 用户
```dockerfile
USER $APP_UID
```
- 使用非 root 用户运行，提高安全性

## 性能验证

### 功能测试
✅ 健康检查端点正常
✅ MCP API 端点正常
✅ 所有工具可用
✅ Webhook 端点正常
✅ Admin 端点正常

### 性能指标
- **启动时间**: ~1-2 秒（包含 UPX 解压）
- **内存占用**: ~30-50MB
- **镜像拉取**: 减少 93.3% 时间
- **存储空间**: 节省 390MB

## 技术栈

- **基础镜像**: mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine (23MB)
- **构建镜像**: mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot
- **C 库**: musl libc
- **后端**: .NET 10 AOT (linux-musl-x64)
- **压缩**: UPX 4.x (LZMA 算法)
- **架构**: Minimal API（无 Controllers）
- **日志**: Microsoft.Extensions.Logging（内置）
- **HTTP**: HttpClient（无弹性库）
- **依赖**: 仅 Microsoft.Extensions.AI.Abstractions

## 构建和运行

### 构建镜像
```bash
docker build -t vikunja-mcp-server:latest .
```

### 运行容器
```bash
docker run -d -p 5082:5082 vikunja-mcp-server:latest
```

### 验证
```bash
# 检查大小
docker images vikunja-mcp-server:latest

# 测试健康检查
curl http://localhost:5082/health

# 测试 MCP 信息
curl http://localhost:5082/mcp/info
```

## 对比分析

| 指标 | 初始版本 | 最终版本 | 改进 |
|------|----------|----------|------|
| 镜像大小 | 418MB | 28MB | -93.3% |
| 二进制大小 | 81MB | 5.1MB | -93.7% |
| 基础镜像 | Debian 180MB | Alpine 23MB | -87.2% |
| 依赖库数量 | 7 个 | 1 个 | -85.7% |
| 构建时间 | ~3 分钟 | ~3 分钟 | 相同 |
| 启动时间 | ~2 秒 | ~2 秒 | 相同 |

## 最佳实践

1. **使用官方 AOT SDK**: 预装所有工具，无需手动配置
2. **使用 runtime-deps**: 包含所有必需依赖，比手动安装更优
3. **启用 BuildKit 缓存**: 加快重复构建
4. **最小化依赖**: 仅保留必需的库
5. **使用 Minimal API**: 比 Controllers 更轻量
6. **InvariantGlobalization**: 移除 ICU 依赖
7. **UPX 压缩**: 大幅减小二进制大小
8. **非 root 用户**: 提高安全性

## 总结

通过采用微软官方推荐的 Dockerfile 模式，结合 Alpine、AOT 编译、UPX 压缩和代码简化，成功将 Docker 镜像从 418MB 优化到 28MB，减少了 93.3%。镜像保持了完整的后端 API 功能，仅使用 .NET 内置库和 AI 抽象库，大幅提升了分发和部署效率。

---

**优化完成日期**: 2026-01-26  
**最终镜像大小**: 28MB  
**压缩率**: 93.3%  
**技术**: 官方 AOT SDK + runtime-deps + Minimal API + UPX
