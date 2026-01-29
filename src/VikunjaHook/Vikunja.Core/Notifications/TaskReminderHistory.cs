using System.Collections.Concurrent;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

public class TaskReminderHistory
{
    private readonly ConcurrentQueue<TaskReminderRecord> _records = new();
    private readonly int _maxRecords;
    private int _totalCount = 0;

    public TaskReminderHistory(int maxRecords = 100)
    {
        _maxRecords = maxRecords;
    }

    public void AddRecord(TaskReminderRecord record)
    {
        _records.Enqueue(record);
        Interlocked.Increment(ref _totalCount);

        // Keep only the last maxRecords
        while (_records.Count > _maxRecords)
        {
            _records.TryDequeue(out _);
        }
    }

    public List<TaskReminderRecord> GetRecentRecords(int count = 50)
    {
        return _records.Reverse().Take(count).ToList();
    }

    public int GetTotalCount()
    {
        return _totalCount;
    }

    public void Clear()
    {
        _records.Clear();
        _totalCount = 0;
    }
}

public class TaskReminderRecord
{
    public required string Id { get; init; }
    public required DateTime Timestamp { get; init; }
    public required long TaskId { get; init; }
    public required string TaskTitle { get; init; }
    public required string ProjectTitle { get; init; }
    public required string ReminderType { get; init; }
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required List<string> Providers { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
