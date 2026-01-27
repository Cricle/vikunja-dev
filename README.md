# Vikunja MCP Server & Webhook Handler

A high-performance Model Context Protocol (MCP) server and webhook handler for [Vikunja](https://vikunja.io/) task management system, built with .NET 10 and native AOT compilation.

## Features

- **54 MCP Tools** - Complete Vikunja API coverage via HTTP/SSE transport
- **26 Webhook Events** - Real-time event handling for all Vikunja events
- **Native AOT** - Ultra-fast startup and minimal memory footprint
- **Tiny Docker Image** - Only 30.5MB with UPX compression (6.3MB binary)
- **Docker Support** - Complete docker-compose stack with Vikunja, PostgreSQL, Redis (Garnet), and MinIO
- **Production Ready** - Health checks, logging, and error handling

## Quick Start

### Using Docker Compose (Recommended)

1. Clone the repository:
```bash
git clone https://github.com/Cricle/vikunja-dev.git
cd vikunja-dev
```

2. Start the complete stack:
```bash
docker-compose up -d
```

This will start:
- **Vikunja** on http://localhost:3456
- **VikunjaHook MCP Server** on http://localhost:5082
- **PostgreSQL 18** (database)
- **Garnet** (Redis-compatible cache)
- **MinIO** (S3-compatible storage) on http://localhost:9000

3. Create a Vikunja account and get your API token:
   - Visit http://localhost:3456
   - Register a new account
   - Go to Settings → API Tokens
   - Create a new token

4. Update the environment variable:
```bash
# Create .env file from example
cp .env.example .env

# Edit .env and add your token
VIKUNJA_API_TOKEN=your_token_here
```

5. Restart VikunjaHook:
```bash
docker-compose restart vikunja-hook
```

### Manual Installation

#### Prerequisites
- .NET 10 SDK
- Vikunja instance with API access

#### Build and Run

```bash
cd src/VikunjaHook
dotnet build -c Release
cd VikunjaHook

# Set environment variables
export VIKUNJA_API_URL=https://your-vikunja-instance.com/api/v1
export VIKUNJA_API_TOKEN=your_api_token

# Run the server
dotnet run -c Release --urls http://localhost:5082
```

## MCP Tools (54 total)

### Tasks (5 tools)
- `ListTasks` - List all tasks in a project
- `CreateTask` - Create a new task
- `GetTask` - Get task details
- `UpdateTask` - Update a task
- `DeleteTask` - Delete a task

### Projects (5 tools)
- `ListProjects` - List all projects
- `CreateProject` - Create a new project
- `GetProject` - Get project details
- `UpdateProject` - Update a project
- `DeleteProject` - Delete a project

### Labels (5 tools)
- `ListLabels` - List all labels
- `CreateLabel` - Create a new label
- `GetLabel` - Get label details
- `UpdateLabel` - Update a label
- `DeleteLabel` - Delete a label

### Task Labels (3 tools)
- `AddTaskLabel` - Add a label to a task
- `ListTaskLabels` - List labels on a task
- `RemoveTaskLabel` - Remove a label from a task

### Task Comments (5 tools)
- `ListTaskComments` - List comments on a task
- `CreateTaskComment` - Add a comment to a task
- `GetTaskComment` - Get comment details
- `UpdateTaskComment` - Update a comment
- `DeleteTaskComment` - Delete a comment

### Task Attachments (3 tools)
- `ListTaskAttachments` - List attachments on a task
- `GetTaskAttachment` - Get attachment details
- `DeleteTaskAttachment` - Delete an attachment

### Task Relations (2 tools)
- `CreateTaskRelation` - Create a relation between tasks
- `DeleteTaskRelation` - Delete a task relation

### Task Assignees (3 tools)
- `AddTaskAssignee` - Assign a user to a task
- `RemoveTaskAssignee` - Remove a user from a task
- `ListTaskAssignees` - List assignees on a task

### Teams (5 tools)
- `ListTeams` - List all teams
- `CreateTeam` - Create a new team
- `GetTeam` - Get team details
- `UpdateTeam` - Update a team
- `DeleteTeam` - Delete a team

### Users (3 tools)
- `GetCurrentUser` - Get current user information
- `SearchUsers` - Search for users
- `GetUser` - Get user details

### Buckets (5 tools)
- `ListBuckets` - List buckets in a project
- `CreateBucket` - Create a new bucket
- `GetBucket` - Get bucket details
- `UpdateBucket` - Update a bucket
- `DeleteBucket` - Delete a bucket

### Webhooks (5 tools)
- `ListWebhooks` - List webhooks in a project
- `CreateWebhook` - Create a new webhook
- `GetWebhook` - Get webhook details
- `UpdateWebhook` - Update a webhook
- `DeleteWebhook` - Delete a webhook

### Saved Filters (5 tools)
- `ListSavedFilters` - List saved filters
- `CreateSavedFilter` - Create a new saved filter
- `GetSavedFilter` - Get saved filter details
- `UpdateSavedFilter` - Update a saved filter
- `DeleteSavedFilter` - Delete a saved filter

## Webhook Events (26 total)

The server handles all Vikunja webhook events:

### Task Events
- `task.created`, `task.updated`, `task.deleted`
- `task.assignee.created`, `task.assignee.deleted`
- `task.comment.created`, `task.comment.updated`, `task.comment.deleted`
- `task.attachment.created`, `task.attachment.deleted`
- `task.relation.created`, `task.relation.deleted`
- `task.label.created`, `task.label.deleted`

### Project Events
- `project.created`, `project.updated`, `project.deleted`

### Label Events
- `label.created`, `label.updated`, `label.deleted`

### Team Events
- `team.created`, `team.updated`, `team.deleted`
- `team.member.added`, `team.member.removed`

### User Events
- `user.created`

## API Endpoints

### MCP Server
- `POST /mcp` - MCP protocol endpoint (HTTP/SSE)

### Webhook Handler
- `POST /webhook/vikunja` - Vikunja webhook endpoint
- `GET /webhook/vikunja/events` - List supported events
- `GET /health` - Health check endpoint

## Configuration

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `VIKUNJA_API_URL` | Yes | Vikunja API URL (e.g., `https://vikunja.example.com/api/v1`) |
| `VIKUNJA_API_TOKEN` | Yes | Vikunja API token |
| `ASPNETCORE_URLS` | No | Server URLs (default: `http://+:5082`) |

### Docker Compose Configuration

The `docker-compose.yml` includes:
- **PostgreSQL 18 Alpine** - Lightweight database
- **Garnet** - High-performance Redis-compatible cache
- **MinIO** - S3-compatible object storage (enabled by default)
  - Web UI: http://localhost:9001
  - API: http://localhost:9000
  - Credentials: `vikunja` / `vikunja123`
- **Vikunja Latest** - Task management system with S3 storage
- **VikunjaHook** - MCP server and webhook handler

Optional features (commented out by default):
- Email notifications (SMTP)

### Docker Image Details

The VikunjaHook Docker image is highly optimized:

| Metric | Value |
|--------|-------|
| **Final Image Size** | 30.5 MB |
| **Binary Size (uncompressed)** | 20.2 MB |
| **Binary Size (UPX compressed)** | 6.3 MB |
| **Compression Ratio** | 68.6% reduction |
| **Base Image** | `mcr.microsoft.com/dotnet/runtime-deps:10.0-alpine` |

The image uses:
- Native AOT compilation for minimal runtime dependencies
- UPX compression with `--best --lzma` for maximum compression
- Alpine Linux for minimal base image size
- Multi-stage build to exclude build tools from final image

## Development

### Running Tests

```bash
cd src/VikunjaHook

# Set test environment
$env:VIKUNJA_API_URL = "https://your-vikunja-instance.com/api/v1"
$env:VIKUNJA_API_TOKEN = "your_token"

# Run MCP tools tests
pwsh -File test-mcp-tools.ps1

# Run API tests
pwsh -File ../../test-api.ps1
```

### Building for Production

```bash
# Build with AOT
dotnet publish -c Release -r linux-x64

# Build Docker image
docker build -t vikunja-hook:latest .
```

## Custom Webhook Handlers

Extend `WebhookHandlerBase` to create custom event handlers:

```csharp
public class MyCustomHandler : WebhookHandlerBase
{
    protected override async Task OnTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        // Your custom logic here
        var task = payload.Data.Task;
        Console.WriteLine($"New task created: {task.Title}");
    }
}
```

Register your handler in `Program.cs`:
```csharp
builder.Services.AddSingleton<IWebhookHandler, MyCustomHandler>();
```

## Performance

- **Startup Time**: < 50ms (with AOT)
- **Memory Usage**: ~30MB (idle)
- **Docker Image Size**: ~30MB (Alpine-based)
- **Request Latency**: < 10ms (p99)

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Links

- [Vikunja](https://vikunja.io/)
- [Model Context Protocol](https://modelcontextprotocol.io/)
- [.NET](https://dot.net/)
