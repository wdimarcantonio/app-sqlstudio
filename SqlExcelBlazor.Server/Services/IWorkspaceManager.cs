using Microsoft.Data.Sqlite;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Gestisce workspace isolati per sessione utente
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>
    /// Ottiene o crea un workspace isolato per la sessione
    /// </summary>
    SessionWorkspace GetOrCreateWorkspace(string sessionId);

    /// <summary>
    /// Chiude il workspace e libera le risorse
    /// </summary>
    Task CloseWorkspaceAsync(string sessionId);

    /// <summary>
    /// Ottiene un workspace esistente (se presente)
    /// </summary>
    SessionWorkspace? GetWorkspace(string sessionId);

    /// <summary>
    /// Ottiene lista di tutti i workspace attivi
    /// </summary>
    List<SessionWorkspace> GetActiveWorkspaces();
}

/// <summary>
/// Rappresenta un workspace isolato per una sessione utente
/// </summary>
public class SessionWorkspace
{
    public string SessionId { get; set; } = "";
    public SqliteConnection Connection { get; set; } = null!;
    public DateTime LastActivity { get; set; }
    public DateTime CreatedAt { get; set; }
}
