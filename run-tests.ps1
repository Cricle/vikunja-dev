# Vikunja MCP C# 服务器测试运行脚本 (Windows PowerShell)
# 用法: .\run-tests.ps1 [-VikunjaUrl <url>] [-VikunjaToken <token>]

param(
    [string]$VikunjaUrl,
    [string]$VikunjaToken
)

$ErrorActionPreference = "Stop"

# 颜色函数
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorOutput "╔══════════════════════════════════════════════════════════════╗" "Cyan"
Write-ColorOutput "║  Vikunja MCP C# 服务器测试运行器                            ║" "Cyan"
Write-ColorOutput "╚══════════════════════════════════════════════════════════════╝" "Cyan"
Write-Host ""

# 设置环境变量
if ($VikunjaUrl) {
    $env:VIKUNJA_URL = $VikunjaUrl
}

if ($VikunjaToken) {
    $env:VIKUNJA_TOKEN = $VikunjaToken
}

# 验证必需的环境变量
if (-not $env:VIKUNJA_URL) {
    Write-ColorOutput "错误: 未设置 VIKUNJA_URL" "Red"
    Write-Host "用法: .\run-tests.ps1 -VikunjaUrl <url> -VikunjaToken <token>"
    Write-Host "或设置环境变量: `$env:VIKUNJA_URL='https://your-vikunja.com/api/v1'"
    exit 1
}

if (-not $env:VIKUNJA_TOKEN) {
    Write-ColorOutput "错误: 未设置 VIKUNJA_TOKEN" "Red"
    Write-Host "用法: .\run-tests.ps1 -VikunjaUrl <url> -VikunjaToken <token>"
    Write-Host "或设置环境变量: `$env:VIKUNJA_TOKEN='tk_your_token'"
    exit 1
}

Write-ColorOutput "✓ 环境变量已设置" "Green"
Write-Host "  VIKUNJA_URL: $env:VIKUNJA_URL"
$tokenPreview = $env:VIKUNJA_TOKEN.Substring(0, [Math]::Min(10, $env:VIKUNJA_TOKEN.Length))
Write-Host "  VIKUNJA_TOKEN: $tokenPreview..."
Write-Host ""

# 检查 .NET 是否安装
try {
    $dotnetVersion = dotnet --version
    Write-ColorOutput "✓ .NET SDK 已安装" "Green"
    Write-Host "  版本: $dotnetVersion"
    Write-Host ""
} catch {
    Write-ColorOutput "错误: 未找到 dotnet 命令" "Red"
    Write-Host "请安装 .NET SDK: https://dotnet.microsoft.com/download"
    exit 1
}

# 检查 Node.js 是否安装
try {
    $nodeVersion = node --version
    Write-ColorOutput "✓ Node.js 已安装" "Green"
    Write-Host "  版本: $nodeVersion"
    Write-Host ""
} catch {
    Write-ColorOutput "错误: 未找到 node 命令" "Red"
    Write-Host "请安装 Node.js: https://nodejs.org/"
    exit 1
}

# 构建项目
Write-ColorOutput "正在构建项目..." "Yellow"
Push-Location src\VikunjaHook
try {
    dotnet build VikunjaHook.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "构建失败"
    }
    Write-ColorOutput "✓ 构建成功" "Green"
    Write-Host ""
} catch {
    Write-ColorOutput "✗ 构建失败" "Red"
    Pop-Location
    exit 1
}

# 启动服务器
Write-ColorOutput "正在启动 MCP 服务器..." "Yellow"
Push-Location VikunjaHook
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --configuration Release" -PassThru -NoNewWindow
Pop-Location
Pop-Location

# 等待服务器启动
Write-ColorOutput "等待服务器就绪..." "Yellow"
$maxAttempts = 30
$attempt = 0
$serverReady = $false

while ($attempt -lt $maxAttempts) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5082/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            $serverReady = $true
            break
        }
    } catch {
        # 继续等待
    }
    Start-Sleep -Seconds 1
    $attempt++
}

if (-not $serverReady) {
    Write-ColorOutput "✗ 服务器启动超时" "Red"
    Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-ColorOutput "✓ 服务器已就绪" "Green"
Write-Host ""

# 运行测试
Write-ColorOutput "正在运行测试..." "Yellow"
Write-Host ""
node test-complete.js
$testExitCode = $LASTEXITCODE

# 停止服务器
Write-Host ""
Write-ColorOutput "正在停止服务器..." "Yellow"
Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
Write-ColorOutput "✓ 服务器已停止" "Green"

# 输出结果
Write-Host ""
if ($testExitCode -eq 0) {
    Write-ColorOutput "╔══════════════════════════════════════════════════════════════╗" "Green"
    Write-ColorOutput "║  🎉 所有测试通过！                                          ║" "Green"
    Write-ColorOutput "╚══════════════════════════════════════════════════════════════╝" "Green"
    exit 0
} else {
    Write-ColorOutput "╔══════════════════════════════════════════════════════════════╗" "Red"
    Write-ColorOutput "║  ❌ 测试失败                                                 ║" "Red"
    Write-ColorOutput "╚══════════════════════════════════════════════════════════════╝" "Red"
    exit 1
}
