# Docker 测试脚本 - Windows PowerShell

Write-Host "🐳 测试 Docker 构建和运行..." -ForegroundColor Cyan
Write-Host ""

# 清理旧容器和镜像
Write-Host "清理旧容器..." -ForegroundColor Yellow
docker rm -f vikunja-mcp-test 2>$null

# 构建镜像
Write-Host ""
Write-Host "构建 Docker 镜像..." -ForegroundColor Cyan
docker build -t vikunja-mcp:test .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 镜像构建失败！" -ForegroundColor Red
    exit 1
}

# 运行容器
Write-Host ""
Write-Host "启动容器..." -ForegroundColor Cyan
docker run -d `
  --name vikunja-mcp-test `
  -p 5083:5082 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  vikunja-mcp:test

# 等待容器启动
Write-Host ""
Write-Host "等待服务启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# 测试健康检查
Write-Host ""
Write-Host "测试健康检查端点..." -ForegroundColor Cyan
$success = $false
for ($i = 1; $i -le 10; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5083/health" -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            Write-Host "✅ 健康检查通过！" -ForegroundColor Green
            $success = $true
            break
        }
    } catch {
        if ($i -eq 10) {
            Write-Host "❌ 健康检查失败！" -ForegroundColor Red
            docker logs vikunja-mcp-test
            docker rm -f vikunja-mcp-test
            exit 1
        }
        Write-Host "等待中... ($i/10)" -ForegroundColor Yellow
        Start-Sleep -Seconds 2
    }
}

if (-not $success) {
    Write-Host "❌ 健康检查失败！" -ForegroundColor Red
    docker logs vikunja-mcp-test
    docker rm -f vikunja-mcp-test
    exit 1
}

# 查看容器信息
Write-Host ""
Write-Host "容器信息:" -ForegroundColor Cyan
docker ps | Select-String "vikunja-mcp-test"

Write-Host ""
Write-Host "镜像大小:" -ForegroundColor Cyan
docker images vikunja-mcp:test

# 清理
Write-Host ""
Write-Host "清理测试容器..." -ForegroundColor Yellow
docker rm -f vikunja-mcp-test

Write-Host ""
Write-Host "✅ Docker 测试完成！" -ForegroundColor Green
