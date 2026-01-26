# Vikunja MCP Tools Reference

Complete list of all 54 MCP tools available in the Vikunja MCP Server.

## Tasks (5 tools)

### ListTasks
List all tasks in a project or across all projects
- `projectId` (optional): Project ID to filter tasks
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page
- `search` (optional): Search query

### CreateTask
Create a new task in a project
- `projectId` (required): Project ID where the task will be created
- `title` (required): Task title
- `description` (optional): Task description
- `dueDate` (optional): Due date in ISO 8601 format
- `priority` (optional): Priority (0-5, default: 0)

### GetTask
Get details of a specific task
- `taskId` (required): Task ID

### UpdateTask
Update an existing task
- `taskId` (required): Task ID
- `title` (optional): New title
- `description` (optional): New description
- `done` (optional): Mark as done
- `priority` (optional): New priority (0-5)
- `dueDate` (optional): New due date in ISO 8601 format

### DeleteTask
Delete a task
- `taskId` (required): Task ID

---

## Task Assignees (3 tools)

### AddTaskAssignee
Add an assignee to a task
- `taskId` (required): Task ID
- `userId` (required): User ID to assign

### RemoveTaskAssignee
Remove an assignee from a task
- `taskId` (required): Task ID
- `userId` (required): User ID to remove

### ListTaskAssignees
List all assignees of a task
- `taskId` (required): Task ID

---

## Task Comments (5 tools)

### ListTaskComments
List all comments on a task
- `taskId` (required): Task ID
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### CreateTaskComment
Create a comment on a task
- `taskId` (required): Task ID
- `comment` (required): Comment text

### GetTaskComment
Get a specific comment
- `taskId` (required): Task ID
- `commentId` (required): Comment ID

### UpdateTaskComment
Update a comment on a task
- `taskId` (required): Task ID
- `commentId` (required): Comment ID
- `comment` (required): New comment text

### DeleteTaskComment
Delete a comment from a task
- `taskId` (required): Task ID
- `commentId` (required): Comment ID

---

## Task Attachments (3 tools)

### ListTaskAttachments
List all attachments on a task
- `taskId` (required): Task ID
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### GetTaskAttachment
Get attachment information
- `taskId` (required): Task ID
- `attachmentId` (required): Attachment ID

### DeleteTaskAttachment
Delete an attachment from a task
- `taskId` (required): Task ID
- `attachmentId` (required): Attachment ID

---

## Task Relations (2 tools)

### CreateTaskRelation
Create a relation between two tasks
- `taskId` (required): Task ID
- `otherTaskId` (required): Related task ID
- `relationKind` (required): Relation kind
  - `subtask`, `parenttask`, `related`, `duplicateof`, `duplicates`
  - `blocking`, `blocked`, `precedes`, `follows`, `copiedfrom`, `copiedto`

### DeleteTaskRelation
Delete a relation between two tasks
- `taskId` (required): Task ID
- `otherTaskId` (required): Related task ID
- `relationKind` (required): Relation kind

---

## Task Labels (3 tools)

### AddTaskLabel
Add a label to a task
- `taskId` (required): Task ID
- `labelId` (required): Label ID

### RemoveTaskLabel
Remove a label from a task
- `taskId` (required): Task ID
- `labelId` (required): Label ID

### ListTaskLabels
List all labels on a task
- `taskId` (required): Task ID

---

## Projects (5 tools)

### ListProjects
List all projects
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page
- `search` (optional): Search query
- `isArchived` (optional): Filter by archived status

### CreateProject
Create a new project
- `title` (required): Project title
- `description` (optional): Project description
- `hexColor` (optional): Hex color code (e.g., #FF5733)
- `parentProjectId` (optional): Parent project ID

### GetProject
Get details of a specific project
- `projectId` (required): Project ID

### UpdateProject
Update an existing project
- `projectId` (required): Project ID
- `title` (optional): New title
- `description` (optional): New description
- `hexColor` (optional): New hex color code

### DeleteProject
Delete a project
- `projectId` (required): Project ID

---

## Labels (5 tools)

### ListLabels
List all labels
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page
- `search` (optional): Search query

### CreateLabel
Create a new label
- `title` (required): Label title
- `description` (optional): Label description
- `hexColor` (optional): Hex color code (e.g., #FF5733)

### GetLabel
Get details of a specific label
- `labelId` (required): Label ID

### UpdateLabel
Update an existing label
- `labelId` (required): Label ID
- `title` (optional): New title
- `description` (optional): New description
- `hexColor` (optional): New hex color code

### DeleteLabel
Delete a label
- `labelId` (required): Label ID

---

## Teams (5 tools)

### ListTeams
List all teams
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page
- `search` (optional): Search query

### CreateTeam
Create a new team
- `name` (required): Team name
- `description` (optional): Team description

### GetTeam
Get details of a specific team
- `teamId` (required): Team ID

### UpdateTeam
Update an existing team
- `teamId` (required): Team ID
- `name` (optional): New name
- `description` (optional): New description

### DeleteTeam
Delete a team
- `teamId` (required): Team ID

---

## Users (3 tools)

### GetCurrentUser
Get current user information
- No parameters required

### SearchUsers
Search for users
- `search` (required): Search query
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### GetUser
Get details of a specific user
- `userId` (required): User ID

---

## Buckets (5 tools)

### ListBuckets
List all buckets in a project
- `projectId` (required): Project ID
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### CreateBucket
Create a new bucket in a project
- `projectId` (required): Project ID
- `title` (required): Bucket title
- `limit` (default: 0): Bucket limit (0 for unlimited)

### GetBucket
Get details of a specific bucket
- `projectId` (required): Project ID
- `bucketId` (required): Bucket ID

### UpdateBucket
Update an existing bucket
- `projectId` (required): Project ID
- `bucketId` (required): Bucket ID
- `title` (optional): New title
- `limit` (optional): New limit

### DeleteBucket
Delete a bucket
- `projectId` (required): Project ID
- `bucketId` (required): Bucket ID

---

## Webhooks (5 tools)

### ListWebhooks
List all webhooks for a project
- `projectId` (required): Project ID
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### CreateWebhook
Create a new webhook for a project
- `projectId` (required): Project ID
- `targetUrl` (required): Target URL for webhook
- `events` (required): Comma-separated list of events
- `secret` (optional): Secret for webhook authentication

### GetWebhook
Get details of a specific webhook
- `projectId` (required): Project ID
- `webhookId` (required): Webhook ID

### UpdateWebhook
Update an existing webhook
- `projectId` (required): Project ID
- `webhookId` (required): Webhook ID
- `targetUrl` (optional): New target URL
- `events` (optional): New comma-separated list of events
- `secret` (optional): New secret

### DeleteWebhook
Delete a webhook
- `projectId` (required): Project ID
- `webhookId` (required): Webhook ID

---

## Saved Filters (5 tools)

### ListSavedFilters
List all saved filters
- `page` (default: 1): Page number
- `perPage` (default: 50): Items per page

### CreateSavedFilter
Create a new saved filter
- `title` (required): Filter title
- `filters` (required): Filter query string
- `description` (optional): Filter description

### GetSavedFilter
Get details of a specific saved filter
- `filterId` (required): Filter ID

### UpdateSavedFilter
Update an existing saved filter
- `filterId` (required): Filter ID
- `title` (optional): New title
- `filters` (optional): New filter query
- `description` (optional): New description

### DeleteSavedFilter
Delete a saved filter
- `filterId` (required): Filter ID

---

## Summary

- **Total Tools**: 54
- **Categories**: 13
- **Coverage**: Complete Vikunja API

All tools support proper error handling, logging, and cancellation tokens for robust operation.
