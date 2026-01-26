# Build stage - 使用 .NET 10 SDK 进行 AOT 编译
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["src/VikunjaHook/VikunjaHook/VikunjaHook.csproj", "VikunjaHook/"]

# 还原依赖
RUN dotnet restore "VikunjaHook/VikunjaHook.csproj"

# 复制所有源代码
COPY ["src/VikunjaHook/VikunjaHook/", "VikunjaHook/"]

# 构建和发布 AOT 版本
WORKDIR "/src/VikunjaHook"
RUN dotnet publish "VikunjaHook.csproj" \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true \
    /p:StripSymbols=true \
    /p:EnableCompressionInSingleFile=true

# Runtime stage - 使用最小的运行时镜像
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine AS runtime
WORKDIR /app

# 安装必要的运行时依赖
RUN apk add --no-cache \
    icu-libs \
    tzdata

# 创建非 root 用户
RUN addgroup -g 1000 vikunja && \
    adduser -D -u 1000 -G vikunja vikunja

# 复制 AOT 编译的二进制文件
COPY --from=build /app/publish .

# 创建日志目录
RUN mkdir -p /app/logs && \
    chown -R vikunja:vikunja /app

# 切换到非 root 用户
USER vikunja

# 暴露端口
EXPOSE 5082

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5082/health || exit 1

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5082 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# 运行应用
ENTRYPOINT ["./VikunjaHook"]
