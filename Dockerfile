# syntax=docker/dockerfile:1
# Build stage - 使用官方 AOT SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine-aot AS build
WORKDIR /source

# Copy and build
COPY --link src/VikunjaHook/VikunjaHook/ .
RUN --mount=type=cache,target=/root/.nuget \
    --mount=type=cache,target=/source/bin \
    --mount=type=cache,target=/source/obj \
    dotnet publish -o /app VikunjaHook.csproj \
    && rm -f /app/*.dbg /app/*.Development.json

# Compress binary with UPX
RUN apk add --no-cache upx \
    && upx --best --lzma /app/VikunjaHook \
    && apk del upx

# Runtime stage - runtime-deps 包含所有必需依赖
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine
WORKDIR /app

COPY --link --from=build /app .

EXPOSE 5082

ENV ASPNETCORE_URLS=http://+:5082 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true \
    ASPNETCORE_ENVIRONMENT=Production

USER $APP_UID

ENTRYPOINT ["./VikunjaHook"]
