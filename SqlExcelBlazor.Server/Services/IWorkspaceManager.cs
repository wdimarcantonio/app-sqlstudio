namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Interface for managing per-session workspaces
/// Each session gets its own isolated SqliteService instance
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Get or create a SqliteService for a specific session
    /// </summary>
    SqliteService GetWorkspace(string sessionId);

    /// <summary>
    /// Remove a workspace and dispose its resources
    /// </summary>
    void RemoveWorkspace(string sessionId);

    /// <summary>
    /// Get all active session IDs
    /// </summary>
    IEnumerable<string> GetActiveSessions();

    /// <summary>
    /// Remove inactive sessions that haven't been accessed recently
    /// </summary>
    int CleanupInactiveSessions(TimeSpan inactivityThreshold);
}
