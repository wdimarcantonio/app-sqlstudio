using Microsoft.Data.Sqlite;
using System.Data;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Servizio SQLite in-memory per eseguire query SQL su dati Excel/CSV
/// </summary>
public class SqliteService : IDisposable
{
    private SqliteConnection? _connection;
    private readonly List<string> _loadedTables = new();
    private readonly object _lock = new();

    public IReadOnlyList<string> LoadedTables => _loadedTables.AsReadOnly();

    private void EnsureConnection()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
        }
    }

    /// <summary>
    /// Carica un DataTable in SQLite come tabella
    /// </summary>
    public async Task LoadTableAsync(DataTable data, string tableName)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                EnsureConnection();

                // Rimuovi tabella esistente
                if (_loadedTables.Contains(tableName))
                {
                    using var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", _connection);
                    dropCmd.ExecuteNonQuery();
                    _loadedTables.Remove(tableName);
                }

                // Crea tabella
                var createSql = GenerateCreateTableSql(data, tableName);
                using var createCmd = new SqliteCommand(createSql, _connection);
                createCmd.ExecuteNonQuery();

                // Inserisci dati
                InsertData(data, tableName);

                _loadedTables.Add(tableName);
            }
        });
    }

    /// <summary>
    /// Rinomina una tabella esistente
    /// </summary>
    public async Task RenameTableAsync(string oldName, string newName)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                EnsureConnection();

                if (!_loadedTables.Contains(oldName))
                {
                    throw new InvalidOperationException($"Table '{oldName}' not found");
                }

                // SQLite usa ALTER TABLE per rinominare
                using var renameCmd = new SqliteCommand($"ALTER TABLE [{oldName}] RENAME TO [{newName}]", _connection);
                renameCmd.ExecuteNonQuery();

                _loadedTables.Remove(oldName);
                _loadedTables.Add(newName);
            }
        });
    }

    /// <summary>
    /// Elimina una tabella esistente
    /// </summary>
    public async Task DropTableAsync(string tableName)
    {
        await Task.Run(() =>
        {
            lock (_lock)
            {
                EnsureConnection();

                if (!_loadedTables.Contains(tableName))
                {
                    throw new InvalidOperationException($"Table '{tableName}' not found");
                }

                using var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", _connection);
                dropCmd.ExecuteNonQuery();

                _loadedTables.Remove(tableName);
            }
        });
    }

    /// <summary>
    /// Esegue una query SQL e ritorna i risultati
    /// </summary>
    public async Task<QueryResultDto> ExecuteQueryAsync(string sql)
    {
        var result = new QueryResultDto();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    EnsureConnection();

                    using var cmd = new SqliteCommand(sql, _connection);
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
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
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

    private void InsertData(DataTable data, string tableName)
    {
        if (_connection == null || data.Rows.Count == 0) return;

        using var transaction = _connection.BeginTransaction();

        var columnNames = string.Join(", ",
            data.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"));
        var parameters = string.Join(", ",
            Enumerable.Range(0, data.Columns.Count).Select(i => $"@p{i}"));

        var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";

        foreach (DataRow row in data.Rows)
        {
            using var cmd = new SqliteCommand(insertSql, _connection, transaction);
            for (int i = 0; i < data.Columns.Count; i++)
            {
                var value = row[i];
                cmd.Parameters.AddWithValue($"@p{i}", value == DBNull.Value ? null : value?.ToString() ?? "");
            }
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _loadedTables.Clear();
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
