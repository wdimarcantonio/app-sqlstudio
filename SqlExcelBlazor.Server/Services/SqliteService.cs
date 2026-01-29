using Microsoft.Data.Sqlite;
using System.Data;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Servizio SQLite in-memory per eseguire query SQL su dati Excel/CSV
/// Ogni sessione ha il proprio database isolato
/// </summary>
public class SqliteService : IDisposable
{
    private readonly IWorkspaceManager _workspaceManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SqliteService> _logger;
    private readonly object _lock = new();

    public SqliteService(
        IWorkspaceManager workspaceManager,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SqliteService> logger)
    {
        _workspaceManager = workspaceManager;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Ottiene SessionId dal contesto HTTP (Blazor Server usa Connection.Id)
    /// </summary>
    private string GetSessionId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Blazor Server: usa SignalR Connection ID
            return httpContext.Connection.Id ?? "default";
        }
        return "default";
    }

    /// <summary>
    /// Ottiene connessione SQLite isolata per la sessione corrente
    /// </summary>
    private SqliteConnection GetConnection()
    {
        var sessionId = GetSessionId();
        var workspace = _workspaceManager.GetOrCreateWorkspace(sessionId);
        return workspace.Connection;
    }

    /// <summary>
    /// Ottiene lista tabelle della sessione corrente
    /// </summary>
    public async Task<List<string>> GetLoadedTablesAsync()
    {
        var connection = GetConnection();
        var tables = new List<string>();

        using (var cmd = new SqliteCommand(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", 
            connection))
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        return tables;
    }

    public IReadOnlyList<string> LoadedTables => GetLoadedTablesAsync().Result;

    /// <summary>
    /// Carica un DataTable in SQLite come tabella (session-scoped)
    /// </summary>
    public async Task LoadTableAsync(DataTable data, string tableName)
    {
        var connection = GetConnection();
        var sessionId = GetSessionId();
        
        await Task.Run(() =>
        {
            lock (_lock)
            {
                // Rimuovi tabella esistente nella sessione corrente
                using (var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", connection))
                {
                    dropCmd.ExecuteNonQuery();
                }

                // Crea tabella
                var createSql = GenerateCreateTableSql(data, tableName);
                using (var createCmd = new SqliteCommand(createSql, connection))
                {
                    createCmd.ExecuteNonQuery();
                }

                // Inserisci dati
                InsertData(data, tableName, connection);
            }
        });

        _logger.LogInformation($"Loaded table '{tableName}' with {data.Rows.Count} rows in session {sessionId}");
    }

    /// <summary>
    /// Rinomina una tabella esistente (session-scoped)
    /// </summary>
    public async Task RenameTableAsync(string oldName, string newName)
    {
        var connection = GetConnection();
        
        await Task.Run(() =>
        {
            lock (_lock)
            {
                using var renameCmd = new SqliteCommand(
                    $"ALTER TABLE [{oldName}] RENAME TO [{newName}]", 
                    connection);
                renameCmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// Elimina una tabella esistente (session-scoped)
    /// </summary>
    public async Task DropTableAsync(string tableName)
    {
        var connection = GetConnection();
        
        await Task.Run(() =>
        {
            lock (_lock)
            {
                using var dropCmd = new SqliteCommand(
                    $"DROP TABLE IF EXISTS [{tableName}]", 
                    connection);
                dropCmd.ExecuteNonQuery();
            }
        });
    }

    /// <summary>
    /// Esegue una query SQL e ritorna i risultati (session-scoped)
    /// </summary>
    public async Task<QueryResultDto> ExecuteQueryAsync(string sql)
    {
        var connection = GetConnection();
        var sessionId = GetSessionId();
        var result = new QueryResultDto();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    using var cmd = new SqliteCommand(sql, connection);
                    using var reader = cmd.ExecuteReader();

                    // Leggi colonne
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        result.Columns.Add(reader.GetName(i));
                    }

                    // Leggi righe
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
                }
            });

            _logger.LogInformation($"Query executed successfully in session {sessionId}: {result.Rows.Count} rows in {result.ExecutionTimeMs:F2}ms");
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            
            _logger.LogError(ex, $"Query execution failed in session {sessionId}");
        }

        stopwatch.Stop();
        result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        return result;
    }

    private string GenerateCreateTableSql(DataTable data, string tableName)
    {
        var columns = new List<string>();
        foreach (DataColumn col in data.Columns)
        {
            // SQLite è typeless, usiamo TEXT per tutto
            columns.Add($"[{col.ColumnName}] TEXT");
        }
        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    /// <summary>
    /// Inserisce dati in una tabella (session-scoped)
    /// </summary>
    private void InsertData(DataTable data, string tableName, SqliteConnection connection)
    {
        if (data.Rows.Count == 0) return;

        using var transaction = connection.BeginTransaction();

        var columnNames = string.Join(", ",
            data.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"));
        var parameters = string.Join(", ",
            Enumerable.Range(0, data.Columns.Count).Select(i => $"@p{i}"));

        var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";

        try
        {
            foreach (DataRow row in data.Rows)
            {
                using var cmd = new SqliteCommand(insertSql, connection, transaction);
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    var value = row[i];
                    // Fix: Use DBNull.Value instead of null
                    cmd.Parameters.AddWithValue($"@p{i}", value == DBNull.Value ? DBNull.Value : (object)(value?.ToString() ?? ""));
                }
                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Dispose()
    {
        // Non disponiamo più la connessione qui
        // WorkspaceManager si occupa della pulizia
    }
}

/// <summary>
/// DTO per i risultati delle query
/// </summary>
public class QueryResultDto
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public double ExecutionTimeMs { get; set; }
}
