# Webhook Handler 使用指南

## 概述

Webhook Handler 系统提供了一个灵活的基类 `WebhookHandlerBase`，允许你轻松创建自定义的 webhook 事件处理器。

## 架构

```
IWebhookHandler (接口)
    ↑
    |
WebhookHandlerBase (抽象基类)
    ↑
    |
    ├── DefaultWebhookHandler (默认实现 - 仅日志记录)
    └── CustomWebhookHandler (你的自定义实现)
```

## 快速开始

### 1. 创建自定义处理器

```csharp
using VikunjaHook.Models;
using VikunjaHook.Services;

public class MyWebhookHandler : WebhookHandlerBase
{
    private readonly IEmailService _emailService;

    public MyWebhookHandler(
        ILogger<MyWebhookHandler> logger,
        IEmailService emailService
    ) : base(logger)
    {
        _emailService = emailService;
    }

    // 只需要覆盖你关心的事件
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        Logger.LogInformation("新任务: {Title}", task?.Title);
        
        // 你的自定义逻辑
        await _emailService.SendNotification($"新任务创建: {task?.Title}");
    }
}
```

### 2. 注册自定义处理器

在 `Program.cs` 中替换默认处理器：

```csharp
// 替换这行
// builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();

// 为这行
builder.Services.AddSingleton<IWebhookHandler, MyWebhookHandler>();
```

## 可覆盖的事件方法

### 任务事件 (Task Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTaskCreatedAsync` | 任务创建 | `Task` |
| `HandleTaskUpdatedAsync` | 任务更新 | `Task`, `OldTask` |
| `HandleTaskDeletedAsync` | 任务删除 | `Task` |

### 项目事件 (Project Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleProjectCreatedAsync` | 项目创建 | `Project` |
| `HandleProjectUpdatedAsync` | 项目更新 | `Project` |
| `HandleProjectDeletedAsync` | 项目删除 | `Project` |

### 任务分配事件 (Task Assignee Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTaskAssigneeCreatedAsync` | 任务分配给用户 | `Task`, `Assignee` |
| `HandleTaskAssigneeDeletedAsync` | 取消任务分配 | `Task`, `Assignee` |

### 评论事件 (Comment Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTaskCommentCreatedAsync` | 评论创建 | `Comment`, `Task` |
| `HandleTaskCommentUpdatedAsync` | 评论更新 | `Comment` |
| `HandleTaskCommentDeletedAsync` | 评论删除 | `Comment` |

### 附件事件 (Attachment Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTaskAttachmentCreatedAsync` | 附件上传 | `Attachment` |
| `HandleTaskAttachmentDeletedAsync` | 附件删除 | `Attachment` |

### 任务关系事件 (Task Relation Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTaskRelationCreatedAsync` | 任务关系创建 | `Relation` |
| `HandleTaskRelationDeletedAsync` | 任务关系删除 | `Relation` |

### 标签事件 (Label Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleLabelCreatedAsync` | 标签创建 | `Label` |
| `HandleLabelUpdatedAsync` | 标签更新 | `Label` |
| `HandleLabelDeletedAsync` | 标签删除 | `Label` |
| `HandleTaskLabelCreatedAsync` | 任务添加标签 | `Task`, `Label` |
| `HandleTaskLabelDeletedAsync` | 任务移除标签 | `Task`, `Label` |

### 用户事件 (User Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleUserCreatedAsync` | 用户创建 | `User` |

### 团队事件 (Team Events)

| 方法 | 触发时机 | Payload 数据 |
|------|---------|-------------|
| `HandleTeamCreatedAsync` | 团队创建 | `Team` |
| `HandleTeamUpdatedAsync` | 团队更新 | `Team` |
| `HandleTeamDeletedAsync` | 团队删除 | `Team` |
| `HandleTeamMemberAddedAsync` | 团队成员添加 | `Team`, `Member` |
| `HandleTeamMemberRemovedAsync` | 团队成员移除 | `Team`, `Member` |

### 其他方法

| 方法 | 说明 |
|------|------|
| `HandleWebhookAsync` | 主入口方法，可覆盖以添加全局逻辑 |
| `HandleUnknownEventAsync` | 处理未知事件类型 |

## 使用场景示例

### 场景 1: 高优先级任务邮件通知

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    var task = payload.Data?.Task;
    
    if (task?.Priority >= 4) // 高优先级
    {
        await _emailService.SendHighPriorityAlert(
            to: "team@example.com",
            subject: $"高优先级任务: {task.Title}",
            body: $"任务 #{task.Id} 需要立即处理"
        );
    }
}
```

### 场景 2: 任务完成统计

```csharp
protected override async Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
{
    var task = payload.Data?.Task;
    var oldTask = payload.Data?.OldTask;
    
    // 检测任务从未完成变为已完成
    if (oldTask?.Done == false && task?.Done == true)
    {
        await _analyticsService.RecordTaskCompletion(
            taskId: task.Id,
            completedBy: payload.Initiator?.Username,
            completedAt: DateTime.UtcNow
        );
    }
}
```

### 场景 3: 自动标签同步

```csharp
protected override async Task HandleLabelCreatedAsync(VikunjaWebhookPayload payload)
{
    var label = payload.Data?.Label;
    
    // 同步到外部系统（如 Jira, Trello 等）
    await _externalSystemService.CreateLabel(new ExternalLabel
    {
        Name = label.Title,
        Color = label.HexColor,
        Description = label.Description
    });
}
```

### 场景 4: 评论中的 @mention 通知

```csharp
protected override async Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
{
    var comment = payload.Data?.Comment;
    var task = payload.Data?.Task;
    
    // 提取 @username
    var mentions = Regex.Matches(comment.Comment, @"@(\w+)")
        .Select(m => m.Groups[1].Value)
        .ToList();
    
    foreach (var username in mentions)
    {
        await _notificationService.NotifyUser(
            username: username,
            message: $"你在任务 '{task.Title}' 中被提及",
            link: $"/tasks/{task.Id}"
        );
    }
}
```

### 场景 5: 项目创建时自动初始化

```csharp
protected override async Task HandleProjectCreatedAsync(VikunjaWebhookPayload payload)
{
    var project = payload.Data?.Project;
    
    // 创建默认标签
    var defaultLabels = new[] { "Bug", "Feature", "Documentation" };
    foreach (var labelName in defaultLabels)
    {
        await _vikunjaApiService.CreateLabel(project.Id, labelName);
    }
    
    // 创建默认任务模板
    await _vikunjaApiService.CreateTask(project.Id, new
    {
        Title = "项目启动会议",
        Description = "讨论项目目标和里程碑",
        Priority = 3
    });
}
```

### 场景 6: 全局事件日志和监控

```csharp
public override async Task HandleWebhookAsync(VikunjaWebhookPayload payload)
{
    // 记录所有事件到数据库
    await _eventLogService.LogEvent(new EventLog
    {
        EventType = payload.EventName,
        Timestamp = payload.Time,
        Initiator = payload.Initiator?.Username,
        Data = JsonSerializer.Serialize(payload.Data)
    });
    
    // 发送到监控系统
    _metricsService.IncrementCounter($"webhook.{payload.EventName}");
    
    // 调用基类处理
    await base.HandleWebhookAsync(payload);
    
    // 事件处理后的清理工作
    Logger.LogDebug("事件 {EventName} 处理完成", payload.EventName);
}
```

## 最佳实践

### 1. 异步处理

所有方法都是异步的，充分利用 `async/await`：

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    // ✅ 好的做法
    await _emailService.SendAsync(...);
    await _databaseService.SaveAsync(...);
    
    // ❌ 避免阻塞调用
    // _emailService.Send(...); // 同步调用
}
```

### 2. 错误处理

添加适当的错误处理：

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    try
    {
        await ProcessTask(payload.Data?.Task);
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "处理任务创建事件失败: {TaskId}", payload.Data?.Task?.Id);
        // 决定是否重新抛出异常
        // throw; // 如果需要让 webhook 返回错误状态
    }
}
```

### 3. 使用 Logger

基类提供了 `Logger` 属性：

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    Logger.LogInformation("处理任务创建: {TaskId}", payload.Data?.Task?.Id);
    Logger.LogDebug("任务详情: {@Task}", payload.Data?.Task);
    Logger.LogWarning("任务优先级过高: {Priority}", payload.Data?.Task?.Priority);
}
```

### 4. 依赖注入

充分利用 DI 容器：

```csharp
public class MyWebhookHandler : WebhookHandlerBase
{
    private readonly IEmailService _emailService;
    private readonly IDbContext _dbContext;
    private readonly IConfiguration _configuration;
    
    public MyWebhookHandler(
        ILogger<MyWebhookHandler> logger,
        IEmailService emailService,
        IDbContext dbContext,
        IConfiguration configuration
    ) : base(logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
        _configuration = configuration;
    }
}
```

### 5. 选择性覆盖

只覆盖你需要的方法，其他方法使用基类的空实现：

```csharp
public class SimpleWebhookHandler : WebhookHandlerBase
{
    public SimpleWebhookHandler(ILogger<SimpleWebhookHandler> logger) : base(logger) { }
    
    // 只关心任务创建和更新
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        // 你的逻辑
    }
    
    protected override async Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
    {
        // 你的逻辑
    }
    
    // 其他事件使用基类的空实现（什么都不做）
}
```

## 测试

### 单元测试示例

```csharp
public class MyWebhookHandlerTests
{
    [Fact]
    public async Task HandleTaskCreated_HighPriority_SendsEmail()
    {
        // Arrange
        var logger = Mock.Of<ILogger<MyWebhookHandler>>();
        var emailService = new Mock<IEmailService>();
        var handler = new MyWebhookHandler(logger, emailService.Object);
        
        var payload = new VikunjaWebhookPayload
        {
            EventName = VikunjaEventTypes.TaskCreated,
            Data = new VikunjaWebhookData
            {
                Task = new VikunjaTask
                {
                    Id = 1,
                    Title = "紧急任务",
                    Priority = 5
                }
            }
        };
        
        // Act
        await handler.HandleWebhookAsync(payload);
        
        // Assert
        emailService.Verify(
            x => x.SendHighPriorityAlert(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once
        );
    }
}
```

## 参考

- 查看 `CustomWebhookHandlerExample.cs` 获取更多示例
- 查看 `DefaultWebhookHandler.cs` 了解默认实现
- 查看 `WebhookHandlerBase.cs` 了解基类结构
- 查看 `Models/VikunjaWebhookPayload.cs` 了解数据结构

## 常见问题

### Q: 如何同时使用多个处理器？

A: 创建一个组合处理器：

```csharp
public class CompositeWebhookHandler : IWebhookHandler
{
    private readonly IEnumerable<IWebhookHandler> _handlers;
    
    public CompositeWebhookHandler(IEnumerable<IWebhookHandler> handlers)
    {
        _handlers = handlers;
    }
    
    public async Task HandleWebhookAsync(VikunjaWebhookPayload payload)
    {
        foreach (var handler in _handlers)
        {
            await handler.HandleWebhookAsync(payload);
        }
    }
}
```

### Q: 如何访问 Vikunja API？

A: 注入 Vikunja API 客户端服务（需要自己实现）：

```csharp
public class MyWebhookHandler : WebhookHandlerBase
{
    private readonly IVikunjaApiClient _apiClient;
    
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        // 使用 API 客户端获取更多信息
        var fullTask = await _apiClient.GetTaskAsync(task.Id);
    }
}
```

### Q: 如何处理长时间运行的任务？

A: 使用后台任务队列：

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    // 快速返回，避免阻塞 webhook
    await _backgroundJobQueue.QueueBackgroundWorkItemAsync(async token =>
    {
        await ProcessTaskInBackground(payload.Data?.Task);
    });
}
```
