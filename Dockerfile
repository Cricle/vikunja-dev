# syntax=docker/dockerfile:1

# ============================================
# Frontend build stage - 优化 Node.js 构建
# ============================================
FROM node:20-alpine AS frontend-build
WORKDIR /frontend

# 安装依赖（利用缓存层）
COPY --link src/VikunjaHook/VikunjaHook/wwwroot/package*.json ./
RUN --mount=type=cache,target=/root/.npm \
    npm ci --prefer-offline --no-audit

# 复制源码并构建（生产模式）
COPY --link src/VikunjaHook/VikunjaHook/wwwroot/ ./
RUN npm run build \
    && find dist -type f -name "*.map" -delete

# ============================================
# Backend build stage - 优化 .NET AOT 构建
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS backend-build
WORKDIR /source

# 复制项目文件（利用缓存层）
COPY --link src/VikunjaHook/VikunjaHook.sln ./
COPY --link src/VikunjaHook/Vikunja.Core/Vikunja.Core.csproj ./Vikunja.Core/
COPY --link src/VikunjaHook/VikunjaHook/VikunjaHook.csproj ./VikunjaHook/

# 恢复依赖（使用缓存）
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore --runtime linux-musl-x64

# 复制源码
COPY --link src/VikunjaHook/Vikunja.Core/ ./Vikunja.Core/
COPY --link src/VikunjaHook/VikunjaHook/ ./VikunjaHook/

# 复制前端构建产物
COPY --from=frontend-build /frontend/dist ./VikunjaHook/wwwroot/dist/

# 发布应用（优化 AOT 编译）
RUN --mount=type=cache,target=/root/.nuget/packages \
    --mount=type=cache,target=/source/bin \
    --mount=type=cache,target=/source/obj \
    dotnet publish -c Release -o /app \
    --runtime linux-musl-x64 \
    --self-contained true \
    -p:PublishTrimmed=true \
    -p:PublishSingleFile=false \
    -p:EnableCompressionInSingleFile=true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -p:StripSymbols=true \
    VikunjaHook/VikunjaHook.csproj \
    && rm -rf /app/*.pdb /app/*.dbg /app/*.Development.json /app/appsettings.Development.json

# 压缩二进制文件（UPX）
RUN apk add --no-cache upx \
    && upx --best --lzma /app/VikunjaHook \
    && apk del upx

# ============================================
# Runtime stage - 最小化运行时镜像
# ============================================
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS runtime

# 添加非 root 用户（安全最佳实践）
RUN addgroup -g 1000 vikunja \
    && adduser -D -u 1000 -G vikunja vikunja

WORKDIR /app

# 复制应用程序
COPY --link --from=backend-build --chown=vikunja:vikunja /app .

# 创建数据目录
RUN mkdir -p /app/data/configs \
    && chown -R vikunja:vikunja /app/data

# 切换到非 root 用户
USER vikunja

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5082/health || exit 1

EXPOSE 5082
VOLUME /app/data

ENV ASPNETCORE_URLS=http://+:5082 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["./VikunjaHook"]
