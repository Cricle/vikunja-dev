#!/bin/bash

# Vikunja MCP C# 服务器测试运行脚本
# 用法: ./run-tests.sh [VIKUNJA_URL] [VIKUNJA_TOKEN]

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}╔══════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Vikunja MCP C# 服务器测试运行器                            ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════════╝${NC}"
echo ""

# 检查参数或环境变量
if [ -n "$1" ]; then
    export VIKUNJA_URL="$1"
fi

if [ -n "$2" ]; then
    export VIKUNJA_TOKEN="$2"
fi

# 验证必需的环境变量
if [ -z "$VIKUNJA_URL" ]; then
    echo -e "${RED}错误: 未设置 VIKUNJA_URL${NC}"
    echo "用法: ./run-tests.sh [VIKUNJA_URL] [VIKUNJA_TOKEN]"
    echo "或设置环境变量: export VIKUNJA_URL=https://your-vikunja.com/api/v1"
    exit 1
fi

if [ -z "$VIKUNJA_TOKEN" ]; then
    echo -e "${RED}错误: 未设置 VIKUNJA_TOKEN${NC}"
    echo "用法: ./run-tests.sh [VIKUNJA_URL] [VIKUNJA_TOKEN]"
    echo "或设置环境变量: export VIKUNJA_TOKEN=tk_your_token"
    exit 1
fi

echo -e "${GREEN}✓ 环境变量已设置${NC}"
echo -e "  VIKUNJA_URL: ${VIKUNJA_URL}"
echo -e "  VIKUNJA_TOKEN: ${VIKUNJA_TOKEN:0:10}...${NC}"
echo ""

# 检查 .NET 是否安装
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}错误: 未找到 dotnet 命令${NC}"
    echo "请安装 .NET SDK: https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "${GREEN}✓ .NET SDK 已安装${NC}"
dotnet --version
echo ""

# 检查 Node.js 是否安装
if ! command -v node &> /dev/null; then
    echo -e "${RED}错误: 未找到 node 命令${NC}"
    echo "请安装 Node.js: https://nodejs.org/"
    exit 1
fi

echo -e "${GREEN}✓ Node.js 已安装${NC}"
node --version
echo ""

# 构建项目
echo -e "${YELLOW}正在构建项目...${NC}"
cd src/VikunjaHook
dotnet build VikunjaHook.sln --configuration Release
if [ $? -ne 0 ]; then
    echo -e "${RED}✗ 构建失败${NC}"
    exit 1
fi
echo -e "${GREEN}✓ 构建成功${NC}"
echo ""

# 启动服务器
echo -e "${YELLOW}正在启动 MCP 服务器...${NC}"
cd VikunjaHook
dotnet run --configuration Release &
SERVER_PID=$!
cd ../../..

# 等待服务器启动
echo -e "${YELLOW}等待服务器就绪...${NC}"
for i in {1..30}; do
    if curl -s http://localhost:5082/health > /dev/null 2>&1; then
        echo -e "${GREEN}✓ 服务器已就绪${NC}"
        break
    fi
    if [ $i -eq 30 ]; then
        echo -e "${RED}✗ 服务器启动超时${NC}"
        kill $SERVER_PID 2>/dev/null || true
        exit 1
    fi
    sleep 1
done
echo ""

# 运行测试
echo -e "${YELLOW}正在运行测试...${NC}"
echo ""
node test-complete.js
TEST_EXIT_CODE=$?

# 停止服务器
echo ""
echo -e "${YELLOW}正在停止服务器...${NC}"
kill $SERVER_PID 2>/dev/null || true
wait $SERVER_PID 2>/dev/null || true
echo -e "${GREEN}✓ 服务器已停止${NC}"

# 输出结果
echo ""
if [ $TEST_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║  🎉 所有测试通过！                                          ║${NC}"
    echo -e "${GREEN}╚══════════════════════════════════════════════════════════════╝${NC}"
    exit 0
else
    echo -e "${RED}╔══════════════════════════════════════════════════════════════╗${NC}"
    echo -e "${RED}║  ❌ 测试失败                                                 ║${NC}"
    echo -e "${RED}╚══════════════════════════════════════════════════════════════╝${NC}"
    exit 1
fi
