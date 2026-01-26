# Webhook Handler 快速开始

## 3 步创建自定义 Webhook 处理器

### 步骤 1: 创建处理器类

```csharp
using VikunjaHook.Models;
using VikunjaHook.Services;

public class MyWebhookHandler : WebhookHandlerBase
{
    public MyWebhookHandler(ILogger<MyWebhookHandler> logger) : base(logger)
    {
    }

    // 只覆盖你需要的事件
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        Logger.LogInformation("新任务: {Title}", task?.Title);
        
        // 你的自定义逻辑
        // await DoSomething(task);
    }
}
```

### 步骤 2: 注册处理器

在 `Program.cs` 中找到这行：

```csharp
builder.Services.AddSingleton<IWebhookHandler, DefaultWebhookHandler>();
```

替换为：

```csharp
builder.Services.AddSingleton<IWebhookHandler, MyWebhookHandler>();
```

### 步骤 3: 运行测试

```bash
dotnet build
dotnet test
dotnet run
```

## 常用事件方法

```csharp
// 任务事件
protected override Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
protected override Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
protected override Task HandleTaskDeletedAsync(VikunjaWebhookPayload payload)

// 项目事件
protected override Task HandleProjectCreatedAsync(VikunjaWebhookPayload payload)
protected override Task HandleProjectUpdatedAsync(VikunjaWebhookPayload payload)

// 评论事件
protected override Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)

// 分配事件
protected override Task HandleTaskAssigneeCreatedAsync(VikunjaWebhookPayload payload)

// 标签事件
protected override Task HandleLabelCreatedAsync(VikunjaWebhookPayload payload)
protected override Task HandleTaskLabelCreatedAsync(VikunjaWebhookPayload payload)
```

## 访问数据

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    // 基本信息
    var eventName = payload.EventName;  // "task.created"
    var time = payload.Time;            // 事件时间

    // 任务数据
    var task = payload.Data?.Task;
    var taskId = task?.Id;
    var title = task?.Title;
    var priority = task?.Priority;
    var done = task?.Done;
    var dueDate = task?.DueDate;
    
    // 创建者
    var creator = task?.CreatedBy;
    var creatorName = creator?.Username;
    
    // 分配人员
    var assignees = task?.Assignees;
    
    // 标签
    var labels = task?.Labels;
}
```

## 注入服务

```csharp
public class MyWebhookHandler : WebhookHandlerBase
{
    private readonly IEmailService _emailService;
    private readonly IDbContext _dbContext;

    public MyWebhookHandler(
        ILogger<MyWebhookHandler> logger,
        IEmailService emailService,
        IDbContext dbContext
    ) : base(logger)
    {
        _emailService = emailService;
        _dbContext = dbContext;
    }

    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        // 使用注入的服务
        await _emailService.SendAsync(...);
        await _dbContext.SaveAsync(...);
    }
}
```

## 实用示例

### 高优先级任务通知

```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    var task = payload.Data?.Task;
    
    if (task?.Priority >= 4)
    {
        Logger.LogWarning("高优先级任务: {Title}", task.Title);
        // 发送通知
    }
}
```

### 任务完成检测

```csharp
protected override async Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
{
    var oldTask = payload.Data?.OldTask;
    var newTask = payload.Data?.Task;
    
    if (oldTask?.Done == false && newTask?.Done == true)
    {
        Logger.LogInformation("任务完成: {Title}", newTask.Title);
        // 记录统计
    }
}
```

### 评论提及通知

```csharp
protected override async Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
{
    var comment = payload.Data?.Comment?.Comment;
    
    if (comment?.Contains("@") == true)
    {
        Logger.LogInformation("评论包含提及");
        // 通知被提及的用户
    }
}
```

## 更多信息

- 📖 [完整文档](./WEBHOOK_HANDLER_GUIDE.md)
- 💡 [示例代码](./VikunjaHook/Services/CustomWebhookHandlerExample.cs)
- 📝 [更新说明](./WEBHOOK_HANDLER_UPDATE.md)
