#!/bin/bash
# Docker 测试脚本

set -e

echo "🐳 测试 Docker 构建和运行..."
echo ""

# 清理旧容器和镜像
echo "清理旧容器..."
docker rm -f vikunja-mcp-test 2>/dev/null || true

# 构建镜像
echo ""
echo "构建 Docker 镜像..."
docker build -t vikunja-mcp:test .

# 运行容器
echo ""
echo "启动容器..."
docker run -d \
  --name vikunja-mcp-test \
  -p 5083:5082 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  vikunja-mcp:test

# 等待容器启动
echo ""
echo "等待服务启动..."
sleep 5

# 测试健康检查
echo ""
echo "测试健康检查端点..."
for i in {1..10}; do
  if curl -f http://localhost:5083/health > /dev/null 2>&1; then
    echo "✅ 健康检查通过！"
    break
  fi
  if [ $i -eq 10 ]; then
    echo "❌ 健康检查失败！"
    docker logs vikunja-mcp-test
    docker rm -f vikunja-mcp-test
    exit 1
  fi
  echo "等待中... ($i/10)"
  sleep 2
done

# 查看容器信息
echo ""
echo "容器信息:"
docker ps | grep vikunja-mcp-test

echo ""
echo "镜像大小:"
docker images vikunja-mcp:test

# 清理
echo ""
echo "清理测试容器..."
docker rm -f vikunja-mcp-test

echo ""
echo "✅ Docker 测试完成！"
