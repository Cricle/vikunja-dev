# Vikunja MCP C# Server

[![CI - Build and Test](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/YOUR_REPO/actions/workflows/ci.yml)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A Model Context Protocol (MCP) server for Vikunja task management, built with C# and ASP.NET Core with Native AOT support.

## Features

- ✅ **5 Tools with 45+ Subcommands**
  - Tasks (22 subcommands): Full task management including CRUD, assignments, comments, relations, reminders, labels, bulk operations
  - Projects (11 subcommands): CRUD, archiving, hierarchy management (tree, breadcrumb, move)
  - Labels (5 subcommands): Label CRUD operations
  - Teams (3 subcommands): Team management
  - Users (4 subcommands): User management (JWT only)

- ✅ **Native AOT Compilation** - Fast startup, low memory footprint
- ✅ **HTTP/RESTful API** - Easy integration with any client
- ✅ **Dual Authentication** - Supports both API tokens and JWT
- ✅ **Resilient HTTP Client** - Polly retry and circuit breaker policies
- ✅ **Type-Safe** - C# record types with source-generated JSON serialization
- ✅ **Project Hierarchy** - Full tree operations with circular reference detection

## Quick Start

### Prerequisites

- .NET 10.0 SDK or later
- A running Vikunja instance

### Build and Run

```bash
# Development mode
cd src/VikunjaHook/VikunjaHook
dotnet run

# Production build with AOT
dotnet publish -c Release
./bin/Release/net10.0/win-x64/publish/VikunjaHook.exe
```

The server will start on `http://localhost:5082` by default.

## API Endpoints

### Authentication

```bash
# Authenticate with Vikunja
POST /mcp/auth
Content-Type: application/json

{
  "apiUrl": "https://your-vikunja-instance.com/api/v1",
  "apiToken": "your-api-token-or-jwt"
}

# Response
{
  "sessionId": "generated-session-id",
  "authType": "ApiToken" | "Jwt"
}
```

### Server Information

```bash
# Get server info
GET /mcp/info

# Get available tools
GET /mcp/tools

# Health check
GET /mcp/health
```

### Tool Invocation

```bash
# RESTful tool invocation
POST /mcp/tools/{toolName}/{subcommand}
Authorization: Bearer {sessionId}
Content-Type: application/json

{
  "param1": "value1",
  "param2": "value2"
}
```

## Usage Examples

### 1. List Tasks

```bash
curl -X POST http://localhost:5082/mcp/tools/tasks/list \
  -H "Authorization: Bearer your-session-id" \
  -H "Content-Type: application/json" \
  -d '{
    "page": 1,
    "perPage": 50,
    "projectId": 1
  }'
```

### 2. Create a Task

```bash
curl -X POST http://localhost:5082/mcp/tools/tasks/create \
  -H "Authorization: Bearer your-session-id" \
  -H "Content-Type: application/json" \
  -d '{
    "projectId": 1,
    "title": "My new task",
    "description": "Task description",
    "priority": 3,
    "dueDate": "2024-12-31T23:59:59Z"
  }'
```

### 3. Get Project Tree

```bash
curl -X POST http://localhost:5082/mcp/tools/projects/get-tree \
  -H "Authorization: Bearer your-session-id" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 1,
    "maxDepth": 5,
    "includeArchived": false
  }'
```

### 4. Move a Project

```bash
curl -X POST http://localhost:5082/mcp/tools/projects/move \
  -H "Authorization: Bearer your-session-id" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 5,
    "parentProjectId": 2
  }'
```

## Available Tools and Subcommands

### Tasks Tool
- `list` - List tasks with pagination and filtering
- `create` - Create a new task
- `get` - Get task by ID
- `update` - Update task
- `delete` - Delete task
- `assign` - Assign users to task
- `unassign` - Unassign users from task
- `list-assignees` - List task assignees
- `comment` - Add comment to task
- `list-comments` - List task comments
- `relate` - Create task relation
- `unrelate` - Remove task relation
- `relations` - List task relations
- `add-reminder` - Add reminder to task
- `remove-reminder` - Remove reminder from task
- `list-reminders` - List task reminders
- `apply-label` - Apply labels to task
- `remove-label` - Remove labels from task
- `list-labels` - List task labels
- `bulk-create` - Bulk create tasks (max 100)
- `bulk-update` - Bulk update tasks
- `bulk-delete` - Bulk delete tasks

### Projects Tool
- `list` - List projects with search and pagination
- `create` - Create a new project
- `get` - Get project by ID
- `update` - Update project
- `delete` - Delete project
- `archive` - Archive project
- `unarchive` - Unarchive project
- `get-children` - Get direct children of a project
- `get-tree` - Get complete project tree
- `get-breadcrumb` - Get breadcrumb path to project
- `move` - Move project to new parent

### Labels Tool
- `list` - List labels
- `create` - Create label
- `get` - Get label by ID
- `update` - Update label
- `delete` - Delete label

### Teams Tool
- `list` - List teams
- `create` - Create team
- `delete` - Delete team

### Users Tool (JWT Only)
- `current` - Get current user info
- `search` - Search users
- `settings` - Get user settings
- `update-settings` - Update user settings

## Architecture

### Project Structure

```
src/VikunjaHook/VikunjaHook/
├── Mcp/
│   ├── Models/          # MCP and Vikunja entity models
│   │   └── Requests/    # Request DTOs
│   ├── Services/        # Core services (Auth, HTTP client, etc.)
│   └── Tools/           # MCP tool implementations
├── Models/              # Webhook models
├── Services/            # Webhook services
└── Program.cs           # Application entry point
```

### Key Components

- **Authentication Manager**: Handles session management and token validation
- **Vikunja Client Factory**: HTTP client with Polly resilience policies
- **Tool Registry**: Manages available MCP tools
- **MCP Server**: Dispatches requests to appropriate tools
- **Response Factory**: Creates standardized responses with error handling

## Configuration

The server uses standard ASP.NET Core configuration. You can configure:

- Listening ports via `ASPNETCORE_URLS` environment variable
- Logging levels via `appsettings.json`
- CORS policies (if needed)

## Error Handling

The server provides comprehensive error handling with specific error codes:

- **1xxx**: Authentication errors
- **2xxx**: Validation errors
- **3xxx**: Resource not found errors
- **4xxx**: Operation errors
- **5xxx**: Server errors

## Performance

- **Native AOT**: Fast startup (~50ms) and low memory usage
- **Connection Pooling**: Efficient HTTP client reuse
- **Retry Logic**: Automatic retry with exponential backoff (1s, 2s, 4s)
- **Circuit Breaker**: Prevents cascading failures (5 errors, 30s timeout)

## Development

### Building

```bash
dotnet build
```

### Running Tests

#### 自动化测试（推荐）

使用提供的测试脚本自动构建、启动服务器并运行测试：

**Linux/macOS:**
```bash
chmod +x run-tests.sh
./run-tests.sh "https://your-vikunja.com/api/v1" "tk_your_token"
```

**Windows PowerShell:**
```powershell
.\run-tests.ps1 -VikunjaUrl "https://your-vikunja.com/api/v1" -VikunjaToken "tk_your_token"
```

或使用环境变量：
```bash
# Linux/macOS
export VIKUNJA_URL="https://your-vikunja.com/api/v1"
export VIKUNJA_TOKEN="tk_your_token"
./run-tests.sh

# Windows PowerShell
$env:VIKUNJA_URL="https://your-vikunja.com/api/v1"
$env:VIKUNJA_TOKEN="tk_your_token"
.\run-tests.ps1
```

#### 手动测试

1. 启动服务器：
```bash
cd src/VikunjaHook/VikunjaHook
dotnet run
```

2. 在另一个终端运行测试：
```bash
export VIKUNJA_URL="https://your-vikunja.com/api/v1"
export VIKUNJA_TOKEN="tk_your_token"
node test-complete.js
```

### 持续集成 (CI)

项目配置了 GitHub Actions CI，会在每次推送和 PR 时自动运行：

- ✅ 多平台构建检查 (Ubuntu, Windows, macOS)
- ✅ 完整的测试套件 (28 项测试)
- ✅ 构建警告检查

**设置 CI Secrets:**

在 GitHub 仓库中配置以下 secrets：
- `VIKUNJA_URL`: Vikunja API URL (包含 `/api/v1`)
- `VIKUNJA_TOKEN`: Vikunja API Token

详细说明请参考 [.github/SETUP_SECRETS.md](.github/SETUP_SECRETS.md)

### Publishing for Production

```bash
# Windows x64
dotnet publish -c Release -r win-x64

# Linux x64
dotnet publish -c Release -r linux-x64

# macOS ARM64
dotnet publish -c Release -r osx-arm64
```

## License

[Your License Here]

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
