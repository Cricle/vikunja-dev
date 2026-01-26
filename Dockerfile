# Frontend build stage - 构建 Vue.js 前端
FROM node:20-alpine AS frontend-build
WORKDIR /frontend

# 复制前端项目文件
COPY ["src/vikunja-mcp-admin/package*.json", "./"]

# 安装所有依赖（包括 devDependencies，构建需要）
RUN npm ci

# 复制前端源代码
COPY ["src/vikunja-mcp-admin/", "./"]

# 构建前端（跳过类型检查以加快构建速度）
RUN npx vite build

# Backend build stage - 使用 .NET 10 SDK 进行 AOT 编译
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# 安装 AOT 编译所需的工具
RUN apt-get update && \
    apt-get install -y clang zlib1g-dev && \
    rm -rf /var/lib/apt/lists/*

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
    /p:IlcOptimizationPreference=Size \
    /p:IlcGenerateStackTraceData=false
    
# Runtime stage - 使用最小的运行时镜像
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

# 复制 AOT 编译的二进制文件
COPY --from=backend-build /app/publish .

# 复制前端构建产物到 wwwroot
COPY --from=frontend-build /frontend/dist ./wwwroot

# 暴露端口
EXPOSE 5082

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:5082 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_ENVIRONMENT=Production

# 运行应用
ENTRYPOINT ["./VikunjaHook"]
