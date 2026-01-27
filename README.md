# Vikunja MCP Server & Webhook Notification System

A high-performance MCP server and webhook notification system for [Vikunja](https://vikunja.io/), built with .NET 10 AOT.

## Features

- **54 MCP Tools** - Complete Vikunja API coverage
- **26 Webhook Events** - Real-time event handling
- **Notification System** - Multi-provider notifications with web UI
- **Native AOT** - Fast startup, minimal memory
- **Docker Ready** - 35MB image with full stack

## Quick Start

### Docker Compose (Recommended)

```bash
git clone https://github.com/Cricle/vikunja-dev.git
cd vikunja-dev
docker-compose up -d
```

Access:
- Vikunja: http://localhost:8080
- Webhook UI: http://localhost:5082

### Standalone

```bash
# Windows
.\setup-and-run.ps1 -VikunjaUrl "https://vikunja.example.com/api/v1" -VikunjaToken "your_token"

# Linux/macOS
./setup-and-run.sh --vikunja-url "https://vikunja.example.com/api/v1" --vikunja-token "your_token"
```

## Configuration

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `VIKUNJA_API_URL` | Yes | Vikunja API URL |
| `VIKUNJA_API_TOKEN` | Yes | Vikunja API token |
| `ASPNETCORE_URLS` | No | Server URLs (default: `http://+:5082`) |

### Notification System

The web UI provides:
- **Providers**: Configure notification providers (PushDeer, etc.)
- **Project Rules**: Set which events trigger notifications per project
- **Templates**: Customize notification messages with placeholders
- **Dashboard**: View system status and statistics

Configuration is stored in `data/configs/default.json`.

## MCP Tools

54 tools covering all Vikunja APIs:
- Tasks (5), Projects (5), Labels (5)
- Comments (5), Attachments (3), Relations (2)
- Assignees (3), Teams (5), Users (3)
- Buckets (5), Webhooks (5), Filters (5)

## Webhook Events

26 events supported:
- Task: created, updated, deleted, assignee, comment, attachment, relation, label
- Project: created, updated, deleted
- Label: created, updated, deleted
- Team: created, updated, deleted, member
- User: created

## API Endpoints

- `POST /mcp` - MCP protocol endpoint
- `POST /webhook/vikunja` - Webhook handler
- `GET /api/webhook-config/{userId}` - Get notification config
- `PUT /api/webhook-config/{userId}` - Update notification config
- `GET /health` - Health check

## Development

```bash
cd src/VikunjaHook
dotnet build -c Release
dotnet run -c Release
```

### Testing

```bash
# Test MCP tools
pwsh -File test-mcp-tools.ps1

# Test Docker
pwsh -File test-docker.ps1
```

## Performance

- Startup: < 50ms
- Memory: ~30MB idle
- Image: 35MB (Alpine + AOT + UPX)

## License

MIT
