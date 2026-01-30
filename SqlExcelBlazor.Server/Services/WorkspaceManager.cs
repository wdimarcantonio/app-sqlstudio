using System.Collections.Concurrent;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Manages per-session workspaces for session isolation
/// Each session gets its own SqliteService instance with isolated data
/// </summary>
public class WorkspaceManager : IWorkspaceManager
{
    private readonly ConcurrentDictionary<string, WorkspaceEntry> _workspaces = new();
    private readonly object _lock = new();

    private class WorkspaceEntry
    {
        public SqliteService Service { get; set; } = null!;
        public DateTime LastAccessed { get; set; }
    }

    /// <summary>
    /// Get or create a SqliteService for a specific session
    /// </summary>
    public SqliteService GetWorkspace(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        }

        var entry = _workspaces.GetOrAdd(sessionId, _ => new WorkspaceEntry
        {
            Service = new SqliteService(),
            LastAccessed = DateTime.UtcNow
        });

        entry.LastAccessed = DateTime.UtcNow;
        return entry.Service;
    }

    /// <summary>
    /// Remove a workspace and dispose its resources
    /// </summary>
    public void RemoveWorkspace(string sessionId)
    {
        if (_workspaces.TryRemove(sessionId, out var entry))
        {
            entry.Service.Dispose();
        }
    }

    /// <summary>
    /// Get all active session IDs
    /// </summary>
    public IEnumerable<string> GetActiveSessions()
    {
        return _workspaces.Keys.ToList();
    }

    /// <summary>
    /// Remove inactive sessions that haven't been accessed recently
    /// </summary>
    public int CleanupInactiveSessions(TimeSpan inactivityThreshold)
    {
        var cutoff = DateTime.UtcNow - inactivityThreshold;
        var inactiveSessions = _workspaces
            .Where(kvp => kvp.Value.LastAccessed < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var sessionId in inactiveSessions)
        {
            RemoveWorkspace(sessionId);
        }

        return inactiveSessions.Count;
    }
}
