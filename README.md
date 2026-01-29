# Vikunja Webhook Notification System

Webhook notification system for [Vikunja](https://vikunja.io/) with MCP server support. Built with .NET 10 AOT.

## Features

- **Webhook Notifications** - 26 event types with customizable templates
- **Task Reminders** - Auto-scan tasks by start/due/reminder times with label filtering
- **Multi-Provider** - PushDeer support (extensible)
- **Web UI** - Configure providers, templates, and reminders
- **MCP Server** - 54 tools for Vikunja API integration
- **Hot Reload** - Config changes apply without restart
- **Docker Ready** - 35MB image, full stack included

## Quick Start

```bash
git clone https://github.com/Cricle/vikunja-dev.git
cd vikunja-dev
docker-compose up -d
```

Access:
- Vikunja: http://localhost:8080
- Webhook UI: http://localhost:5082

## Configuration

### Environment Variables

```bash
VIKUNJA_API_URL=http://vikunja:3456/api/v1
VIKUNJA_API_TOKEN=your_token_here
VIKUNJA_URL=http://localhost:3456  # For task links
```

### Webhook Setup

1. In Vikunja, create webhook pointing to `http://vikunja-hook:5082/api/webhook`
2. Select events to monitor
3. Configure notifications in web UI

### Task Reminders

Configure in web UI:
- **Check Interval**: 10-300 seconds (checks memory, not API)
- **Time Window**: Past 5 min to future 5 min
- **Label Filter**: OR logic, optional
- **Templates**: Separate for start/due/end/reminder
- **Architecture**: Webhook-based memory management
  - Startup: Scans all tasks once
  - Updates: Real-time via webhook events
  - Checking: Timer checks memory every 10s
- **Duplicate Prevention**: Tracks sent reminders (2h expiry, auto-cleanup)

## Notification Templates

Available placeholders:
- Task: `{{task.id}}`, `{{task.title}}`, `{{task.description}}`, `{{task.done}}`, `{{task.priority}}`, `{{task.dueDate}}`
- Project: `{{project.id}}`, `{{project.title}}`, `{{project.description}}`
- Event: `{{event.type}}`, `{{event.timestamp}}`, `{{event.url}}`
- Special: `{{assignees}}`, `{{labels}}`, `{{comment.text}}`, `{{attachment.fileName}}`

Example:
```
Title: ⏰ {{task.title}} due soon
Body: Task: {{task.title}}
Project: {{project.title}}
Due: {{task.dueDate}}
Link: {{event.url}}
```

## Features Detail

### Webhook Events (26)
- **Task**: created, updated, deleted, assignee (add/remove), comment (add/update/delete), attachment (add/delete), relation (add/delete), label (add/delete)
- **Project**: created, updated, deleted
- **Label**: created, updated, deleted
- **Team**: created, updated, deleted, member (add/remove)
- **User**: created

### Task Reminders
- **Webhook-based memory management** for real-time updates
- Startup initialization scans all tasks once
- Timer checks memory every 10s (not API)
- Supports start date, due date, end date, reminder times
- Time window: -5min to +5min (catches past times)
- Duplicate prevention with sent reminder tracking
- Label filtering with OR logic
- Auto-cleanup of old records (2h expiry)

### MCP Tools (54)
- Tasks (5), Projects (5), Labels (5)
- Comments (5), Attachments (3), Relations (2)
- Assignees (3), Teams (5), Users (3)
- Buckets (5), Webhooks (5), Filters (5)

## API Endpoints

```
POST /api/webhook                      - Receive webhooks
GET  /api/webhook-config/{userId}      - Get config
PUT  /api/webhook-config/{userId}      - Update config
GET  /api/push-history                 - Push history
GET  /api/reminder-history             - Reminder history
GET  /api/reminder-status              - Reminder status (memory state)
GET  /api/mcp/labels                   - Get labels
POST /mcp                              - MCP protocol
GET  /health                           - Health check
```

## Development

```bash
# Build
cd src/VikunjaHook
dotnet build -c Release

# Run
dotnet run -c Release

# Test
pwsh test-webhook-dev.ps1
```

## Performance

- Startup: < 50ms
- Memory: ~30MB idle
- Image: 35MB (Alpine + AOT)
- No CDN dependencies (fonts bundled)

## License

MIT
