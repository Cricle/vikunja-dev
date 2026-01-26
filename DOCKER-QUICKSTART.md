# Docker 快速开始

## 最快速的方式

```bash
# 一键启动
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止服务
docker-compose down
```

## 常用命令

### 构建镜像

```bash
# Linux/macOS
./docker-build.sh

# Windows
.\docker-build.ps1

# 或直接使用 Docker
docker build -t vikunja-mcp:latest .
```

### 运行容器

```bash
# 使用 Docker Compose（推荐）
docker-compose up -d

# 使用 Docker 命令
docker run -d \
  --name vikunja-mcp-server \
  -p 5082:5082 \
  -v $(pwd)/logs:/app/logs \
  vikunja-mcp:latest
```

### 管理容器

```bash
# 查看运行状态
docker ps

# 查看日志
docker logs -f vikunja-mcp-server

# 停止容器
docker stop vikunja-mcp-server

# 启动容器
docker start vikunja-mcp-server

# 重启容器
docker restart vikunja-mcp-server

# 删除容器
docker rm -f vikunja-mcp-server
```

### 测试健康状态

```bash
# 检查健康状态
docker inspect --format='{{.State.Health.Status}}' vikunja-mcp-server

# 手动测试健康端点
curl http://localhost:5082/health
```

## 环境变量配置

在 `docker-compose.yml` 中修改：

```yaml
environment:
  # CORS 配置
  - Cors__AllowedOrigins__0=https://your-app.com
  
  # 速率限制
  - RateLimit__RequestsPerMinute=100
  
  # 日志级别
  - Logging__LogLevel__Default=Information
```

## 镜像信息

- **基础镜像**: Alpine Linux
- **大小**: ~50-80MB
- **启动时间**: ~50ms
- **内存占用**: ~30MB
- **编译方式**: .NET 10 AOT

## 性能对比

| 指标 | AOT (Docker) | JIT (传统) |
|------|--------------|------------|
| 镜像大小 | ~60MB | ~200MB |
| 启动时间 | ~50ms | ~500ms |
| 内存占用 | ~30MB | ~100MB |
| CPU 使用 | 低 | 中等 |

## 故障排查

### 容器无法启动

```bash
# 查看详细日志
docker logs vikunja-mcp-server

# 检查端口占用
netstat -ano | findstr :5082  # Windows
lsof -i :5082                 # Linux/macOS
```

### 健康检查失败

```bash
# 进入容器
docker exec -it vikunja-mcp-server sh

# 测试健康端点
wget -O- http://localhost:5082/health
```

### 权限问题

```bash
# 确保日志目录权限正确
mkdir -p logs
chmod 755 logs
```

## 生产部署

### 使用反向代理

```nginx
# Nginx 配置示例
server {
    listen 80;
    server_name mcp.example.com;

    location / {
        proxy_pass http://localhost:5082;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 资源限制

```yaml
# docker-compose.yml
services:
  vikunja-mcp:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 256M
        reservations:
          cpus: '0.5'
          memory: 128M
```

### 日志轮转

```yaml
# docker-compose.yml
services:
  vikunja-mcp:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

## 更多信息

- [完整 Docker 文档](DOCKER.md)
- [项目 README](README.md)
- [测试指南](TESTING.md)
