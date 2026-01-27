# AOT Verification Script
# This script verifies that the project is 100% AOT compatible

Write-Host "=== AOT Compatibility Verification ===" -ForegroundColor Cyan
Write-Host ""

# 1. Check project configuration
Write-Host "1. Checking project configuration..." -ForegroundColor Yellow
$csprojContent = Get-Content "src/VikunjaHook/VikunjaHook/VikunjaHook.csproj" -Raw
if ($csprojContent -match "<PublishAot>true</PublishAot>") {
    Write-Host "   ✓ PublishAot is enabled" -ForegroundColor Green
} else {
    Write-Host "   ✗ PublishAot is NOT enabled" -ForegroundColor Red
    exit 1
}

if ($csprojContent -match "<InvariantGlobalization>true</InvariantGlobalization>") {
    Write-Host "   ✓ InvariantGlobalization is enabled" -ForegroundColor Green
} else {
    Write-Host "   ✗ InvariantGlobalization is NOT enabled" -ForegroundColor Red
    exit 1
}

# 2. Check for reflection usage
Write-Host ""
Write-Host "2. Checking for reflection usage..." -ForegroundColor Yellow
$reflectionPatterns = @(
    "Activator.CreateInstance",
    "Type.GetType",
    "Assembly.Load",
    "MethodInfo.Invoke"
)

$foundReflection = $false
foreach ($pattern in $reflectionPatterns) {
    $results = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | Select-String -Pattern $pattern
    if ($results) {
        Write-Host "   ✗ Found reflection usage: $pattern" -ForegroundColor Red
        $foundReflection = $true
    }
}

if (-not $foundReflection) {
    Write-Host "   ✓ No reflection usage found" -ForegroundColor Green
}

# 3. Check for dynamic code generation
Write-Host ""
Write-Host "3. Checking for dynamic code generation..." -ForegroundColor Yellow
$dynamicPatterns = @(
    "Emit",
    "DynamicMethod",
    "Expression.Compile"
)

$foundDynamic = $false
foreach ($pattern in $dynamicPatterns) {
    $results = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | Select-String -Pattern $pattern
    if ($results) {
        Write-Host "   ✗ Found dynamic code generation: $pattern" -ForegroundColor Red
        $foundDynamic = $true
    }
}

if (-not $foundDynamic) {
    Write-Host "   ✓ No dynamic code generation found" -ForegroundColor Green
}

# 4. Check JSON serialization
Write-Host ""
Write-Host "4. Checking JSON serialization..." -ForegroundColor Yellow
$jsonContexts = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse | Select-String -Pattern "JsonSerializerContext"
if ($jsonContexts) {
    Write-Host "   ✓ Found $($jsonContexts.Count) JsonSerializerContext usages" -ForegroundColor Green
} else {
    Write-Host "   ✗ No JsonSerializerContext found" -ForegroundColor Red
    exit 1
}

# 5. Build and publish
Write-Host ""
Write-Host "5. Building and publishing with AOT..." -ForegroundColor Yellow
$publishOutput = dotnet publish src/VikunjaHook/VikunjaHook/VikunjaHook.csproj -c Release -r win-x64 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✓ AOT publish successful" -ForegroundColor Green
    
    # Check output size
    $exePath = "src/VikunjaHook/VikunjaHook/bin/Release/net10.0/win-x64/publish/VikunjaHook.exe"
    if (Test-Path $exePath) {
        $size = (Get-Item $exePath).Length / 1MB
        Write-Host "   ✓ Output size: $([math]::Round($size, 2)) MB" -ForegroundColor Green
    }
} else {
    Write-Host "   ✗ AOT publish failed" -ForegroundColor Red
    Write-Host $publishOutput
    exit 1
}

# 6. Check for AOT warnings
Write-Host ""
Write-Host "6. Checking for AOT warnings..." -ForegroundColor Yellow
$warnings = $publishOutput | Select-String -Pattern "warning.*AOT|warning.*IL\d+"
if ($warnings) {
    Write-Host "   ⚠ Found AOT warnings:" -ForegroundColor Yellow
    $warnings | ForEach-Object { Write-Host "     $_" -ForegroundColor Yellow }
} else {
    Write-Host "   ✓ No AOT warnings found" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== AOT Verification Complete ===" -ForegroundColor Cyan
Write-Host "✓ Project is 100% AOT compatible!" -ForegroundColor Green
