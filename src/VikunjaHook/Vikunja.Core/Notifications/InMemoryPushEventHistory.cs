using System.Collections.Concurrent;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

/// <summary>
/// Thread-safe in-memory implementation of push event history
/// </summary>
public sealed class InMemoryPushEventHistory : IPushEventHistory
{
    private readonly ConcurrentQueue<PushEventRecord> _records = new();
    private readonly int _maxRecords;
    private int _totalCount;

    public InMemoryPushEventHistory(int maxRecords = 100)
    {
        _maxRecords = maxRecords;
    }

    public void AddRecord(PushEventRecord record)
    {
        _records.Enqueue(record);
        Interlocked.Increment(ref _totalCount);

        // Trim old records if exceeding max
        while (_records.Count > _maxRecords)
        {
            _records.TryDequeue(out _);
        }
    }

    public List<PushEventRecord> GetRecentRecords(int count = 50)
    {
        return _records
            .Reverse()
            .Take(count)
            .ToList();
    }

    public int GetTotalCount()
    {
        return _totalCount;
    }

    public void Clear()
    {
        while (_records.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _totalCount, 0);
    }
}
