using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Models;
using Vikunja.Core.Notifications;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Services;

public class DefaultWebhookHandler : WebhookHandlerBase
{
    private readonly EventRouter _eventRouter;
    private readonly TaskReminderService _reminderService;
    private readonly IVikunjaClientFactory _clientFactory;

    public DefaultWebhookHandler(
        ILogger<DefaultWebhookHandler> logger,
        EventRouter eventRouter,
        TaskReminderService reminderService,
        IVikunjaClientFactory clientFactory) : base(logger)
    {
        _eventRouter = eventRouter;
        _reminderService = reminderService;
        _clientFactory = clientFactory;
    }

    public override async Task HandleWebhookAsync(VikunjaWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Received webhook event: {EventName}", payload.EventName);

        try
        {
            var webhookEvent = ConvertToWebhookEvent(payload);
            
            // 先更新提醒服务
            await UpdateReminderServiceAsync(webhookEvent, cancellationToken);
            
            // 然后路由事件到通知系统
            await _eventRouter.RouteEventAsync(webhookEvent, cancellationToken);
            await base.HandleWebhookAsync(payload, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling webhook event: {EventName}", payload.EventName);
            throw;
        }
    }

    private WebhookEvent ConvertToWebhookEvent(VikunjaWebhookPayload payload)
    {
        var dataJson = JsonSerializer.Serialize(payload.Data, AppJsonSerializerContext.Default.Object);
        var dataElement = JsonDocument.Parse(dataJson).RootElement;

        return new WebhookEvent
        {
            EventName = payload.EventName,
            Time = payload.Time,
            Data = dataElement
        };
    }

    // 根据 webhook 事件更新提醒服务的内存
    private async Task UpdateReminderServiceAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (webhookEvent.EventType == VikunjaEventTypes.TaskCreated && webhookEvent.Task != null)
            {
                await HandleTaskCreatedOrUpdatedAsync(webhookEvent, true, cancellationToken);
            }
            else if (webhookEvent.EventType == VikunjaEventTypes.TaskUpdated && webhookEvent.Task != null)
            {
                await HandleTaskCreatedOrUpdatedAsync(webhookEvent, false, cancellationToken);
            }
            else if (webhookEvent.EventType == VikunjaEventTypes.TaskDeleted && webhookEvent.Task != null)
            {
                Logger.LogInformation("TaskDeleted: Removing task {TaskId} from reminder service", webhookEvent.Task.Id);
                _reminderService.OnTaskDeleted(webhookEvent.Task.Id);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update reminder service for event {EventType}, TaskId={TaskId}", 
                webhookEvent.EventType, webhookEvent.Task?.Id);
        }
    }

    // 处理任务创建或更新事件
    private async Task HandleTaskCreatedOrUpdatedAsync(WebhookEvent webhookEvent, bool isCreated, CancellationToken cancellationToken)
    {
        var eventType = isCreated ? "TaskCreated" : "TaskUpdated";
        
        // 获取完整的任务信息
        var task = await _clientFactory.GetAsync<VikunjaTask>($"tasks/{webhookEvent.Task!.Id}", cancellationToken);
        
        // 获取项目信息（如果有效）
        VikunjaProject? project = null;
        if (webhookEvent.ProjectId > 0)
        {
            project = await _clientFactory.GetAsync<VikunjaProject>($"projects/{webhookEvent.ProjectId}", cancellationToken);
        }
        
        if (task != null && project != null)
        {
            Logger.LogInformation("{EventType}: Updating reminder service for task {TaskId}, Done={Done}, Reminders={ReminderCount}, StartDate={StartDate}, DueDate={DueDate}, EndDate={EndDate}",
                eventType, task.Id, task.Done, task.Reminders?.Count ?? 0, task.StartDate, task.DueDate, task.EndDate);
            
            if (isCreated)
            {
                _reminderService.OnTaskCreated(task, project);
            }
            else
            {
                _reminderService.OnTaskUpdated(task, project);
            }
        }
        else
        {
            Logger.LogWarning("{EventType}: Failed to get complete task or project info for task {TaskId}", eventType, webhookEvent.Task.Id);
        }
    }
}
