# Vikunja Hook

[![.NET Version](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Docker Image](https://img.shields.io/badge/Docker-28MB-2496ED)](https://hub.docker.com/)

A dual-mode server for Vikunja task management system, built with .NET 10 Native AOT.

## Features

- 🚀 **Dual Mode**: MCP Server (stdio) + Webhook API (HTTP)
- 📦 **Ultra Small**: Docker image 28MB, binary ~5MB
- ⚡ **Native AOT**: Fast startup (<100ms), low memory (~20MB)
- 🛠️ **54 MCP Tools**: Complete Vikunja API coverage
- 🔔 **Webhook Support**: Real-time event notifications

## Quick Start

### Mode 1: MCP Server (for AI assistants)

```bash
# Set environment variables
export VIKUNJA_API_URL="https://vikunja.example.com/api/v1"
export VIKUNJA_API_TOKEN="your_token_here"

# Run in MCP mode
dotnet run --project src/VikunjaHook/VikunjaHook -- --mcp
```

**Kiro IDE Configuration** (`.kiro/settings/mcp.json`):
```json
{
  "mcpServers": {
    "vikunja": {
      "command": "dotnet",
      "args": ["run", "--project", "src/VikunjaHook/VikunjaHook", "--", "--mcp"],
      "env": {
        "VIKUNJA_API_URL": "https://vikunja.example.com/api/v1",
        "VIKUNJA_API_TOKEN": "your_token_here"
      }
    }
  }
}
```

### Mode 2: Webhook API (for event notifications)

```bash
# Run in webhook mode (default)
dotnet run --project src/VikunjaHook/VikunjaHook
```

Server starts at `http://localhost:5082`

**Vikunja Webhook Configuration:**
- URL: `http://your-server:5082/webhook/vikunja`
- Events: Select events to monitor

## API Endpoints

### Webhook Endpoints
- `POST /webhook/vikunja` - Receive Vikunja webhook events
- `GET /webhook/vikunja/events` - List supported events

### Health Check
- `GET /health` - Server health status

## Custom Webhook Handler

You can create custom webhook handlers by inheriting from `WebhookHandlerBase`:

```csharp
public class MyCustomHandler : WebhookHandlerBase
{
    public MyCustomHandler(ILogger<MyCustomHandler> logger) : base(logger)
    {
    }

    // Override only the events you want to handle
    protected override async Task OnTaskCreatedAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken)
    {
        // Your custom logic here
        Logger.LogInformation("Task created: {EventName}", payload.EventName);
        
        // Send notification, update database, etc.
        
        await base.OnTaskCreatedAsync(payload, cancellationToken);
    }
}
```

Register your custom handler in `Program.cs`:

```csharp
builder.Services.AddSingleton<IWebhookHandler, MyCustomHandler>();
```

Available virtual methods (26 events):
- Task: `OnTaskCreated`, `OnTaskUpdated`, `OnTaskDeleted`
- Project: `OnProjectCreated`, `OnProjectUpdated`, `OnProjectDeleted`
- Task Assignee: `OnTaskAssigneeCreated`, `OnTaskAssigneeDeleted`
- Task Comment: `OnTaskCommentCreated`, `OnTaskCommentUpdated`, `OnTaskCommentDeleted`
- Task Attachment: `OnTaskAttachmentCreated`, `OnTaskAttachmentDeleted`
- Task Relation: `OnTaskRelationCreated`, `OnTaskRelationDeleted`
- Label: `OnLabelCreated`, `OnLabelUpdated`, `OnLabelDeleted`
- Task Label: `OnTaskLabelCreated`, `OnTaskLabelDeleted`
- User: `OnUserCreated`
- Team: `OnTeamCreated`, `OnTeamUpdated`, `OnTeamDeleted`
- Team Member: `OnTeamMemberAdded`, `OnTeamMemberRemoved`
- Special: `OnUnknownEvent`, `OnError`

## MCP Tools

| Category | Tools | Description |
|----------|-------|-------------|
| **Tasks** | 5 | List, Create, Get, Update, Delete |
| **Task Assignees** | 3 | Add, Remove, List |
| **Task Comments** | 5 | List, Create, Get, Update, Delete |
| **Task Attachments** | 3 | List, Get, Delete |
| **Task Relations** | 2 | Create, Delete |
| **Task Labels** | 3 | Add, Remove, List |
| **Projects** | 5 | List, Create, Get, Update, Delete |
| **Labels** | 5 | List, Create, Get, Update, Delete |
| **Teams** | 5 | List, Create, Get, Update, Delete |
| **Users** | 3 | GetCurrentUser, SearchUsers, GetUser |
| **Buckets** | 5 | List, Create, Get, Update, Delete |
| **Webhooks** | 5 | List, Create, Get, Update, Delete |
| **Saved Filters** | 5 | List, Create, Get, Update, Delete |

**Total: 54 MCP tools**

## Docker

### Build

```bash
docker build -t vikunja-hook .
```

### Run MCP Mode

```bash
docker run -it \
  -e VIKUNJA_API_URL="https://vikunja.example.com/api/v1" \
  -e VIKUNJA_API_TOKEN="your_token" \
  vikunja-hook --mcp
```

### Run Webhook Mode

```bash
docker run -d -p 5082:5082 vikunja-hook
```

### Docker Compose

```yaml
version: '3.8'
services:
  vikunja-webhook:
    image: vikunja-hook
    ports:
      - "5082:5082"
    restart: unless-stopped
```

## Development

### Prerequisites
- .NET 10 SDK
- Docker (optional)

### Build

```bash
cd src/VikunjaHook/VikunjaHook
dotnet build
```

### Test

```bash
# Test webhook mode
dotnet run

# Test MCP mode
export VIKUNJA_API_URL="https://vikunja.example.com/api/v1"
export VIKUNJA_API_TOKEN="your_token"
dotnet run -- --mcp
```

### Publish (Native AOT)

```bash
dotnet publish -c Release -r linux-x64
```

## Supported Vikunja Events

- **Task**: created, updated, deleted
- **Project**: created, updated, deleted
- **Label**: created, updated, deleted
- **Team**: created, updated, deleted, member.added, member.removed
- **User**: created
- **Task Relations**: assignee, comment, attachment, relation, label

## Performance

- **Binary Size**: ~5MB (UPX compressed)
- **Docker Image**: 28MB (Alpine-based)
- **Memory Usage**: ~20MB at runtime
- **Startup Time**: <100ms (Native AOT)

## Architecture

```
vikunja-hook/
├── Program.cs              # Dual-mode entry point
├── Services/
│   ├── IWebhookHandler.cs
│   └── DefaultWebhookHandler.cs
├── Mcp/
│   ├── Tools/              # 23 MCP tools
│   │   ├── TasksTools.cs
│   │   ├── ProjectsTools.cs
│   │   ├── LabelsTools.cs
│   │   ├── TeamsTools.cs
│   │   └── UsersTools.cs
│   └── Services/
│       └── VikunjaClientFactory.cs
└── Models/                 # Data models
```

## License

MIT License

## Links

- [Vikunja](https://vikunja.io/) - Task management system
- [Model Context Protocol](https://modelcontextprotocol.io/) - MCP specification
- [.NET 10](https://dotnet.microsoft.com/) - .NET platform
