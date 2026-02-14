using Microsoft.Data.Sqlite;
using System.Data;
using Microsoft.Extensions.Logging;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Servizio SQLite in-memory per eseguire query SQL su dati Excel/CSV
/// </summary>
public class SqliteService : IDisposable
{
    private SqliteConnection? _connection;
    private readonly List<string> _loadedTables = new();
    private readonly object _lock = new();
    private readonly ILogger<SqliteService>? _logger;

    public SqliteService(ILogger<SqliteService>? logger = null)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> LoadedTables => _loadedTables.AsReadOnly();

    private void EnsureConnection()
    {
        if (_connection == null)
        {
            _logger?.LogInformation("Creating new SQLite in-memory connection");
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _logger?.LogInformation("SQLite connection opened successfully");
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
                _logger?.LogInformation("LoadTableAsync called for table '{TableName}' with {RowCount} rows, {ColumnCount} columns", 
                    tableName, data.Rows.Count, data.Columns.Count);
                
                EnsureConnection();

                // Rimuovi tabella esistente
                if (_loadedTables.Contains(tableName))
                {
                    _logger?.LogInformation("Dropping existing table '{TableName}'", tableName);
                    using var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", _connection);
                    dropCmd.ExecuteNonQuery();
                    _loadedTables.Remove(tableName);
                }

                // Crea tabella
                var createSql = GenerateCreateTableSql(data, tableName);
                _logger?.LogInformation("Creating table with SQL: {CreateSql}", createSql);
                using var createCmd = new SqliteCommand(createSql, _connection);
                createCmd.ExecuteNonQuery();

                // Inserisci dati
                _logger?.LogInformation("Inserting {RowCount} rows into table '{TableName}'", data.Rows.Count, tableName);
                InsertData(data, tableName);

                _loadedTables.Add(tableName);
                _logger?.LogInformation("Table '{TableName}' successfully loaded. Total tables: {TableCount}. Tables: {Tables}", 
                    tableName, _loadedTables.Count, string.Join(", ", _loadedTables));
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
            var sqliteType = DetectSqliteType(data, col);
            columns.Add($"[{col.ColumnName}] {sqliteType}");
        }
        return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
    }

    private string DetectSqliteType(DataTable data, DataColumn column)
    {
        // SQLite types: NULL, INTEGER, REAL, TEXT, BLOB
        // We map .NET types to SQLite types
        
        // Get regional settings from DataTable if available
        var dateFormat = data.ExtendedProperties["DateFormat"]?.ToString() ?? "auto";
        var decimalSeparator = data.ExtendedProperties["DecimalSeparator"]?.ToString() ?? ".";
        
        // If all values are DBNull or empty, default to TEXT
        if (data.Rows.Count == 0)
            return "TEXT";
        
        // Random sampling of up to 100 rows instead of first 100
        var random = new Random();
        var totalRows = data.Rows.Count;
        var sampleSize = Math.Min(100, totalRows);
        
        var sampledIndices = Enumerable.Range(0, totalRows)
            .OrderBy(x => random.Next())
            .Take(sampleSize)
            .ToList();
        
        var samples = sampledIndices
            .Select(i => data.Rows[i][column])
            .Where(val => val != null && val != DBNull.Value && !string.IsNullOrWhiteSpace(val.ToString()))
            .ToList();
        
        if (!samples.Any())
            return "TEXT";
        
        // Boolean -> INTEGER (0/1)
        if (samples.All(v => v is bool || (v is string s && bool.TryParse(s, out _))))
            return "INTEGER";
        
        // Integer types
        if (samples.All(v => v is sbyte || v is byte || v is short || v is ushort || 
                             v is int || v is uint || v is long || v is ulong ||
                             (v is string s && long.TryParse(s, out _))))
            return "INTEGER";
        
        // Floating point types - use decimal separator setting
        if (samples.All(v => v is float || v is double || v is decimal ||
                             (v is string s && TryParseDecimal(s, decimalSeparator))))
            return "REAL";
        
        // DateTime types - use date format setting
        if (samples.All(v => v is DateTime || v is DateTimeOffset ||
                             (v is string s && TryParseDateTime(s, dateFormat))))
            return "TEXT"; // SQLite stores dates as TEXT (ISO 8601)
        
        // Default to TEXT
        return "TEXT";
    }
    
    private bool TryParseDecimal(string value, string decimalSeparator)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Replace the decimal separator with invariant culture separator
        var normalized = decimalSeparator == "," 
            ? value.Replace(",", ".").Replace(" ", "")
            : value.Replace(" ", "");
        
        return double.TryParse(normalized, System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, out _);
    }
    
    private bool TryParseDateTime(string value, string dateFormat)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        // Check if it has typical date separators
        if (!value.Contains('-') && !value.Contains('/') && !value.Contains('.'))
            return false;
        
        // Try parsing with specific format if not "auto"
        if (dateFormat != "auto")
        {
            var formats = new[] { dateFormat, dateFormat + " HH:mm", dateFormat + " HH:mm:ss" };
            return DateTime.TryParseExact(value, formats, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, out _);
        }
        
        // Auto detection: try standard parsing
        return DateTime.TryParse(value, out _);
    }

    private void InsertData(DataTable data, string tableName)
    {
        if (_connection == null || data.Rows.Count == 0) return;

        // Get regional settings
        var dateFormat = data.ExtendedProperties["DateFormat"]?.ToString() ?? "auto";
        var decimalSeparator = data.ExtendedProperties["DecimalSeparator"]?.ToString() ?? ".";

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
                
                if (value == null || value == DBNull.Value)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", DBNull.Value);
                }
                else
                {
                    // Convert value to appropriate type for SQLite
                    var convertedValue = ConvertValueForSqlite(value, dateFormat, decimalSeparator);
                    cmd.Parameters.AddWithValue($"@p{i}", convertedValue);
                }
            }
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private object ConvertValueForSqlite(object value, string dateFormat, string decimalSeparator)
    {
        if (value == null || value == DBNull.Value)
            return DBNull.Value;
        
        // Boolean to INTEGER (0/1)
        if (value is bool b)
            return b ? 1 : 0;
        
        // String that looks like boolean
        if (value is string str)
        {
            if (bool.TryParse(str, out var boolVal))
                return boolVal ? 1 : 0;
            
            // Try to parse as integer
            if (long.TryParse(str, out var longVal))
                return longVal;
            
            // Try to parse as decimal/double with regional separator
            var normalizedStr = decimalSeparator == "," 
                ? str.Replace(",", ".").Replace(" ", "")
                : str.Replace(" ", "");
                
            if (double.TryParse(normalizedStr, System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
                return doubleVal;
            
            // Try to parse as DateTime with regional format
            DateTime dateVal;
            if (dateFormat != "auto")
            {
                var formats = new[] { dateFormat, dateFormat + " HH:mm", dateFormat + " HH:mm:ss" };
                if (DateTime.TryParseExact(str, formats, 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out dateVal))
                {
                    // Store DateTime in ISO 8601 format for SQLite
                    return dateVal.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else if (DateTime.TryParse(str, out dateVal))
            {
                // Store DateTime in ISO 8601 format for SQLite
                return dateVal.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            // Keep as string
            return str;
        }
        
        // DateTime to ISO 8601 string
        if (value is DateTime dt)
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        
        if (value is DateTimeOffset dto)
            return dto.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        
        // Numeric types - let SQLite handle them
        if (value is sbyte || value is byte || value is short || value is ushort ||
            value is int || value is uint || value is long || value is ulong)
            return value;
        
        if (value is float || value is double || value is decimal)
            return value;
        
        // Default: convert to string
        return value.ToString() ?? "";
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
