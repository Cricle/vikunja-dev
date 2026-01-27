#!/bin/bash
# Setup and Run Script for Vikunja Webhook Notification System

set -e

SKIP_BUILD=false
DEV_MODE=false
VIKUNJA_URL=""
VIKUNJA_TOKEN=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --dev)
            DEV_MODE=true
            shift
            ;;
        --vikunja-url)
            VIKUNJA_URL="$2"
            shift 2
            ;;
        --vikunja-token)
            VIKUNJA_TOKEN="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--skip-build] [--dev] [--vikunja-url URL] [--vikunja-token TOKEN]"
            exit 1
            ;;
    esac
done

echo "🔔 Vikunja Webhook Notification System - Setup & Run"
echo "================================================="
echo ""

# Check prerequisites
echo "Checking prerequisites..."

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "✅ .NET SDK found: $DOTNET_VERSION"
else
    echo "❌ .NET SDK not found. Please install .NET 10 SDK."
    exit 1
fi

# Check Node.js
if command -v node &> /dev/null; then
    NODE_VERSION=$(node --version)
    echo "✅ Node.js found: $NODE_VERSION"
else
    echo "❌ Node.js not found. Please install Node.js 18+."
    exit 1
fi

# Check npm
if command -v npm &> /dev/null; then
    NPM_VERSION=$(npm --version)
    echo "✅ npm found: $NPM_VERSION"
else
    echo "❌ npm not found. Please install npm."
    exit 1
fi

echo ""

# Set environment variables
if [ -n "$VIKUNJA_URL" ]; then
    export VIKUNJA_API_URL="$VIKUNJA_URL"
    echo "✅ VIKUNJA_API_URL set to: $VIKUNJA_URL"
fi

if [ -n "$VIKUNJA_TOKEN" ]; then
    export VIKUNJA_API_TOKEN="$VIKUNJA_TOKEN"
    echo "✅ VIKUNJA_API_TOKEN set"
fi

# Check if environment variables are set
if [ -z "$VIKUNJA_API_URL" ]; then
    echo "⚠️  VIKUNJA_API_URL not set. Please set it:"
    echo '   export VIKUNJA_API_URL="https://your-vikunja.com/api/v1"'
    echo "   Or use: ./setup-and-run.sh --vikunja-url 'https://your-vikunja.com/api/v1'"
    exit 1
fi

if [ -z "$VIKUNJA_API_TOKEN" ]; then
    echo "⚠️  VIKUNJA_API_TOKEN not set. Please set it:"
    echo '   export VIKUNJA_API_TOKEN="your_token_here"'
    echo "   Or use: ./setup-and-run.sh --vikunja-token 'your_token_here'"
    exit 1
fi

echo ""

# Build frontend
if [ "$SKIP_BUILD" = false ]; then
    echo "Building frontend..."
    
    WWWROOT_PATH="src/VikunjaHook/VikunjaHook/wwwroot"
    
    if [ ! -d "$WWWROOT_PATH/node_modules" ]; then
        echo "Installing npm dependencies..."
        cd "$WWWROOT_PATH"
        npm install
        cd - > /dev/null
        echo "✅ npm dependencies installed"
    else
        echo "✅ npm dependencies already installed"
    fi
    
    echo "Building frontend assets..."
    cd "$WWWROOT_PATH"
    
    if [ "$DEV_MODE" = true ]; then
        echo "Starting frontend in development mode..."
        npm run dev &
        echo "✅ Frontend dev server starting on http://localhost:5173"
    else
        npm run build
        echo "✅ Frontend built successfully"
    fi
    
    cd - > /dev/null
else
    echo "⏭️  Skipping frontend build"
fi

echo ""

# Create data directory if it doesn't exist
DATA_DIR="data/configs"
if [ ! -d "$DATA_DIR" ]; then
    mkdir -p "$DATA_DIR"
    echo "✅ Created data directory: $DATA_DIR"
fi

echo ""
echo "Starting backend..."

# Run backend
PROJECT_PATH="src/VikunjaHook/VikunjaHook/VikunjaHook.csproj"

if [ "$DEV_MODE" = true ]; then
    echo "Running in development mode with hot reload..."
    dotnet watch run --project "$PROJECT_PATH"
else
    dotnet run --project "$PROJECT_PATH"
fi
