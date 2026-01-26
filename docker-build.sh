#!/bin/bash
# Docker 构建脚本 - Linux/macOS

set -e

echo "🐳 开始构建 Vikunja MCP Docker 镜像..."
echo ""

# 构建镜像
docker build -t vikunja-mcp:latest .

echo ""
echo "✅ 镜像构建完成！"
echo ""
echo "镜像信息:"
docker images vikunja-mcp:latest

echo ""
echo "下一步:"
echo "  运行容器: docker-compose up -d"
echo "  或者:     docker run -d -p 5082:5082 --name vikunja-mcp-server vikunja-mcp:latest"
