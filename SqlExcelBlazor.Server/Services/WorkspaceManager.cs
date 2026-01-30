using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Manages isolated workspaces for multi-user session isolation.
/// Uses dual SQLite strategy: in-memory for performance + file-based for persistence.
/// </summary>
public class WorkspaceManager : IDisposable
{
    private readonly ConcurrentDictionary<string, WorkspaceSession> _sessions = new();
    private readonly string _dataPath;
    private readonly string _sessionsPath;
    private readonly ILogger<WorkspaceManager> _logger;

    public WorkspaceManager(ILogger<WorkspaceManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        
        // Get data path from configuration or use default
        var configPath = configuration["DataPath"];
        var baseDataPath = string.IsNullOrWhiteSpace(configPath) ?
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqlStudio", "data") :
            configPath;
        
        _dataPath = baseDataPath;
        _sessionsPath = Path.Combine(_dataPath, "sessions");
        
        // Ensure directories exist
        Directory.CreateDirectory(_dataPath);
        Directory.CreateDirectory(_sessionsPath);
        Directory.CreateDirectory(Path.Combine(_dataPath, "uploads"));
        
        _logger.LogInformation("WorkspaceManager initialized. Data path: {DataPath}", _dataPath);
    }

    /// <summary>
    /// Creates a new isolated session with unique ID
    /// </summary>
    public WorkspaceSession CreateSession(string? userId = null)
    {
        var sessionId = Guid.NewGuid().ToString();
        var sessionFilePath = Path.Combine(_sessionsPath, $"session_{sessionId}.db");
        
        var session = new WorkspaceSession
        {
            SessionId = sessionId,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            FilePath = sessionFilePath
        };

        // Create dual SQLite connections: memory + file
        session.MemoryConnection = new SqliteConnection("Data Source=:memory:");
        session.MemoryConnection.Open();
        
        session.FileConnection = new SqliteConnection($"Data Source={sessionFilePath}");
        session.FileConnection.Open();
        
        _sessions[sessionId] = session;
        _logger.LogInformation("Created session {SessionId} for user {UserId}", sessionId, userId ?? "anonymous");
        
        return session;
    }

    /// <summary>
    /// Gets an existing session by ID
    /// </summary>
    public WorkspaceSession? GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastAccessedAt = DateTime.UtcNow;
            return session;
        }
        return null;
    }

    /// <summary>
    /// Loads a DataTable into the session's SQLite databases (both memory and file)
    /// </summary>
    public async Task LoadTableAsync(string sessionId, DataTable data, string tableName)
    {
        var session = GetSession(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        await Task.Run(() =>
        {
            lock (session.Lock)
            {
                // Load into memory database (fast access)
                LoadTableIntoConnection(session.MemoryConnection, data, tableName);
                
                // Load into file database (persistence)
                LoadTableIntoConnection(session.FileConnection, data, tableName);
                
                if (!session.LoadedTables.Contains(tableName))
                {
                    session.LoadedTables.Add(tableName);
                }
                
                session.LastAccessedAt = DateTime.UtcNow;
            }
        });
        
        _logger.LogInformation("Loaded table {TableName} into session {SessionId} ({RowCount} rows)", 
            tableName, sessionId, data.Rows.Count);
    }

    /// <summary>
    /// Executes a query on the session's memory database
    /// </summary>
    public async Task<QueryResultDto> ExecuteQueryAsync(string sessionId, string sql)
    {
        var session = GetSession(sessionId);
        if (session == null)
            throw new InvalidOperationException($"Session {sessionId} not found");

        var result = new QueryResultDto();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                lock (session.Lock)
                {
                    using var cmd = new SqliteCommand(sql, session.MemoryConnection);
                    using var reader = cmd.ExecuteReader();

                    // Read columns
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        result.Columns.Add(reader.GetName(i));
                    }

                    // Read rows
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, object?>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                            row[result.Columns[i]] = value;
                        }
                        result.Rows.Add(row);
                    }

                    result.IsSuccess = true;
                    result.RowCount = result.Rows.Count;
                    result.ColumnCount = result.Columns.Count;
                    
                    session.LastAccessedAt = DateTime.UtcNow;
                }
            });
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Error executing query in session {SessionId}", sessionId);
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        return result;
    }

    /// <summary>
    /// Gets all loaded tables in a session
    /// </summary>
    public List<string> GetLoadedTables(string sessionId)
    {
        var session = GetSession(sessionId);
        return session?.LoadedTables.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Closes and removes a session
    /// </summary>
    public void CloseSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Dispose();
            
            // Optionally delete the file (or keep it for later recovery)
            try
            {
                if (File.Exists(session.FilePath))
                {
                    File.Delete(session.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete session file {FilePath}", session.FilePath);
            }
            
            _logger.LogInformation("Closed session {SessionId}", sessionId);
        }
    }

    /// <summary>
    /// Cleans up inactive sessions (last accessed > 2 hours ago)
    /// </summary>
    public int CleanupInactiveSessions(TimeSpan inactivityThreshold)
    {
        var now = DateTime.UtcNow;
        var inactiveSessions = _sessions.Values
            .Where(s => now - s.LastAccessedAt > inactivityThreshold)
            .ToList();

        foreach (var session in inactiveSessions)
        {
            CloseSession(session.SessionId);
        }

        if (inactiveSessions.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} inactive sessions", inactiveSessions.Count);
        }

        return inactiveSessions.Count;
    }

    /// <summary>
    /// Gets all active sessions
    /// </summary>
    public List<SessionInfo> GetActiveSessions()
    {
        return _sessions.Values.Select(s => new SessionInfo
        {
            SessionId = s.SessionId,
            UserId = s.UserId,
            CreatedAt = s.CreatedAt,
            LastAccessedAt = s.LastAccessedAt,
            LoadedTables = s.LoadedTables.ToList()
        }).ToList();
    }

    private void LoadTableIntoConnection(SqliteConnection connection, DataTable data, string tableName)
    {
        // Drop existing table if present
        using (var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", connection))
        {
            dropCmd.ExecuteNonQuery();
        }

        // Create table
        var createSql = GenerateCreateTableSql(data, tableName);
        using (var createCmd = new SqliteCommand(createSql, connection))
        {
            createCmd.ExecuteNonQuery();
        }

        // Insert data
        InsertData(connection, data, tableName);
    }

    private string GenerateCreateTableSql(DataTable data, string tableName)
    {
        var columns = new List<string>();
        foreach (DataColumn col in data.Columns)
        {
            columns.Add($"[{col.ColumnName}] TEXT");
        }
        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    private void InsertData(SqliteConnection connection, DataTable data, string tableName)
    {
        if (data.Rows.Count == 0) return;

        using var transaction = connection.BeginTransaction();

        var columnNames = string.Join(", ",
            data.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"));
        var parameters = string.Join(", ",
            Enumerable.Range(0, data.Columns.Count).Select(i => $"@p{i}"));

        var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";

        foreach (DataRow row in data.Rows)
        {
            using var cmd = new SqliteCommand(insertSql, connection, transaction);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                var value = row[i];
                cmd.Parameters.AddWithValue($"@p{i}", value == DBNull.Value ? DBNull.Value : (object)(value?.ToString() ?? ""));
            }
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            session.Dispose();
        }
        _sessions.Clear();
    }
}

/// <summary>
/// Represents an isolated workspace session
/// </summary>
public class WorkspaceSession : IDisposable
{
    public string SessionId { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public string FilePath { get; set; } = "";
    public SqliteConnection MemoryConnection { get; set; } = null!;
    public SqliteConnection FileConnection { get; set; } = null!;
    public List<string> LoadedTables { get; set; } = new();
    public object Lock { get; } = new object();

    public void Dispose()
    {
        MemoryConnection?.Close();
        MemoryConnection?.Dispose();
        FileConnection?.Close();
        FileConnection?.Dispose();
    }
}

/// <summary>
/// Session information for API responses
/// </summary>
public class SessionInfo
{
    public string SessionId { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<string> LoadedTables { get; set; } = new();
}
