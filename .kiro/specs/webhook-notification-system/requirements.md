# Requirements Document

## Introduction

本系统为 Vikunja 提供一个可扩展的 webhook 通知系统，支持多种推送渠道（如 PushDeer），并通过 Web 界面让用户自主配置项目级别的 webhook 事件通知。系统采用 JSON 文件存储用户配置，便于备份和恢复，并复用现有的 MCP 工具接口避免重复开发。

## Glossary

- **System**: Webhook 通知系统
- **PushDeer**: 一个开源的消息推送服务
- **Notification_Provider**: 通知提供者，如 PushDeer、Email、Telegram 等
- **User_Config**: 用户配置，存储在 JSON 文件中
- **Webhook_Event**: Vikunja 产生的 webhook 事件（任务创建、更新、删除等）
- **MCP_Tools**: 现有的 Model Context Protocol 工具接口
- **Web_UI**: 基于 Vue3 + TypeScript + Vuestic 的 Web 管理界面
- **Template_Placeholder**: 模板占位符，用于在通知标题和正文中动态插入事件数据
- **AOT**: Ahead-of-Time 编译，.NET 的原生编译模式

## Requirements

### Requirement 1: 可扩展的通知提供者架构

**User Story:** 作为系统架构师，我希望系统支持多种通知渠道，以便用户可以选择适合自己的推送方式。

#### Acceptance Criteria

1. THE System SHALL define a Notification_Provider interface that supports multiple notification channels
2. WHEN a new Notification_Provider is implemented, THE System SHALL register it without modifying existing code
3. THE System SHALL support PushDeer as the initial Notification_Provider
4. WHERE additional providers are needed, THE System SHALL allow registration of Email, Telegram, Slack, and custom providers
5. WHEN a Notification_Provider sends a message, THE System SHALL return success or failure status

### Requirement 2: PushDeer 通知集成

**User Story:** 作为用户，我希望通过 PushDeer 接收 Vikunja 的任务通知，以便及时了解项目动态。

#### Acceptance Criteria

1. WHEN a user configures PushDeer credentials, THE System SHALL validate the PushDeer API key
2. WHEN a Webhook_Event occurs, THE System SHALL send notification with user-defined title and body
3. THE System SHALL support PushDeer text and markdown message formats
4. IF PushDeer API returns an error, THEN THE System SHALL log the error and retry up to 3 times
5. WHEN a notification is sent successfully, THE System SHALL record the timestamp in User_Config

### Requirement 3: 用户配置存储

**User Story:** 作为用户，我希望我的配置存储在独立的 JSON 文件中，以便轻松备份和恢复配置。

#### Acceptance Criteria

1. THE System SHALL store each user's configuration in a separate JSON file named `{userId}.json`
2. WHEN a user updates their configuration, THE System SHALL write changes to the JSON file immediately
3. THE System SHALL validate JSON structure before writing to prevent corruption
4. WHEN the System starts, THE System SHALL load all user configurations from JSON files
5. IF a JSON file is corrupted, THEN THE System SHALL log the error and use default configuration
6. THE System SHALL support atomic file writes to prevent data loss during updates

### Requirement 4: 项目级别的 Webhook 配置

**User Story:** 作为项目管理者，我希望为不同项目配置不同的通知规则，以便精细控制通知行为。

#### Acceptance Criteria

1. WHEN a user configures notifications, THE System SHALL allow per-project webhook settings
2. THE System SHALL support enabling or disabling specific Webhook_Event types per project
3. WHEN a Webhook_Event occurs, THE System SHALL check project-specific rules before sending notifications
4. THE System SHALL support wildcard rules for all projects or specific project IDs
5. WHEN no project-specific rule exists, THE System SHALL fall back to user-level default settings

### Requirement 5: Web 界面管理

**User Story:** 作为用户，我希望通过美观的 Web 界面管理我的通知配置，以便直观地控制推送事件和自定义通知内容。

#### Acceptance Criteria

1. THE Web_UI SHALL be built using Vue3, TypeScript, and Vuestic UI framework
2. THE Web_UI SHALL follow VSCode official website design principles for visual aesthetics
3. THE Web_UI SHALL adopt Grafana-style configuration patterns for intuitive user experience
4. WHEN a user accesses the Web_UI, THE System SHALL display all configured projects and notification settings
5. THE Web_UI SHALL allow users to add, edit, and delete Notification_Provider configurations
6. THE Web_UI SHALL provide toggles for enabling/disabling specific Webhook_Event types per project
7. THE Web_UI SHALL include a template editor with syntax highlighting for notification templates
8. THE Web_UI SHALL provide a live preview of notification output with sample data
9. WHEN a user saves configuration changes, THE Web_UI SHALL call the backend API to persist changes
10. THE Web_UI SHALL display real-time validation errors for invalid configurations
11. THE Web_UI SHALL support testing notifications by sending a test message to configured providers

### Requirement 6: 复用 MCP 工具接口

**User Story:** 作为开发者，我希望复用现有的 MCP 工具接口获取项目和任务数据，以避免重复开发。

#### Acceptance Criteria

1. THE System SHALL use existing MCP_Tools to retrieve project lists
2. THE System SHALL use existing MCP_Tools to retrieve task details
3. THE System SHALL use existing MCP_Tools to retrieve user information
4. WHEN formatting notifications, THE System SHALL call MCP_Tools to enrich event data
5. THE System SHALL NOT duplicate API client code that already exists in MCP_Tools

### Requirement 7: 通知模板和占位符

**User Story:** 作为用户，我希望使用占位符自定义通知的标题和正文，以便根据不同事件类型发送个性化的通知内容。

#### Acceptance Criteria

1. THE System SHALL support Template_Placeholder in notification title and body
2. THE System SHALL provide placeholders for task properties: `{{task.title}}`, `{{task.description}}`, `{{task.id}}`, `{{task.done}}`
3. THE System SHALL provide placeholders for project properties: `{{project.title}}`, `{{project.id}}`, `{{project.description}}`
4. THE System SHALL provide placeholders for user properties: `{{user.name}}`, `{{user.username}}`, `{{user.email}}`
5. THE System SHALL provide placeholders for event metadata: `{{event.type}}`, `{{event.timestamp}}`, `{{event.url}}`
6. THE System SHALL provide placeholders for assignees: `{{assignees}}`, `{{assignee.count}}`
7. THE System SHALL provide placeholders for labels: `{{labels}}`, `{{label.count}}`
8. WHEN a Template_Placeholder is not available in the event data, THE System SHALL replace it with an empty string
9. THE Web_UI SHALL provide a placeholder reference panel showing all available placeholders
10. THE Web_UI SHALL provide autocomplete suggestions when typing placeholders in the template editor

### Requirement 8: Webhook 事件全覆盖

**User Story:** 作为用户，我希望系统能够处理所有 Vikunja webhook 事件，以便接收全面的项目通知。

#### Acceptance Criteria

1. THE System SHALL support task.created Webhook_Event
2. THE System SHALL support task.updated Webhook_Event
3. THE System SHALL support task.deleted Webhook_Event
4. THE System SHALL support task.assigned Webhook_Event
5. THE System SHALL support task.comment.created Webhook_Event
6. THE System SHALL support task.comment.updated Webhook_Event
7. THE System SHALL support task.comment.deleted Webhook_Event
8. THE System SHALL support task.attachment.created Webhook_Event
9. THE System SHALL support task.attachment.deleted Webhook_Event
10. THE System SHALL support task.relation.created Webhook_Event
11. THE System SHALL support task.relation.deleted Webhook_Event
12. THE System SHALL support project.created Webhook_Event
13. THE System SHALL support project.updated Webhook_Event
14. THE System SHALL support project.deleted Webhook_Event
15. THE System SHALL support team.member.added Webhook_Event
16. THE System SHALL support team.member.removed Webhook_Event
17. WHEN a Webhook_Event is received, THE System SHALL parse the event payload
18. WHEN a Webhook_Event matches user configuration, THE System SHALL trigger configured Notification_Provider
19. THE System SHALL process webhook events asynchronously to avoid blocking
20. THE System SHALL provide default notification templates for each Webhook_Event type

### Requirement 9: 配置备份和恢复

**User Story:** 作为用户，我希望能够轻松备份和恢复我的通知配置，以便在迁移或灾难恢复时快速恢复服务。

#### Acceptance Criteria

1. THE System SHALL provide an API endpoint to export all user configurations as a ZIP file
2. THE System SHALL provide an API endpoint to import configurations from a ZIP file
3. WHEN exporting, THE System SHALL include all JSON configuration files and metadata
4. WHEN importing, THE System SHALL validate each configuration file before applying
5. IF an import fails validation, THEN THE System SHALL skip that file and continue with others
6. THE System SHALL log all backup and restore operations with timestamps

### Requirement 10: AOT 编译支持

**User Story:** 作为开发者，我希望系统 100% 支持 .NET AOT 编译，以便获得更快的启动速度和更小的部署体积。

#### Acceptance Criteria

1. THE System SHALL be compatible with .NET Native AOT compilation
2. THE System SHALL avoid using reflection APIs that are incompatible with AOT
3. THE System SHALL use source generators for JSON serialization instead of runtime reflection
4. THE System SHALL declare all required JSON serialization contexts at compile time
5. WHEN compiled with AOT, THE System SHALL produce a native executable
6. THE System SHALL pass all tests when compiled with AOT enabled
7. THE System SHALL document any AOT-specific configuration requirements
