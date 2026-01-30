using System.Text;
using Microsoft.Extensions.Logging;
using Vikunja.Core.Mcp.Models;
using Vikunja.Core.Mcp.Services;
using Vikunja.Core.Notifications.Models;
using Vikunja.Core.Notifications.Providers;

namespace Vikunja.Core.Notifications;

/// <summary>
/// 定时推送服务
/// </summary>
public sealed class ScheduledPushService
{
    private readonly IVikunjaClientFactory _clientFactory;
    private readonly JsonFileConfigurationManager _configManager;
    private readonly IEnumerable<PushDeerProvider> _providers;
    private readonly ILogger<ScheduledPushService> _logger;
    private readonly Timer _timer;
    private readonly List<ScheduledPushRecord> _history;
    private readonly object _historyLock = new();
    private const int MaxHistoryRecords = 100;

    public ScheduledPushService(
        IVikunjaClientFactory clientFactory,
        JsonFileConfigurationManager configManager,
        IEnumerable<PushDeerProvider> providers,
        ILogger<ScheduledPushService> logger)
    {
        _clientFactory = clientFactory;
        _configManager = configManager;
        _providers = providers;
        _logger = logger;
        _history = new List<ScheduledPushRecord>();

        // 每分钟检查一次
        _timer = new Timer(CheckAndPushAsync, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
    }

    private async void CheckAndPushAsync(object? state)
    {
        try
        {
            var now = DateTime.Now;
            var currentTime = now.ToString("HH:mm");

            _logger.LogDebug("⏰ 检查定时推送任务 - 当前时间: {Time}", currentTime);

            // 加载所有用户配置
            var userIds = await _configManager.GetAllUserIdsAsync(CancellationToken.None);

            foreach (var userId in userIds)
            {
                try
                {
                    var userConfig = await _configManager.LoadUserConfigAsync(userId, CancellationToken.None);
                    if (userConfig == null)
                        continue;

                    // 检查是否有定时推送配置
                    var scheduledConfigs = await LoadScheduledConfigsAsync(userId, CancellationToken.None);
                    
                    foreach (var config in scheduledConfigs.Where(c => c.Enabled))
                    {
                        // 检查是否到达推送时间
                        if (config.PushTime == currentTime)
                        {
                            // 检查今天是否已推送（使用本地时间比较）
                            if (config.LastPushTime.HasValue)
                            {
                                var lastPushLocal = config.LastPushTime.Value.ToLocalTime();
                                if (lastPushLocal.Date == now.Date)
                                {
                                    _logger.LogDebug("用户 {UserId} 的配置 {ConfigId} 今天已推送，跳过", userId, config.Id);
                                    continue;
                                }
                            }

                            _logger.LogInformation("⏰ 触发定时推送 - 用户: {UserId}, 时间: {Time}", userId, currentTime);
                            await ExecutePushAsync(config, userConfig, CancellationToken.None);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "处理用户 {UserId} 的定时推送时出错", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查定时推送任务时出错");
        }
    }

    private async Task ExecutePushAsync(
        ScheduledPushConfig config,
        UserConfig userConfig,
        CancellationToken cancellationToken)
    {
        var record = new ScheduledPushRecord
        {
            ConfigId = config.Id,
            UserId = config.UserId,
            Timestamp = DateTime.UtcNow,
            Providers = config.Providers
        };

        try
        {
            // 获取未完成的任务
            var tasks = await GetUncompletedTasksAsync(config, cancellationToken);
            
            record.TaskCount = tasks.Count;

            if (tasks.Count == 0)
            {
                _logger.LogInformation("用户 {UserId} 没有符合条件的未完成任务，跳过推送", config.UserId);
                record.Success = true;
                record.Title = config.TitleTemplate.Replace("{{count}}", "0").Replace("{{date}}", DateTime.Now.ToString("yyyy-MM-dd"));
                record.Body = "今天没有待办任务 ✨";
                
                // 更新最后推送时间
                config.LastPushTime = DateTime.UtcNow;
                await SaveScheduledConfigAsync(config, cancellationToken);
                
                AddHistory(record);
                return;
            }

            // 渲染标题和正文
            record.Title = RenderTitle(config.TitleTemplate, tasks);
            record.Body = RenderBody(config.BodyTemplate, tasks);

            _logger.LogInformation("📤 推送未完成任务 - 用户: {UserId}, 任务数: {Count}", 
                config.UserId, tasks.Count);

            // 发送推送
            var pushSuccess = false;
            foreach (var providerType in config.Providers)
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
                if (provider == null)
                    continue;

                var providerConfig = userConfig.Providers.FirstOrDefault(p => p.ProviderType == providerType);
                if (providerConfig == null)
                    continue;

                try
                {
                    var message = new NotificationMessage(
                        Title: record.Title,
                        Body: record.Body,
                        Format: NotificationFormat.Markdown
                    );

                    if (provider is PushDeerProvider pushDeer && 
                        providerConfig.Settings.TryGetValue("pushkey", out var pushKey))
                    {
                        var result = await pushDeer.SendAsync(message, pushKey, cancellationToken);
                        if (result.Success)
                        {
                            pushSuccess = true;
                            _logger.LogInformation("✓ 推送成功 - 提供商: {Provider}", providerType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "推送失败 - 提供商: {Provider}", providerType);
                }
            }

            record.Success = pushSuccess;

            // 更新最后推送时间
            config.LastPushTime = DateTime.UtcNow;
            await SaveScheduledConfigAsync(config, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行定时推送失败 - 用户: {UserId}", config.UserId);
            record.Success = false;
            record.ErrorMessage = ex.Message;
        }

        AddHistory(record);
    }

    private async Task<List<VikunjaTask>> GetUncompletedTasksAsync(
        ScheduledPushConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            // 获取所有未完成的任务
            var allTasks = await _clientFactory.GetAsync<List<VikunjaTask>>(
                "tasks?filter=done%3Dfalse&per_page=1000",
                cancellationToken
            );

            if (allTasks == null || allTasks.Count == 0)
                return new List<VikunjaTask>();

            // 过滤任务：优先级 OR 标签
            var filteredTasks = allTasks.Where(task =>
            {
                // 如果没有设置任何过滤条件，返回所有任务
                if (config.MinPriority == 0 && config.LabelIds.Count == 0)
                {
                    return true;
                }

                // 检查优先级（大于等于最低优先级）
                var priorityMatch = config.MinPriority > 0 && task.Priority >= config.MinPriority;

                // 检查标签（任意标签匹配）
                var labelMatch = false;
                if (config.LabelIds.Count > 0 && task.Labels != null)
                {
                    labelMatch = task.Labels.Any(label => config.LabelIds.Contains(label.Id));
                }

                // OR 运算：优先级匹配 OR 标签匹配
                return priorityMatch || labelMatch;
            }).ToList();

            return filteredTasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取未完成任务失败");
            return new List<VikunjaTask>();
        }
    }

    private static string RenderTitle(string template, List<VikunjaTask> tasks)
    {
        return template
            .Replace("{{count}}", tasks.Count.ToString())
            .Replace("{{date}}", DateTime.Now.ToString("yyyy-MM-dd"));
    }

    private static string RenderBody(string template, List<VikunjaTask> tasks)
    {
        var sb = new StringBuilder();
        
        // 按优先级分组
        var groupedTasks = tasks
            .GroupBy(t => t.Priority)
            .OrderByDescending(g => g.Key);

        foreach (var group in groupedTasks)
        {
            var priorityEmoji = group.Key switch
            {
                5 => "🔴",
                4 => "🟠",
                3 => "🟡",
                2 => "🟢",
                1 => "🔵",
                _ => "⚪"
            };

            foreach (var task in group.OrderBy(t => t.DueDate ?? DateTime.MaxValue))
            {
                sb.Append($"- {priorityEmoji} **{task.Title}**");
                
                if (task.DueDate.HasValue)
                {
                    var dueDate = task.DueDate.Value;
                    var daysUntilDue = (dueDate.Date - DateTime.Now.Date).Days;
                    
                    if (daysUntilDue < 0)
                        sb.Append($" ⚠️ 已逾期 {-daysUntilDue} 天");
                    else if (daysUntilDue == 0)
                        sb.Append(" 📅 今天到期");
                    else if (daysUntilDue <= 3)
                        sb.Append($" 📅 {daysUntilDue} 天后到期");
                }

                if (task.Labels != null && task.Labels.Count > 0)
                {
                    sb.Append($" 🏷️ {string.Join(", ", task.Labels.Select(l => l.Title))}");
                }

                sb.AppendLine();
            }
        }

        var tasksMarkdown = sb.ToString();

        return template
            .Replace("{{tasks}}", tasksMarkdown)
            .Replace("{{count}}", tasks.Count.ToString())
            .Replace("{{date}}", DateTime.Now.ToString("yyyy-MM-dd"));
    }

    private void AddHistory(ScheduledPushRecord record)
    {
        lock (_historyLock)
        {
            _history.Insert(0, record);
            if (_history.Count > MaxHistoryRecords)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }
    }

    public List<ScheduledPushRecord> GetHistory(int count = 50)
    {
        lock (_historyLock)
        {
            return _history.Take(count).ToList();
        }
    }

    public void ClearHistory()
    {
        lock (_historyLock)
        {
            _history.Clear();
        }
    }

    public async Task<List<ScheduledPushConfig>> LoadScheduledConfigsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine("data", "scheduled-push", $"{userId}.json");
        
        if (!File.Exists(filePath))
            return new List<ScheduledPushConfig>();

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var configs = System.Text.Json.JsonSerializer.Deserialize(
                json,
                WebhookNotificationJsonContext.Default.ListScheduledPushConfig
            );
            return configs ?? new List<ScheduledPushConfig>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载定时推送配置失败 - 用户: {UserId}", userId);
            return new List<ScheduledPushConfig>();
        }
    }

    public async Task SaveScheduledConfigAsync(
        ScheduledPushConfig config,
        CancellationToken cancellationToken)
    {
        var dirPath = Path.Combine("data", "scheduled-push");
        Directory.CreateDirectory(dirPath);

        var filePath = Path.Combine(dirPath, $"{config.UserId}.json");

        var configs = await LoadScheduledConfigsAsync(config.UserId, cancellationToken);
        
        var existingIndex = configs.FindIndex(c => c.Id == config.Id);
        if (existingIndex >= 0)
        {
            config.Updated = DateTime.UtcNow;
            configs[existingIndex] = config;
        }
        else
        {
            configs.Add(config);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(
            configs,
            WebhookNotificationJsonContext.Default.ListScheduledPushConfig
        );

        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("✓ 保存定时推送配置 - 用户: {UserId}, 配置ID: {ConfigId}", 
            config.UserId, config.Id);
    }

    public async Task DeleteScheduledConfigAsync(
        string userId,
        string configId,
        CancellationToken cancellationToken)
    {
        var configs = await LoadScheduledConfigsAsync(userId, cancellationToken);
        configs.RemoveAll(c => c.Id == configId);

        var dirPath = Path.Combine("data", "scheduled-push");
        var filePath = Path.Combine(dirPath, $"{userId}.json");

        var json = System.Text.Json.JsonSerializer.Serialize(
            configs,
            WebhookNotificationJsonContext.Default.ListScheduledPushConfig
        );

        await File.WriteAllTextAsync(filePath, json, cancellationToken);
        _logger.LogInformation("✓ 删除定时推送配置 - 用户: {UserId}, 配置ID: {ConfigId}", 
            userId, configId);
    }
}
