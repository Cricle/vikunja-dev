using System.Collections.Immutable;
using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

/// <summary>
/// Lock-free thread-safe in-memory implementation of push event history
/// Uses immutable collections for thread safety without locks
/// </summary>
public sealed class InMemoryPushEventHistory : IPushEventHistory
{
    private volatile ImmutableList<PushEventRecord> _records = ImmutableList<PushEventRecord>.Empty;
    private readonly int _maxRecords;
    private int _totalCount;
    private readonly object _updateLock = new object();

    public InMemoryPushEventHistory(int maxRecords = 30)
    {
        _maxRecords = maxRecords;
    }

    public void AddRecord(PushEventRecord record)
    {
        // Use a minimal lock only for the update operation
        // Reading is lock-free due to immutable collections
        lock (_updateLock)
        {
            // Add new record at the beginning (most recent first)
            var updated = _records.Insert(0, record);
            
            // Trim to max records if needed
            if (updated.Count > _maxRecords)
            {
                updated = updated.RemoveRange(_maxRecords, updated.Count - _maxRecords);
            }
            
            _records = updated;
            Interlocked.Increment(ref _totalCount);
        }
    }

    public List<PushEventRecord> GetRecentRecords(int count = 50)
    {
        var current = _records;
        var takeCount = Math.Min(count, current.Count);
        return current.Take(takeCount).ToList();
    }

    public int GetTotalCount()
    {
        return _totalCount;
    }

    public void Clear()
    {
        lock (_updateLock)
        {
            _records = ImmutableList<PushEventRecord>.Empty;
            Interlocked.Exchange(ref _totalCount, 0);
        }
    }
}
