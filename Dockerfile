# syntax=docker/dockerfile:1

# Frontend build stage
FROM node:20-alpine AS frontend-build
WORKDIR /frontend

# Copy frontend files
COPY --link src/VikunjaHook/VikunjaHook/wwwroot/package*.json ./
RUN npm ci --prefer-offline --no-audit

COPY --link src/VikunjaHook/VikunjaHook/wwwroot/ ./
RUN npm run build

# Backend build stage - 使用官方 AOT SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS backend-build
WORKDIR /source

# Copy solution and project files
COPY --link src/VikunjaHook/VikunjaHook.sln ./
COPY --link src/VikunjaHook/Vikunja.Core/Vikunja.Core.csproj ./Vikunja.Core/
COPY --link src/VikunjaHook/VikunjaHook/VikunjaHook.csproj ./VikunjaHook/

# Restore dependencies
RUN --mount=type=cache,target=/root/.nuget \
    dotnet restore

# Copy source code
COPY --link src/VikunjaHook/Vikunja.Core/ ./Vikunja.Core/
COPY --link src/VikunjaHook/VikunjaHook/ ./VikunjaHook/

# Copy built frontend to wwwroot
COPY --from=frontend-build /frontend/dist ./VikunjaHook/wwwroot/dist/

# Build and publish
RUN --mount=type=cache,target=/root/.nuget \
    --mount=type=cache,target=/source/bin \
    --mount=type=cache,target=/source/obj \
    dotnet publish -c Release -o /app VikunjaHook/VikunjaHook.csproj \
    && rm -f /app/*.dbg /app/*.Development.json

# Compress binary with UPX
# 压缩前: ~24MB -> 压缩后: ~7-8MB (节省 65-70%)
RUN apk add --no-cache upx \
    && upx --best --lzma /app/VikunjaHook \
    && apk del upx

# Runtime stage - runtime-deps 包含所有必需依赖
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app

# Copy application
COPY --link --from=backend-build /app .

# Copy test scripts for container testing
COPY --link test-*.sh ./

# Create data directory
RUN mkdir -p /app/data/configs

EXPOSE 8080
VOLUME /app/data/configs

ENV ASPNETCORE_URLS=http://+:8080

USER $APP_UID

ENTRYPOINT ["./VikunjaHook"]
