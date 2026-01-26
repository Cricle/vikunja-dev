# Vikunja MCP Server Testing Guide

This guide explains how to test the Vikunja MCP Server.

## Prerequisites

1. .NET 10 SDK installed
2. Access to a Vikunja instance
3. Valid Vikunja API token

## Test Scripts

### 1. MCP Tools Test (`test-mcp-tools.ps1`)

Comprehensive test suite that tests all 54 MCP tools by making actual API calls to Vikunja.

**Features:**
- Tests all major tool categories
- Creates and cleans up test data
- Provides detailed test results
- Shows success rate

**Usage:**

```powershell
# Set environment variables
$env:VIKUNJA_API_URL = "https://vikunja.example.com/api/v1"
$env:VIKUNJA_API_TOKEN = "your_api_token_here"

# Run tests
cd src/VikunjaHook
.\test-mcp-tools.ps1

# Skip cleanup (keep test data)
.\test-mcp-tools.ps1 -SkipCleanup

# Or pass credentials as parameters
.\test-mcp-tools.ps1 -ApiUrl "https://vikunja.example.com/api/v1" -ApiToken "your_token"
```

**What it tests:**
- ✅ Users (3 tools): GetCurrentUser, SearchUsers, GetUser
- ✅ Projects (5 tools): List, Create, Get, Update, Delete
- ✅ Tasks (5 tools): List, Create, Get, Update, Delete
- ✅ Labels (5 tools): List, Create, Get, Update, Delete
- ✅ Task Labels (3 tools): Add, List, Remove
- ✅ Task Comments (5 tools): List, Create, Get, Update, Delete
- ✅ Teams (5 tools): List, Create, Get, Update, Delete
- ⏭️ Other tools (skipped - require additional setup)

### 2. MCP stdio Test (`test-mcp-stdio.ps1`)

Tests the MCP server in stdio mode by sending JSON-RPC messages.

**Features:**
- Tests MCP protocol communication
- Sends initialize and tools/list messages
- Captures server output and errors

**Usage:**

```powershell
# Set environment variables
$env:VIKUNJA_API_URL = "https://vikunja.example.com/api/v1"
$env:VIKUNJA_API_TOKEN = "your_api_token_here"

# Run test
cd src/VikunjaHook
.\test-mcp-stdio.ps1
```

**What it tests:**
- ✅ Server starts in MCP mode
- ✅ Accepts JSON-RPC messages
- ✅ Responds to initialize request
- ✅ Responds to tools/list request

### 3. API Test (`../../test-api.ps1`)

Tests the webhook API endpoints.

**Usage:**

```powershell
# Start the server in webhook mode
cd src/VikunjaHook/VikunjaHook
dotnet run

# In another terminal, run the test
cd ../..
.\test-api.ps1
```

**What it tests:**
- ✅ Health check endpoint
- ✅ Supported events endpoint
- ✅ Webhook endpoint

### 4. Docker Test (`../../test-docker.ps1`)

Tests the Docker build and deployment.

**Usage:**

```powershell
cd ../..
.\test-docker.ps1
```

**What it tests:**
- ✅ Docker image builds successfully
- ✅ Container starts and runs
- ✅ API endpoints are accessible
- ✅ Health check passes

## Manual Testing

### Testing MCP Mode Manually

1. Start the server:
```powershell
$env:VIKUNJA_API_URL = "https://vikunja.example.com/api/v1"
$env:VIKUNJA_API_TOKEN = "your_token"
dotnet run --project VikunjaHook/VikunjaHook.csproj -- --mcp
```

2. Send JSON-RPC messages via stdin:

**Initialize:**
```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
```

**List Tools:**
```json
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
```

**Call a Tool (GetCurrentUser):**
```json
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"GetCurrentUser","arguments":{}}}
```

### Testing Webhook Mode Manually

1. Start the server:
```powershell
dotnet run --project VikunjaHook/VikunjaHook.csproj
```

2. Test endpoints:
```powershell
# Health check
Invoke-WebRequest http://localhost:5082/health

# Supported events
Invoke-WebRequest http://localhost:5082/webhook/vikunja/events

# Send webhook
$body = @{
    eventName = "task.created"
    time = (Get-Date).ToUniversalTime().ToString("o")
} | ConvertTo-Json

Invoke-WebRequest -Uri http://localhost:5082/webhook/vikunja `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

## Test Results

After running tests, you'll see:
- Total number of tests
- Passed/Failed/Skipped counts
- Success rate percentage
- Detailed failure messages (if any)

Example output:
```
=== Test Summary ===
Total Tests:   35
Passed:        28
Failed:        0
Skipped:       7

Success Rate: 100%

✓ All tests passed!
```

## Troubleshooting

### "Build failed"
- Ensure .NET 10 SDK is installed
- Run `dotnet restore` first

### "API Error: 401 Unauthorized"
- Check your API token is valid
- Verify the API URL is correct

### "Connection refused"
- Ensure Vikunja server is running
- Check firewall settings
- Verify the URL is accessible

### "Some tests failed"
- Check the detailed error messages
- Verify your Vikunja instance supports the features
- Some features may require admin permissions

## CI/CD Integration

These tests can be integrated into CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Run MCP Tests
  env:
    VIKUNJA_API_URL: ${{ secrets.VIKUNJA_API_URL }}
    VIKUNJA_API_TOKEN: ${{ secrets.VIKUNJA_API_TOKEN }}
  run: |
    cd src/VikunjaHook
    pwsh -File test-mcp-tools.ps1
```

## Contributing

When adding new tools:
1. Add corresponding tests to `test-mcp-tools.ps1`
2. Update this documentation
3. Ensure all tests pass before submitting PR
