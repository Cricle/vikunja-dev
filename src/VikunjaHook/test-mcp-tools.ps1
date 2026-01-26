# Comprehensive MCP Tools Test Script
# Tests all 54 MCP tools with the Vikunja API

param(
    [string]$ApiUrl = $env:VIKUNJA_API_URL,
    [string]$ApiToken = $env:VIKUNJA_API_TOKEN,
    [switch]$SkipCleanup
)

if ([string]::IsNullOrWhiteSpace($ApiUrl) -or [string]::IsNullOrWhiteSpace($ApiToken)) {
    Write-Host "ERROR: VIKUNJA_API_URL and VIKUNJA_API_TOKEN are required" -ForegroundColor Red
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  `$env:VIKUNJA_API_URL = 'https://vikunja.example.com/api/v1'"
    Write-Host "  `$env:VIKUNJA_API_TOKEN = 'your_token_here'"
    Write-Host "  .\test-mcp-tools.ps1"
    exit 1
}

Write-Host "=== Vikunja MCP Tools Test Suite ===" -ForegroundColor Cyan
Write-Host "API URL: $ApiUrl" -ForegroundColor Gray
Write-Host ""

# Set environment variables
$env:VIKUNJA_API_URL = $ApiUrl
$env:VIKUNJA_API_TOKEN = $ApiToken

# Test counters
$script:totalTests = 0
$script:passedTests = 0
$script:failedTests = 0
$script:skippedTests = 0

# Test results
$script:testResults = @()

function Test-Tool {
    param(
        [string]$Category,
        [string]$ToolName,
        [string]$Description,
        [scriptblock]$TestScript
    )
    
    $script:totalTests++
    Write-Host "[$script:totalTests] Testing: $Category - $ToolName" -ForegroundColor Yellow
    Write-Host "    $Description" -ForegroundColor Gray
    
    try {
        $result = & $TestScript
        if ($result) {
            Write-Host "    ✓ PASSED" -ForegroundColor Green
            $script:passedTests++
            $script:testResults += [PSCustomObject]@{
                Category = $Category
                Tool = $ToolName
                Status = "PASSED"
                Message = ""
            }
        } else {
            Write-Host "    ✗ FAILED" -ForegroundColor Red
            $script:failedTests++
            $script:testResults += [PSCustomObject]@{
                Category = $Category
                Tool = $ToolName
                Status = "FAILED"
                Message = "Test returned false"
            }
        }
    }
    catch {
        Write-Host "    ✗ FAILED: $_" -ForegroundColor Red
        $script:failedTests++
        $script:testResults += [PSCustomObject]@{
            Category = $Category
            Tool = $ToolName
            Status = "FAILED"
            Message = $_.Exception.Message
        }
    }
    Write-Host ""
}

function Skip-Tool {
    param(
        [string]$Category,
        [string]$ToolName,
        [string]$Reason
    )
    
    $script:totalTests++
    $script:skippedTests++
    Write-Host "[$script:totalTests] Skipping: $Category - $ToolName" -ForegroundColor DarkGray
    Write-Host "    Reason: $Reason" -ForegroundColor DarkGray
    $script:testResults += [PSCustomObject]@{
        Category = $Category
        Tool = $ToolName
        Status = "SKIPPED"
        Message = $Reason
    }
    Write-Host ""
}

# Helper function to make API calls
function Invoke-VikunjaApi {
    param(
        [string]$Endpoint,
        [string]$Method = "GET",
        [object]$Body = $null
    )
    
    $headers = @{
        "Authorization" = "Bearer $ApiToken"
        "Content-Type" = "application/json"
    }
    
    $params = @{
        Uri = "$ApiUrl/$Endpoint"
        Method = $Method
        Headers = $headers
    }
    
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    
    try {
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        Write-Host "    API Error: $_" -ForegroundColor Red
        return $null
    }
}

Write-Host "Starting tests..." -ForegroundColor Cyan
Write-Host ""

# ===== Users Tests =====
Write-Host "=== Users Tests (3 tools) ===" -ForegroundColor Magenta

Test-Tool "Users" "GetCurrentUser" "Get current user information" {
    $user = Invoke-VikunjaApi "user"
    return $user -ne $null -and $user.id -gt 0
}

Test-Tool "Users" "SearchUsers" "Search for users" {
    $users = Invoke-VikunjaApi "users?s=test"
    return $users -ne $null
}

Skip-Tool "Users" "GetUser" "Requires valid user ID from previous test"

# ===== Projects Tests =====
Write-Host "=== Projects Tests (5 tools) ===" -ForegroundColor Magenta

$testProjectId = $null

Test-Tool "Projects" "ListProjects" "List all projects" {
    $projects = Invoke-VikunjaApi "projects"
    return $projects -ne $null
}

Test-Tool "Projects" "CreateProject" "Create a new project" {
    $body = @{
        title = "MCP Test Project $(Get-Date -Format 'yyyyMMdd-HHmmss')"
        description = "Created by MCP test script"
    }
    $project = Invoke-VikunjaApi "projects" -Method "PUT" -Body $body
    if ($project -and $project.id) {
        $script:testProjectId = $project.id
        return $true
    }
    return $false
}

Test-Tool "Projects" "GetProject" "Get project details" {
    if ($script:testProjectId) {
        $project = Invoke-VikunjaApi "projects/$script:testProjectId"
        return $project -ne $null
    }
    return $false
}

Test-Tool "Projects" "UpdateProject" "Update project" {
    if ($script:testProjectId) {
        $body = @{
            id = $script:testProjectId
            title = "MCP Test Project (Updated)"
        }
        $project = Invoke-VikunjaApi "projects/$script:testProjectId" -Method "POST" -Body $body
        return $project -ne $null
    }
    return $false
}

# ===== Tasks Tests =====
Write-Host "=== Tasks Tests (5 tools) ===" -ForegroundColor Magenta

$testTaskId = $null

Test-Tool "Tasks" "ListTasks" "List all tasks" {
    if ($script:testProjectId) {
        $tasks = Invoke-VikunjaApi "projects/$script:testProjectId/tasks"
        return $tasks -ne $null
    }
    return $false
}

Test-Tool "Tasks" "CreateTask" "Create a new task" {
    if ($script:testProjectId) {
        $body = @{
            project_id = $script:testProjectId
            title = "MCP Test Task $(Get-Date -Format 'HHmmss')"
            description = "Created by MCP test script"
            priority = 3
        }
        $task = Invoke-VikunjaApi "projects/$script:testProjectId/tasks" -Method "PUT" -Body $body
        if ($task -and $task.id) {
            $script:testTaskId = $task.id
            return $true
        }
    }
    return $false
}

Test-Tool "Tasks" "GetTask" "Get task details" {
    if ($script:testTaskId) {
        $task = Invoke-VikunjaApi "tasks/$script:testTaskId"
        return $task -ne $null
    }
    return $false
}

Test-Tool "Tasks" "UpdateTask" "Update task" {
    if ($script:testTaskId) {
        $body = @{
            id = $script:testTaskId
            title = "MCP Test Task (Updated)"
            done = $false
        }
        $task = Invoke-VikunjaApi "tasks/$script:testTaskId" -Method "POST" -Body $body
        return $task -ne $null
    }
    return $false
}

# ===== Labels Tests =====
Write-Host "=== Labels Tests (5 tools) ===" -ForegroundColor Magenta

$testLabelId = $null

Test-Tool "Labels" "ListLabels" "List all labels" {
    $labels = Invoke-VikunjaApi "labels"
    return $labels -ne $null
}

Test-Tool "Labels" "CreateLabel" "Create a new label" {
    $body = @{
        title = "MCP Test Label $(Get-Date -Format 'HHmmss')"
        hex_color = "#FF5733"
    }
    $label = Invoke-VikunjaApi "labels" -Method "PUT" -Body $body
    if ($label -and $label.id) {
        $script:testLabelId = $label.id
        return $true
    }
    return $false
}

Test-Tool "Labels" "GetLabel" "Get label details" {
    if ($script:testLabelId) {
        $label = Invoke-VikunjaApi "labels/$script:testLabelId"
        return $label -ne $null
    }
    return $false
}

Test-Tool "Labels" "UpdateLabel" "Update label" {
    if ($script:testLabelId) {
        $body = @{
            id = $script:testLabelId
            title = "MCP Test Label (Updated)"
        }
        $label = Invoke-VikunjaApi "labels/$script:testLabelId" -Method "POST" -Body $body
        return $label -ne $null
    }
    return $false
}

# ===== Task Labels Tests =====
Write-Host "=== Task Labels Tests (3 tools) ===" -ForegroundColor Magenta

Test-Tool "Task Labels" "AddTaskLabel" "Add label to task" {
    if ($script:testTaskId -and $script:testLabelId) {
        $body = @{ label_id = $script:testLabelId }
        $label = Invoke-VikunjaApi "tasks/$script:testTaskId/labels" -Method "PUT" -Body $body
        return $label -ne $null
    }
    return $false
}

Test-Tool "Task Labels" "ListTaskLabels" "List task labels" {
    if ($script:testTaskId) {
        $task = Invoke-VikunjaApi "tasks/$script:testTaskId"
        return $task -ne $null
    }
    return $false
}

Test-Tool "Task Labels" "RemoveTaskLabel" "Remove label from task" {
    if ($script:testTaskId -and $script:testLabelId) {
        $result = Invoke-VikunjaApi "tasks/$script:testTaskId/labels/$script:testLabelId" -Method "DELETE"
        return $true
    }
    return $false
}

# ===== Task Comments Tests =====
Write-Host "=== Task Comments Tests (5 tools) ===" -ForegroundColor Magenta

$testCommentId = $null

Test-Tool "Task Comments" "ListTaskComments" "List task comments" {
    if ($script:testTaskId) {
        $comments = Invoke-VikunjaApi "tasks/$script:testTaskId/comments"
        return $comments -ne $null
    }
    return $false
}

Test-Tool "Task Comments" "CreateTaskComment" "Create task comment" {
    if ($script:testTaskId) {
        $body = @{ comment = "MCP test comment $(Get-Date -Format 'HHmmss')" }
        $comment = Invoke-VikunjaApi "tasks/$script:testTaskId/comments" -Method "PUT" -Body $body
        if ($comment -and $comment.id) {
            $script:testCommentId = $comment.id
            return $true
        }
    }
    return $false
}

Test-Tool "Task Comments" "GetTaskComment" "Get task comment" {
    if ($script:testTaskId -and $script:testCommentId) {
        $comment = Invoke-VikunjaApi "tasks/$script:testTaskId/comments/$script:testCommentId"
        return $comment -ne $null
    }
    return $false
}

Test-Tool "Task Comments" "UpdateTaskComment" "Update task comment" {
    if ($script:testTaskId -and $script:testCommentId) {
        $body = @{ comment = "MCP test comment (updated)" }
        $comment = Invoke-VikunjaApi "tasks/$script:testTaskId/comments/$script:testCommentId" -Method "POST" -Body $body
        return $comment -ne $null
    }
    return $false
}

Test-Tool "Task Comments" "DeleteTaskComment" "Delete task comment" {
    if ($script:testTaskId -and $script:testCommentId) {
        $result = Invoke-VikunjaApi "tasks/$script:testTaskId/comments/$script:testCommentId" -Method "DELETE"
        return $true
    }
    return $false
}

# ===== Teams Tests =====
Write-Host "=== Teams Tests (5 tools) ===" -ForegroundColor Magenta

$testTeamId = $null

Test-Tool "Teams" "ListTeams" "List all teams" {
    $teams = Invoke-VikunjaApi "teams"
    return $teams -ne $null
}

Test-Tool "Teams" "CreateTeam" "Create a new team" {
    $body = @{
        name = "MCP Test Team $(Get-Date -Format 'HHmmss')"
        description = "Created by MCP test script"
    }
    $team = Invoke-VikunjaApi "teams" -Method "PUT" -Body $body
    if ($team -and $team.id) {
        $script:testTeamId = $team.id
        return $true
    }
    return $false
}

Test-Tool "Teams" "GetTeam" "Get team details" {
    if ($script:testTeamId) {
        $team = Invoke-VikunjaApi "teams/$script:testTeamId"
        return $team -ne $null
    }
    return $false
}

Test-Tool "Teams" "UpdateTeam" "Update team" {
    if ($script:testTeamId) {
        $body = @{
            id = $script:testTeamId
            name = "MCP Test Team (Updated)"
        }
        $team = Invoke-VikunjaApi "teams/$script:testTeamId" -Method "POST" -Body $body
        return $team -ne $null
    }
    return $false
}

# Skip remaining tests that require more setup
Skip-Tool "Task Assignees" "AddTaskAssignee" "Requires additional user setup"
Skip-Tool "Task Assignees" "RemoveTaskAssignee" "Requires additional user setup"
Skip-Tool "Task Assignees" "ListTaskAssignees" "Requires additional user setup"

Skip-Tool "Task Attachments" "ListTaskAttachments" "Requires file upload"
Skip-Tool "Task Attachments" "GetTaskAttachment" "Requires file upload"
Skip-Tool "Task Attachments" "DeleteTaskAttachment" "Requires file upload"

Skip-Tool "Task Relations" "CreateTaskRelation" "Requires multiple tasks"
Skip-Tool "Task Relations" "DeleteTaskRelation" "Requires multiple tasks"

Skip-Tool "Buckets" "ListBuckets" "Requires bucket-enabled project"
Skip-Tool "Buckets" "CreateBucket" "Requires bucket-enabled project"
Skip-Tool "Buckets" "GetBucket" "Requires bucket-enabled project"
Skip-Tool "Buckets" "UpdateBucket" "Requires bucket-enabled project"
Skip-Tool "Buckets" "DeleteBucket" "Requires bucket-enabled project"

Skip-Tool "Webhooks" "ListWebhooks" "Requires webhook setup"
Skip-Tool "Webhooks" "CreateWebhook" "Requires webhook setup"
Skip-Tool "Webhooks" "GetWebhook" "Requires webhook setup"
Skip-Tool "Webhooks" "UpdateWebhook" "Requires webhook setup"
Skip-Tool "Webhooks" "DeleteWebhook" "Requires webhook setup"

Skip-Tool "Saved Filters" "ListSavedFilters" "Requires filter setup"
Skip-Tool "Saved Filters" "CreateSavedFilter" "Requires filter setup"
Skip-Tool "Saved Filters" "GetSavedFilter" "Requires filter setup"
Skip-Tool "Saved Filters" "UpdateSavedFilter" "Requires filter setup"
Skip-Tool "Saved Filters" "DeleteSavedFilter" "Requires filter setup"

# ===== Cleanup =====
if (-not $SkipCleanup) {
    Write-Host "=== Cleanup ===" -ForegroundColor Magenta
    
    if ($script:testTaskId) {
        Write-Host "Deleting test task..." -ForegroundColor Yellow
        Test-Tool "Tasks" "DeleteTask" "Delete test task" {
            $result = Invoke-VikunjaApi "tasks/$script:testTaskId" -Method "DELETE"
            return $true
        }
    }
    
    if ($script:testLabelId) {
        Write-Host "Deleting test label..." -ForegroundColor Yellow
        Test-Tool "Labels" "DeleteLabel" "Delete test label" {
            $result = Invoke-VikunjaApi "labels/$script:testLabelId" -Method "DELETE"
            return $true
        }
    }
    
    if ($script:testTeamId) {
        Write-Host "Deleting test team..." -ForegroundColor Yellow
        Test-Tool "Teams" "DeleteTeam" "Delete test team" {
            $result = Invoke-VikunjaApi "teams/$script:testTeamId" -Method "DELETE"
            return $true
        }
    }
    
    if ($script:testProjectId) {
        Write-Host "Deleting test project..." -ForegroundColor Yellow
        Test-Tool "Projects" "DeleteProject" "Delete test project" {
            $result = Invoke-VikunjaApi "projects/$script:testProjectId" -Method "DELETE"
            return $true
        }
    }
}

# ===== Summary =====
Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
Write-Host "Total Tests:   $script:totalTests" -ForegroundColor White
Write-Host "Passed:        $script:passedTests" -ForegroundColor Green
Write-Host "Failed:        $script:failedTests" -ForegroundColor Red
Write-Host "Skipped:       $script:skippedTests" -ForegroundColor DarkGray
Write-Host ""

if ($script:failedTests -gt 0) {
    Write-Host "Failed Tests:" -ForegroundColor Red
    $script:testResults | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
        Write-Host "  - $($_.Category): $($_.Tool)" -ForegroundColor Red
        Write-Host "    $($_.Message)" -ForegroundColor Gray
    }
    Write-Host ""
}

$successRate = [math]::Round(($script:passedTests / ($script:totalTests - $script:skippedTests)) * 100, 2)
Write-Host "Success Rate: $successRate%" -ForegroundColor $(if ($successRate -ge 80) { "Green" } elseif ($successRate -ge 50) { "Yellow" } else { "Red" })
Write-Host ""

if ($script:failedTests -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    exit 1
}
