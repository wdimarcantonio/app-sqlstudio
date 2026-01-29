using Microsoft.Data.Sqlite;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Gestisce workspace isolati per sessione utente con connessioni SQLite dedicate
/// </summary>
public class WorkspaceManager : IWorkspaceManager
{
    private readonly Dictionary<string, SessionWorkspace> _sessions = new();
    private readonly ILogger<WorkspaceManager> _logger;
    private readonly object _lock = new();

    public WorkspaceManager(ILogger<WorkspaceManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Ottiene o crea un workspace isolato per la sessione
    /// </summary>
    public SessionWorkspace GetOrCreateWorkspace(string sessionId)
    {
        lock (_lock)
        {
            if (_sessions.TryGetValue(sessionId, out var workspace))
            {
                workspace.LastActivity = DateTime.UtcNow;
                return workspace;
            }

            _logger.LogInformation("Creating new workspace for session {SessionId}", sessionId);

            var newWorkspace = new SessionWorkspace
            {
                SessionId = sessionId,
                Connection = new SqliteConnection("Data Source=:memory:"),
                LastActivity = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            newWorkspace.Connection.Open();
            _sessions[sessionId] = newWorkspace;

            return newWorkspace;
        }
    }

    /// <summary>
    /// Chiude il workspace e libera le risorse
    /// </summary>
    public async Task CloseWorkspaceAsync(string sessionId)
    {
        SessionWorkspace? workspace;
        
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out workspace))
                return;

            _sessions.Remove(sessionId);
        }

        // Dispose delle risorse fuori dal lock
        try
        {
            workspace.Connection?.Close();
            workspace.Connection?.Dispose();
            _logger.LogInformation("Closed workspace for session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing workspace for session {SessionId}", sessionId);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Ottiene un workspace esistente (se presente)
    /// </summary>
    public SessionWorkspace? GetWorkspace(string sessionId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(sessionId, out var workspace) ? workspace : null;
        }
    }

    /// <summary>
    /// Ottiene lista di tutti i workspace attivi
    /// </summary>
    public List<SessionWorkspace> GetActiveWorkspaces()
    {
        lock (_lock)
        {
            return _sessions.Values.ToList();
        }
    }
}
