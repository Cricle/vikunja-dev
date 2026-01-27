# Docker 测试脚本 - Windows PowerShell

Write-Host "🐳 测试 Docker 构建和运行..." -ForegroundColor Cyan
Write-Host ""

# 清理旧容器和镜像
Write-Host "清理旧容器..." -ForegroundColor Yellow
docker rm -f vikunja-mcp-test 2>$null

# 构建镜像
Write-Host ""
Write-Host "构建 Docker 镜像（包含前端编译）..." -ForegroundColor Cyan
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
  -e VIKUNJA_API_URL=http://host.docker.internal:8080/api/v1 `
  -e VIKUNJA_API_TOKEN=test_token `
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
            Write-Host ""
            Write-Host "容器日志:" -ForegroundColor Yellow
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

# 测试静态文件服务
Write-Host ""
Write-Host "测试静态文件服务..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5083/" -UseBasicParsing -TimeoutSec 2
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ 静态文件服务正常！" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠ 静态文件服务可能有问题" -ForegroundColor Yellow
}

# 查看容器信息
Write-Host ""
Write-Host "容器信息:" -ForegroundColor Cyan
docker ps | Select-String "vikunja-mcp-test"

Write-Host ""
Write-Host "镜像大小:" -ForegroundColor Cyan
docker images vikunja-mcp:test

# 显示容器日志（最后 20 行）
Write-Host ""
Write-Host "容器日志（最后 20 行）:" -ForegroundColor Cyan
docker logs --tail 20 vikunja-mcp-test

# 清理
Write-Host ""
Write-Host "清理测试容器..." -ForegroundColor Yellow
docker rm -f vikunja-mcp-test
Write-Host "✅ 测试容器已清理" -ForegroundColor Green

Write-Host ""
Write-Host "✅ Docker 测试完成！" -ForegroundColor Green
