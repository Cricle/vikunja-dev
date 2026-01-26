# Docker 构建脚本 - Windows PowerShell

Write-Host "🐳 开始构建 Vikunja MCP Docker 镜像..." -ForegroundColor Cyan
Write-Host ""

# 构建镜像
docker build -t vikunja-mcp:latest .

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ 镜像构建完成！" -ForegroundColor Green
    Write-Host ""
    Write-Host "镜像信息:" -ForegroundColor Cyan
    docker images vikunja-mcp:latest
    
    Write-Host ""
    Write-Host "下一步:" -ForegroundColor Yellow
    Write-Host "  运行容器: docker-compose up -d"
    Write-Host "  或者:     docker run -d -p 5082:5082 --name vikunja-mcp-server vikunja-mcp:latest"
} else {
    Write-Host ""
    Write-Host "❌ 镜像构建失败！" -ForegroundColor Red
    exit 1
}
