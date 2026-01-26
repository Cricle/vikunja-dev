# Webhook Handler 架构更新

## 更新内容

为 Webhook Handler 系统添加了可扩展的基类架构，使得自定义 webhook 事件处理逻辑变得更加简单和灵活。

## 新增文件

### 1. `Services/WebhookHandlerBase.cs`
抽象基类，提供：
- 事件分发逻辑（将 webhook 事件路由到对应的处理方法）
- 27 个可覆盖的虚方法（对应所有 Vikunja webhook 事件类型）
- 受保护的 `Logger` 属性供子类使用
- 默认空实现（不需要覆盖所有方法）

### 2. `Services/CustomWebhookHandlerExample.cs`
完整的示例实现，展示：
- 如何继承 `WebhookHandlerBase`
- 如何注入自定义服务
- 如何覆盖特定事件方法
- 如何添加自定义业务逻辑
- 10+ 个实际使用场景示例

### 3. `WEBHOOK_HANDLER_GUIDE.md`
详细的使用指南，包含：
- 快速开始教程
- 完整的事件方法列表
- 6 个实际场景示例
- 最佳实践建议
- 单元测试示例
- 常见问题解答

## 修改文件

### `Services/DefaultWebhookHandler.cs`
- 从直接实现 `IWebhookHandler` 改为继承 `WebhookHandlerBase`
- 移除了事件分发逻辑（由基类提供）
- 所有私有方法改为受保护的虚方法覆盖
- 使用基类的 `Logger` 属性替代私有 `_logger` 字段

## 架构优势

### 1. 可扩展性
```csharp
// 只需继承基类并覆盖需要的方法
public class MyHandler : WebhookHandlerBase
{
    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        // 你的自定义逻辑
    }
}
```

### 2. 选择性实现
- 不需要实现所有 27 个事件方法
- 只覆盖你关心的事件
- 其他事件使用基类的空实现

### 3. 依赖注入友好
```csharp
public class MyHandler : WebhookHandlerBase
{
    private readonly IEmailService _emailService;
    
    public MyHandler(ILogger<MyHandler> logger, IEmailService emailService) 
        : base(logger)
    {
        _emailService = emailService;
    }
}
```

### 4. 易于测试
- 每个事件方法都是独立的
- 可以单独测试每个方法
- 支持 Mock 和依赖注入

## 使用方法

### 创建自定义处理器

```csharp
public class NotificationHandler : WebhookHandlerBase
{
    private readonly INotificationService _notifications;

    public NotificationHandler(
        ILogger<NotificationHandler> logger,
        INotificationService notifications
    ) : base(logger)
    {
        _notifications = notifications;
    }

    protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
    {
        var task = payload.Data?.Task;
        await _notifications.SendAsync($"新任务: {task?.Title}");
    }
}
```

### 注册处理器

在 `Program.cs` 中：

```csharp
// 替换默认处理器
builder.Services.AddSingleton<IWebhookHandler, NotificationHandler>();
```

## 兼容性

- ✅ 完全向后兼容
- ✅ 不影响现有的 `DefaultWebhookHandler` 功能
- ✅ 可以无缝切换处理器实现
- ✅ 支持 .NET 10 AOT 编译

## 测试结果

```
✅ 编译成功 (0 错误, 0 警告)
✅ 所有测试通过
✅ AOT 兼容性验证通过
```

## 下一步

1. 查看 `WEBHOOK_HANDLER_GUIDE.md` 了解详细用法
2. 参考 `CustomWebhookHandlerExample.cs` 查看示例代码
3. 根据你的需求创建自定义处理器
4. 在 `Program.cs` 中注册你的处理器

## 示例场景

### 高优先级任务邮件通知
```csharp
protected override async Task HandleTaskCreatedAsync(VikunjaWebhookPayload payload)
{
    if (payload.Data?.Task?.Priority >= 4)
    {
        await _emailService.SendHighPriorityAlert(...);
    }
}
```

### 任务完成统计
```csharp
protected override async Task HandleTaskUpdatedAsync(VikunjaWebhookPayload payload)
{
    if (payload.Data?.OldTask?.Done == false && payload.Data?.Task?.Done == true)
    {
        await _analyticsService.RecordCompletion(...);
    }
}
```

### 评论 @mention 通知
```csharp
protected override async Task HandleTaskCommentCreatedAsync(VikunjaWebhookPayload payload)
{
    var mentions = ExtractMentions(payload.Data?.Comment?.Comment);
    foreach (var user in mentions)
    {
        await _notificationService.NotifyUser(user, ...);
    }
}
```

## 文档

- 📖 [完整使用指南](./WEBHOOK_HANDLER_GUIDE.md)
- 💡 [示例代码](./VikunjaHook/Services/CustomWebhookHandlerExample.cs)
- 🔧 [基类源码](./VikunjaHook/Services/WebhookHandlerBase.cs)
