# Vikunja MCP Server Comprehensive Test Script
# This script performs complete validation of all MCP server endpoints

param(
    [string]$BaseUrl = "http://localhost:5082",
    [string]$VikunjaUrl = "https://try.vikunja.io/api/v1",
    [string]$ApiToken = ""
)

$ErrorActionPreference = "Stop"
$global:TestsPassed = 0
$global:TestsFailed = 0
$global:SessionId = $null

# Helper function to validate response structure
function Assert-ResponseStructure {
    param(
        [Parameter(Mandatory=$true)]
        $Response,
        [Parameter(Mandatory=$true)]
        [string[]]$RequiredFields,
        [string]$TestName
    )
    
    foreach ($field in $RequiredFields) {
        if (-not $Response.PSObject.Properties.Name.Contains($field)) {
            Write-Host "  ✗ Missing required field: $field" -ForegroundColor Red
            $global:TestsFailed++
            return $false
        }
    }
    return $true
}

# Helper function to run a test
function Test-Endpoint {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Name,
        [Parameter(Mandatory=$true)]
        [scriptblock]$TestBlock
    )
    
    Write-Host "`n$Name" -ForegroundColor Yellow
    try {
        & $TestBlock
        $global:TestsPassed++
        Write-Host "  ✓ Test passed" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "  ✗ Test failed: $_" -ForegroundColor Red
        Write-Host "  Error details: $($_.Exception.Message)" -ForegroundColor DarkRed
        $global:TestsFailed++
        return $false
    }
}

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Vikunja MCP Server Comprehensive Test Suite          ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Base URL: $BaseUrl" -ForegroundColor Gray
Write-Host "Vikunja URL: $VikunjaUrl" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# SECTION 1: Server Health and Info Tests
# ============================================================================
Write-Host "═══ Section 1: Server Health and Info ═══" -ForegroundColor Cyan

Test-Endpoint "Test 1.1: Health Check Endpoint" {
    $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/health" -Method Get
    
    # Validate required fields
    $required = @("status", "timestamp", "server", "version")
    if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "Health Check")) {
        throw "Response structure validation failed"
    }
    
    # Validate field values
    if ($response.status -ne "healthy") {
        throw "Expected status 'healthy', got '$($response.status)'"
    }
    
    if ([string]::IsNullOrEmpty($response.server)) {
        throw "Server name is empty"
    }
    
    if ([string]::IsNullOrEmpty($response.version)) {
        throw "Version is empty"
    }
    
    # Validate timestamp format
    try {
        [DateTime]::Parse($response.timestamp) | Out-Null
    } catch {
        throw "Invalid timestamp format: $($response.timestamp)"
    }
    
    Write-Host "  Status: $($response.status)" -ForegroundColor Gray
    Write-Host "  Server: $($response.server)" -ForegroundColor Gray
    Write-Host "  Version: $($response.version)" -ForegroundColor Gray
}

Test-Endpoint "Test 1.2: Server Info Endpoint" {
    $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/info" -Method Get
    
    # Validate required fields
    $required = @("name", "version", "capabilities")
    if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "Server Info")) {
        throw "Response structure validation failed"
    }
    
    if ($response.name -ne "vikunja-mcp-csharp") {
        throw "Expected name 'vikunja-mcp-csharp', got '$($response.name)'"
    }
    
    Write-Host "  Name: $($response.name)" -ForegroundColor Gray
    Write-Host "  Version: $($response.version)" -ForegroundColor Gray
}

Test-Endpoint "Test 1.3: Tools List Endpoint" {
    $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools" -Method Get
    
    # Validate required fields
    $required = @("tools", "count")
    if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "Tools List")) {
        throw "Response structure validation failed"
    }
    
    # Validate count matches array length
    if ($response.count -ne $response.tools.Count) {
        throw "Count mismatch: count=$($response.count), actual=$($response.tools.Count)"
    }
    
    # Validate expected tools exist
    $expectedTools = @("tasks", "projects", "labels", "teams", "users")
    foreach ($toolName in $expectedTools) {
        $tool = $response.tools | Where-Object { $_.name -eq $toolName }
        if (-not $tool) {
            throw "Expected tool '$toolName' not found"
        }
        
        # Validate tool structure
        if (-not $tool.description) {
            throw "Tool '$toolName' missing description"
        }
        if (-not $tool.subcommands) {
            throw "Tool '$toolName' missing subcommands"
        }
        if ($tool.subcommands.Count -eq 0) {
            throw "Tool '$toolName' has no subcommands"
        }
        
        Write-Host "  ✓ $($tool.name): $($tool.subcommands.Count) subcommands" -ForegroundColor Gray
    }
    
    Write-Host "  Total tools: $($response.count)" -ForegroundColor Gray
}

# ============================================================================
# SECTION 2: Authentication Tests
# ============================================================================
Write-Host "`n═══ Section 2: Authentication ═══" -ForegroundColor Cyan

if ([string]::IsNullOrEmpty($ApiToken)) {
    Write-Host "`nSkipping authentication tests (no API token provided)" -ForegroundColor Yellow
    Write-Host "To run full tests, provide: -ApiToken 'your-token'" -ForegroundColor Gray
} else {
    Test-Endpoint "Test 2.1: Authentication with Valid Token" {
        $authBody = @{
            apiUrl = $VikunjaUrl
            apiToken = $ApiToken
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/auth" -Method Post -Body $authBody -ContentType "application/json"
        
        # Validate required fields
        $required = @("sessionId", "authType")
        if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "Authentication")) {
            throw "Response structure validation failed"
        }
        
        # Validate session ID is not empty
        if ([string]::IsNullOrEmpty($response.sessionId)) {
            throw "Session ID is empty"
        }
        
        # Validate auth type
        $validAuthTypes = @("ApiToken", "Jwt")
        if ($response.authType -notin $validAuthTypes) {
            throw "Invalid auth type: $($response.authType). Expected: $($validAuthTypes -join ', ')"
        }
        
        # Store session ID for subsequent tests
        $global:SessionId = $response.sessionId
        
        Write-Host "  Session ID: $($response.sessionId.Substring(0, 16))..." -ForegroundColor Gray
        Write-Host "  Auth Type: $($response.authType)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 2.2: Authentication with Invalid Token" {
        $authBody = @{
            apiUrl = $VikunjaUrl
            apiToken = "invalid-token-12345"
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/auth" -Method Post -Body $authBody -ContentType "application/json"
            throw "Expected authentication to fail with invalid token"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 401) {
                throw "Expected 401 Unauthorized, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected invalid token" -ForegroundColor Gray
        }
    }
    
    Test-Endpoint "Test 2.3: Authentication with Missing Fields" {
        $authBody = @{
            apiUrl = $VikunjaUrl
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/auth" -Method Post -Body $authBody -ContentType "application/json"
            throw "Expected authentication to fail with missing apiToken"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 400) {
                throw "Expected 400 Bad Request, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected missing fields" -ForegroundColor Gray
        }
    }
}

# ============================================================================
# SECTION 3: Tool Invocation Tests (Requires Authentication)
# ============================================================================
if ($global:SessionId) {
    Write-Host "`n═══ Section 3: Projects Tool ═══" -ForegroundColor Cyan
    
    $headers = @{
        "Authorization" = "Bearer $global:SessionId"
    }
    
    Test-Endpoint "Test 3.1: List Projects" {
        $body = @{
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/list" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        # Validate response structure
        $required = @("success", "tool", "subcommand", "data")
        if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "List Projects")) {
            throw "Response structure validation failed"
        }
        
        if ($response.success -ne $true) {
            throw "Expected success=true, got $($response.success)"
        }
        
        if ($response.tool -ne "projects") {
            throw "Expected tool='projects', got '$($response.tool)'"
        }
        
        if ($response.subcommand -ne "list") {
            throw "Expected subcommand='list', got '$($response.subcommand)'"
        }
        
        # Validate data structure
        $dataRequired = @("projects", "count", "page", "perPage")
        if (-not (Assert-ResponseStructure -Response $response.data -RequiredFields $dataRequired -TestName "List Projects Data")) {
            throw "Data structure validation failed"
        }
        
        Write-Host "  Projects found: $($response.data.count)" -ForegroundColor Gray
        Write-Host "  Page: $($response.data.page)/$($response.data.perPage)" -ForegroundColor Gray
    }
    
    Write-Host "`n═══ Section 4: Tasks Tool ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 4.1: List Tasks" {
        $body = @{
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        # Validate response structure
        $required = @("success", "tool", "subcommand", "data")
        if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "List Tasks")) {
            throw "Response structure validation failed"
        }
        
        if ($response.tool -ne "tasks") {
            throw "Expected tool='tasks', got '$($response.tool)'"
        }
        
        # Validate data structure
        $dataRequired = @("tasks", "count", "page", "perPage")
        if (-not (Assert-ResponseStructure -Response $response.data -RequiredFields $dataRequired -TestName "List Tasks Data")) {
            throw "Data structure validation failed"
        }
        
        Write-Host "  Tasks found: $($response.data.count)" -ForegroundColor Gray
    }
    
    Write-Host "`n═══ Section 5: Labels Tool ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 5.1: List Labels" {
        $body = @{
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/labels/list" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        # Validate response structure
        if ($response.tool -ne "labels") {
            throw "Expected tool='labels', got '$($response.tool)'"
        }
        
        $dataRequired = @("labels", "count", "page", "perPage")
        if (-not (Assert-ResponseStructure -Response $response.data -RequiredFields $dataRequired -TestName "List Labels Data")) {
            throw "Data structure validation failed"
        }
        
        Write-Host "  Labels found: $($response.data.count)" -ForegroundColor Gray
    }
    
    Write-Host "`n═══ Section 6: Teams Tool ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 6.1: List Teams" {
        $body = @{
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/teams/list" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        # Validate response structure
        if ($response.tool -ne "teams") {
            throw "Expected tool='teams', got '$($response.tool)'"
        }
        
        $dataRequired = @("teams", "count", "page", "perPage")
        if (-not (Assert-ResponseStructure -Response $response.data -RequiredFields $dataRequired -TestName "List Teams Data")) {
            throw "Data structure validation failed"
        }
        
        Write-Host "  Teams found: $($response.data.count)" -ForegroundColor Gray
    }
    
    Write-Host "`n═══ Section 7: Error Handling ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 7.1: Invalid Tool Name" {
        $body = @{} | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/invalid-tool/list" -Method Post -Headers $headers -Body $body -ContentType "application/json"
            throw "Expected request to fail with invalid tool name"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 404) {
                throw "Expected 404 Not Found, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected invalid tool" -ForegroundColor Gray
        }
    }
    
    Test-Endpoint "Test 7.2: Invalid Subcommand" {
        $body = @{} | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/invalid-subcommand" -Method Post -Headers $headers -Body $body -ContentType "application/json"
            throw "Expected request to fail with invalid subcommand"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 400) {
                throw "Expected 400 Bad Request, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected invalid subcommand" -ForegroundColor Gray
        }
    }
    
    Test-Endpoint "Test 7.3: Missing Required Parameters" {
        $body = @{} | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/get" -Method Post -Headers $headers -Body $body -ContentType "application/json"
            throw "Expected request to fail with missing required parameter"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 400) {
                throw "Expected 400 Bad Request, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected missing parameters" -ForegroundColor Gray
        }
    }
    
    Test-Endpoint "Test 7.4: Unauthorized Access (No Token)" {
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list" -Method Post -Body "{}" -ContentType "application/json"
            throw "Expected request to fail without authorization"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 401) {
                throw "Expected 401 Unauthorized, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected unauthorized request" -ForegroundColor Gray
        }
    }
    
    Test-Endpoint "Test 7.5: Invalid Session ID" {
        $invalidHeaders = @{
            "Authorization" = "Bearer invalid-session-id-12345"
        }
        $body = @{
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        try {
            $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list" -Method Post -Headers $invalidHeaders -Body $body -ContentType "application/json"
            throw "Expected request to fail with invalid session"
        } catch {
            if ($_.Exception.Response.StatusCode -ne 401) {
                throw "Expected 401 Unauthorized, got $($_.Exception.Response.StatusCode)"
            }
            Write-Host "  Correctly rejected invalid session" -ForegroundColor Gray
        }
    }
}

# ============================================================================
# SECTION 8: MCP Protocol Tests
# ============================================================================
if ($global:SessionId) {
    Write-Host "`n═══ Section 8: MCP Protocol ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 8.1: MCP Request Endpoint" {
        $mcpRequest = @{
            sessionId = $global:SessionId
            tool = "projects"
            subcommand = "list"
            arguments = @{
                page = 1
                perPage = 5
            }
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/request" -Method Post -Body $mcpRequest -ContentType "application/json"
        
        # Validate MCP response structure
        $required = @("success", "operation", "data")
        if (-not (Assert-ResponseStructure -Response $response -RequiredFields $required -TestName "MCP Request")) {
            throw "Response structure validation failed"
        }
        
        if ($response.success -ne $true) {
            throw "Expected success=true, got $($response.success)"
        }
        
        if ($response.operation -ne "projects.list") {
            throw "Expected operation='projects.list', got '$($response.operation)'"
        }
        
        Write-Host "  Operation: $($response.operation)" -ForegroundColor Gray
        Write-Host "  Success: $($response.success)" -ForegroundColor Gray
    }
    
    # ============================================================================
    # SECTION 9: Tasks Tool - All Subcommands (22 total)
    # ============================================================================
    Write-Host "`n═══ Section 9: Tasks Tool - All Subcommands ═══" -ForegroundColor Cyan
    
    $global:TestTaskId = $null
    $global:TestCommentId = $null
    $global:TestReminderId = $null
    
    # Create a test task for subsequent operations
    Test-Endpoint "Test 9.1: Tasks - Create Task" {
        $body = @{
            title = "Test Task $(Get-Date -Format 'yyyyMMdd-HHmmss')"
            description = "Created by comprehensive test script"
            priority = 3
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/create" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.success -ne $true) {
            throw "Failed to create task"
        }
        
        if (-not $response.data.task.id) {
            throw "Created task missing ID"
        }
        
        $global:TestTaskId = $response.data.task.id
        Write-Host "  Created task ID: $global:TestTaskId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.2: Tasks - Get Task by ID" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/get" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.task.id -ne $global:TestTaskId) {
            throw "Retrieved task ID mismatch"
        }
        
        Write-Host "  Retrieved task: $($response.data.task.title)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.3: Tasks - Update Task" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
            title = "Updated Test Task"
            priority = 5
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/update" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.task.priority -ne 5) {
            throw "Task priority not updated"
        }
        
        Write-Host "  Updated task priority to 5" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.4: Tasks - Add Comment" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
            comment = "Test comment from comprehensive test"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/comment" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if (-not $response.data.comment.id) {
            throw "Comment not created"
        }
        
        $global:TestCommentId = $response.data.comment.id
        Write-Host "  Added comment ID: $global:TestCommentId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.5: Tasks - List Comments" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list-comments" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.comments.Count -eq 0) {
            throw "No comments found"
        }
        
        Write-Host "  Found $($response.data.comments.Count) comment(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.6: Tasks - Add Reminder" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $reminderDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ")
        $body = @{
            taskId = $global:TestTaskId
            reminder = $reminderDate
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/add-reminder" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if (-not $response.data.reminder.id) {
            throw "Reminder not created"
        }
        
        $global:TestReminderId = $response.data.reminder.id
        Write-Host "  Added reminder ID: $global:TestReminderId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.7: Tasks - List Reminders" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list-reminders" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.reminders.Count -eq 0) {
            throw "No reminders found"
        }
        
        Write-Host "  Found $($response.data.reminders.Count) reminder(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.8: Tasks - List Assignees" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list-assignees" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Found $($response.data.assignees.Count) assignee(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.9: Tasks - List Task Labels" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/list-labels" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Found $($response.data.labels.Count) label(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.10: Tasks - List Relations" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/relations" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Found $($response.data.relations.Count) relation(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.11: Tasks - Remove Reminder" {
        if (-not $global:TestTaskId -or -not $global:TestReminderId) { 
            Write-Host "  Skipping - no reminder to remove" -ForegroundColor Yellow
            return
        }
        
        $body = @{
            taskId = $global:TestTaskId
            reminderId = $global:TestReminderId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/remove-reminder" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Removed reminder ID: $global:TestReminderId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 9.12: Tasks - Delete Task" {
        if (-not $global:TestTaskId) { throw "No test task ID available" }
        
        $body = @{
            taskId = $global:TestTaskId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/tasks/delete" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Deleted task ID: $global:TestTaskId" -ForegroundColor Gray
    }
    
    # ============================================================================
    # SECTION 10: Projects Tool - All Subcommands (11 total)
    # ============================================================================
    Write-Host "`n═══ Section 10: Projects Tool - All Subcommands ═══" -ForegroundColor Cyan
    
    $global:TestProjectId = $null
    
    Test-Endpoint "Test 10.1: Projects - Create Project" {
        $body = @{
            title = "Test Project $(Get-Date -Format 'yyyyMMdd-HHmmss')"
            description = "Created by comprehensive test script"
            hexColor = "#FF5733"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/create" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.success -ne $true) {
            throw "Failed to create project"
        }
        
        if (-not $response.data.project.id) {
            throw "Created project missing ID"
        }
        
        $global:TestProjectId = $response.data.project.id
        Write-Host "  Created project ID: $global:TestProjectId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.2: Projects - Get Project by ID" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/get" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.project.id -ne $global:TestProjectId) {
            throw "Retrieved project ID mismatch"
        }
        
        Write-Host "  Retrieved project: $($response.data.project.title)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.3: Projects - Update Project" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
            title = "Updated Test Project"
            hexColor = "#33FF57"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/update" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Updated project color to #33FF57" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.4: Projects - Get Children" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/get-children" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Found $($response.data.children.Count) child project(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.5: Projects - Get Tree" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/get-tree" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Retrieved project tree" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.6: Projects - Get Breadcrumb" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/get-breadcrumb" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Breadcrumb depth: $($response.data.breadcrumb.Count)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.7: Projects - Archive Project" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/archive" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Archived project ID: $global:TestProjectId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.8: Projects - Unarchive Project" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/unarchive" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Unarchived project ID: $global:TestProjectId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 10.9: Projects - Delete Project" {
        if (-not $global:TestProjectId) { throw "No test project ID available" }
        
        $body = @{
            projectId = $global:TestProjectId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/projects/delete" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Deleted project ID: $global:TestProjectId" -ForegroundColor Gray
    }
    
    # ============================================================================
    # SECTION 11: Labels Tool - All Subcommands (5 total)
    # ============================================================================
    Write-Host "`n═══ Section 11: Labels Tool - All Subcommands ═══" -ForegroundColor Cyan
    
    $global:TestLabelId = $null
    
    Test-Endpoint "Test 11.1: Labels - Create Label" {
        $body = @{
            title = "Test Label $(Get-Date -Format 'yyyyMMdd-HHmmss')"
            description = "Created by comprehensive test script"
            hexColor = "#FF33A1"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/labels/create" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.success -ne $true) {
            throw "Failed to create label"
        }
        
        if (-not $response.data.label.id) {
            throw "Created label missing ID"
        }
        
        $global:TestLabelId = $response.data.label.id
        Write-Host "  Created label ID: $global:TestLabelId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 11.2: Labels - Get Label by ID" {
        if (-not $global:TestLabelId) { throw "No test label ID available" }
        
        $body = @{
            labelId = $global:TestLabelId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/labels/get" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.data.label.id -ne $global:TestLabelId) {
            throw "Retrieved label ID mismatch"
        }
        
        Write-Host "  Retrieved label: $($response.data.label.title)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 11.3: Labels - Update Label" {
        if (-not $global:TestLabelId) { throw "No test label ID available" }
        
        $body = @{
            labelId = $global:TestLabelId
            title = "Updated Test Label"
            hexColor = "#A133FF"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/labels/update" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Updated label color to #A133FF" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 11.4: Labels - Delete Label" {
        if (-not $global:TestLabelId) { throw "No test label ID available" }
        
        $body = @{
            labelId = $global:TestLabelId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/labels/delete" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Deleted label ID: $global:TestLabelId" -ForegroundColor Gray
    }
    
    # ============================================================================
    # SECTION 12: Teams Tool - All Subcommands (3 total)
    # ============================================================================
    Write-Host "`n═══ Section 12: Teams Tool - All Subcommands ═══" -ForegroundColor Cyan
    
    $global:TestTeamId = $null
    
    Test-Endpoint "Test 12.1: Teams - Create Team" {
        $body = @{
            name = "Test Team $(Get-Date -Format 'yyyyMMdd-HHmmss')"
            description = "Created by comprehensive test script"
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/teams/create" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if ($response.success -ne $true) {
            throw "Failed to create team"
        }
        
        if (-not $response.data.team.id) {
            throw "Created team missing ID"
        }
        
        $global:TestTeamId = $response.data.team.id
        Write-Host "  Created team ID: $global:TestTeamId" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 12.2: Teams - Delete Team" {
        if (-not $global:TestTeamId) { throw "No test team ID available" }
        
        $body = @{
            teamId = $global:TestTeamId
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/teams/delete" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Deleted team ID: $global:TestTeamId" -ForegroundColor Gray
    }
    
    # ============================================================================
    # SECTION 13: Users Tool - All Subcommands (4 total)
    # ============================================================================
    Write-Host "`n═══ Section 13: Users Tool - All Subcommands ═══" -ForegroundColor Cyan
    
    Test-Endpoint "Test 13.1: Users - Get Current User" {
        $body = @{} | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/users/current" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        if (-not $response.data.user.id) {
            throw "Current user missing ID"
        }
        
        Write-Host "  Current user: $($response.data.user.username)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 13.2: Users - Search Users" {
        $body = @{
            query = "test"
            page = 1
            perPage = 10
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/users/search" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Found $($response.data.users.Count) user(s)" -ForegroundColor Gray
    }
    
    Test-Endpoint "Test 13.3: Users - Get Settings" {
        $body = @{} | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/mcp/tools/users/settings" -Method Post -Headers $headers -Body $body -ContentType "application/json"
        
        Write-Host "  Retrieved user settings" -ForegroundColor Gray
    }
}

# ============================================================================
# Test Summary
# ============================================================================
Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    Test Summary                           ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tests Passed: " -NoNewline
Write-Host "$global:TestsPassed" -ForegroundColor Green
Write-Host "Tests Failed: " -NoNewline
Write-Host "$global:TestsFailed" -ForegroundColor $(if ($global:TestsFailed -eq 0) { "Green" } else { "Red" })
Write-Host "Total Tests:  $($global:TestsPassed + $global:TestsFailed)" -ForegroundColor Cyan
Write-Host ""

if ($global:TestsFailed -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    exit 1
}
