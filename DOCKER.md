# Docker 部署指南

本文档介绍如何使用 Docker 部署 Vikunja MCP 服务器（.NET 10 AOT 版本）。

## 特性

- ✅ .NET 10 AOT 编译 - 快速启动，低内存占用
- ✅ Alpine Linux 基础镜像 - 最小化镜像大小
- ✅ 非 root 用户运行 - 增强安全性
- ✅ 健康检查 - 自动监控服务状态
- ✅ 日志持久化 - 容器重启后日志不丢失

## 快速开始

### 1. 使用 Docker Compose（推荐）

```bash
# 构建并启动服务
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

### 2. 使用 Docker 命令

```bash
# 构建镜像
docker build -t vikunja-mcp:latest .

# 运行容器
docker run -d \
  --name vikunja-mcp-server \
  -p 5082:5082 \
  -v $(pwd)/logs:/app/logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  vikunja-mcp:latest

# 查看日志
docker logs -f vikunja-mcp-server

# 停止容器
docker stop vikunja-mcp-server

# 删除容器
docker rm vikunja-mcp-server
```

## 配置选项

### 环境变量

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ASPNETCORE_URLS` | `http://+:5082` | 监听地址 |
| `ASPNETCORE_ENVIRONMENT` | `Production` | 运行环境 |
| `Cors__AllowedOrigins__0` | `*` | CORS 允许的源 |
| `RateLimit__RequestsPerMinute` | `60` | 每分钟请求限制 |
| `Logging__LogLevel__Default` | `Information` | 默认日志级别 |

### 端口映射

- `5082`: HTTP API 端口

### 数据卷

- `/app/logs`: 日志文件目录（建议挂载到宿主机）

## 健康检查

容器内置健康检查，每 30 秒检查一次 `/health` 端点：

```bash
# 查看健康状态
docker inspect --format='{{.State.Health.Status}}' vikunja-mcp-server
```

## 镜像信息

### 构建信息

- **基础镜像**: `mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine`
- **编译方式**: AOT (Ahead-of-Time)
- **架构支持**: linux/amd64, linux/arm64

### 镜像大小

- 构建镜像: ~500MB (包含 SDK)
- 运行镜像: ~50-80MB (仅运行时依赖 + AOT 二进制)

## 生产部署建议

### 1. 使用特定的 CORS 配置

```yaml
environment:
  - Cors__AllowedOrigins__0=https://your-frontend.com
  - Cors__AllowedOrigins__1=https://admin.your-frontend.com
```

### 2. 配置反向代理

使用 Nginx 或 Traefik 作为反向代理：

```nginx
server {
    listen 80;
    server_name mcp.example.com;

    location / {
        proxy_pass http://localhost:5082;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. 资源限制

```yaml
services:
  vikunja-mcp:
    # ... 其他配置
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 256M
        reservations:
          cpus: '0.5'
          memory: 128M
```

### 4. 日志轮转

```yaml
services:
  vikunja-mcp:
    # ... 其他配置
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## 多阶段构建说明

Dockerfile 使用多阶段构建优化镜像大小：

1. **Build Stage**: 使用完整的 .NET 10 SDK 进行 AOT 编译
2. **Runtime Stage**: 仅包含运行时依赖和编译后的二进制文件

## 故障排查

### 容器无法启动

```bash
# 查看详细日志
docker logs vikunja-mcp-server

# 检查容器状态
docker ps -a | grep vikunja-mcp
```

### 健康检查失败

```bash
# 进入容器检查
docker exec -it vikunja-mcp-server sh

# 手动测试健康端点
wget -O- http://localhost:5082/health
```

### 权限问题

确保日志目录有正确的权限：

```bash
# 创建日志目录并设置权限
mkdir -p logs
chmod 755 logs
```

## 性能优化

### AOT 编译优势

- **启动时间**: ~50ms（相比 JIT 的 ~500ms）
- **内存占用**: ~30MB（相比 JIT 的 ~100MB）
- **CPU 使用**: 无 JIT 编译开销

### 容器优化

- 使用 Alpine Linux 减小镜像大小
- 启用符号剥离（StripSymbols）
- 启用单文件压缩（EnableCompressionInSingleFile）

## 安全建议

1. ✅ 使用非 root 用户运行（已配置）
2. ✅ 最小化基础镜像（Alpine）
3. ⚠️ 配置 HTTPS（需要反向代理）
4. ⚠️ 限制 CORS 源（生产环境）
5. ⚠️ 配置防火墙规则
6. ⚠️ 定期更新基础镜像

## 监控和日志

### 查看实时日志

```bash
# Docker Compose
docker-compose logs -f vikunja-mcp

# Docker
docker logs -f vikunja-mcp-server
```

### 日志文件位置

- 容器内: `/app/logs/vikunja-mcp-YYYY-MM-DD.log`
- 宿主机: `./logs/vikunja-mcp-YYYY-MM-DD.log`

## 更新和维护

### 更新镜像

```bash
# 重新构建
docker-compose build --no-cache

# 重启服务
docker-compose up -d
```

### 备份日志

```bash
# 压缩日志
tar -czf logs-backup-$(date +%Y%m%d).tar.gz logs/

# 清理旧日志
find logs/ -name "*.log" -mtime +30 -delete
```

## 示例：完整的生产配置

```yaml
version: '3.8'

services:
  vikunja-mcp:
    build:
      context: .
      dockerfile: Dockerfile
    image: vikunja-mcp:latest
    container_name: vikunja-mcp-server
    restart: unless-stopped
    ports:
      - "127.0.0.1:5082:5082"  # 仅本地访问
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5082
      - Cors__AllowedOrigins__0=https://app.example.com
      - RateLimit__RequestsPerMinute=100
      - Logging__LogLevel__Default=Warning
      - Logging__LogLevel__Microsoft.AspNetCore=Error
    volumes:
      - ./logs:/app/logs:rw
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 256M
        reservations:
          cpus: '0.5'
          memory: 128M
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:5082/health"]
      interval: 30s
      timeout: 3s
      start-period: 5s
      retries: 3
    networks:
      - vikunja-network

networks:
  vikunja-network:
    driver: bridge
```

## 支持

如有问题，请查看：
- [项目 README](README.md)
- [测试指南](TESTING.md)
- [更新日志](CHANGELOG.md)
