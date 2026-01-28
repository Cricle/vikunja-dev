using Vikunja.Core.Notifications.Models;

namespace Vikunja.Core.Notifications;

/// <summary>
/// Service for tracking push event history in memory
/// </summary>
public interface IPushEventHistory
{
    /// <summary>
    /// Add a push event record
    /// </summary>
    void AddRecord(PushEventRecord record);
    
    /// <summary>
    /// Get recent push event records
    /// </summary>
    /// <param name="count">Maximum number of records to return</param>
    List<PushEventRecord> GetRecentRecords(int count = 50);
    
    /// <summary>
    /// Get total count of records
    /// </summary>
    int GetTotalCount();
    
    /// <summary>
    /// Clear all records
    /// </summary>
    void Clear();
}
