using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Smart router that decides whether to execute queries on WASM client or server
/// based on query complexity and estimated row count
/// </summary>
public class HybridQueryRouter
{
    private readonly ServerApiClient _serverApiClient;
    private readonly QueryService _wasmQueryService;
    private readonly AppState _appState;
    private readonly ILogger<HybridQueryRouter> _logger;
    private bool _serverAvailable = true;
    private DateTime _lastServerCheck = DateTime.MinValue;
    private readonly TimeSpan _serverCheckInterval = TimeSpan.FromMinutes(1);

    public HybridQueryRouter(
        ServerApiClient serverApiClient,
        QueryService wasmQueryService,
        AppState appState,
        ILogger<HybridQueryRouter> logger)
    {
        _serverApiClient = serverApiClient;
        _wasmQueryService = wasmQueryService;
        _appState = appState;
        _logger = logger;
    }

    /// <summary>
    /// Routes query execution to either WASM or Server based on complexity
    /// </summary>
    public async Task<QueryResult> ExecuteQueryAsync(string sql, string? sessionId = null)
    {
        try
        {
            // Analyze query complexity
            var complexity = AnalyzeQueryComplexity(sql);
            
            // Decide where to execute
            var shouldUseServer = ShouldUseServer(complexity);
            
            _logger.LogInformation(
                "Query routing decision: {Decision} (Complexity: {Score}, HasJoin: {HasJoin}, EstimatedRows: {Rows})",
                shouldUseServer ? "Server" : "WASM",
                complexity.ComplexityScore,
                complexity.HasJoin,
                complexity.EstimatedRowCount);

            // Try server execution if recommended and available
            if (shouldUseServer && _serverAvailable)
            {
                try
                {
                    return await ExecuteOnServerAsync(sql, sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Server execution failed, falling back to WASM");
                    _serverAvailable = false;
                    _lastServerCheck = DateTime.UtcNow;
                    
                    // Fallback to WASM
                    return await ExecuteOnWasmAsync(sql);
                }
            }
            else if (shouldUseServer && !_serverAvailable)
            {
                // Periodically re-check server availability
                if (DateTime.UtcNow - _lastServerCheck > _serverCheckInterval)
                {
                    _logger.LogInformation("Re-checking server availability...");
                    _serverAvailable = await CheckServerAvailabilityAsync();
                    
                    if (_serverAvailable)
                    {
                        _logger.LogInformation("Server is back online");
                        return await ExecuteOnServerAsync(sql, sessionId);
                    }
                }
                
                // Server still unavailable, use WASM
                return await ExecuteOnWasmAsync(sql);
            }
            else
            {
                // Execute on WASM
                return await ExecuteOnWasmAsync(sql);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in query routing");
            return new QueryResult
            {
                IsSuccess = false,
                ErrorMessage = $"Query execution failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Analyzes query complexity to make routing decisions
    /// </summary>
    private QueryComplexity AnalyzeQueryComplexity(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant();
        
        var hasJoin = sqlUpper.Contains(" JOIN ");
        var hasGroupBy = sqlUpper.Contains(" GROUP BY ");
        var hasOrderBy = sqlUpper.Contains(" ORDER BY ");
        var hasHaving = sqlUpper.Contains(" HAVING ");
        var hasSubquery = sqlUpper.Split('(').Length > 1 && sqlUpper.Contains("SELECT");
        
        // Count aggregations
        var aggregationCount = 0;
        var aggregations = new[] { "COUNT(", "SUM(", "AVG(", "MIN(", "MAX(", "GROUP_CONCAT(" };
        foreach (var agg in aggregations)
        {
            aggregationCount += CountOccurrences(sqlUpper, agg);
        }

        // Estimate row count from tables
        var estimatedRows = EstimateRowCount(sql);

        // Calculate complexity score (0-100)
        var complexityScore = 0;
        if (hasJoin) complexityScore += 30;
        if (hasGroupBy) complexityScore += 20;
        if (hasHaving) complexityScore += 15;
        if (hasSubquery) complexityScore += 25;
        complexityScore += Math.Min(aggregationCount * 5, 20);
        if (hasOrderBy) complexityScore += 10;
        
        // Add score based on estimated rows
        if (estimatedRows > 5000) complexityScore += 20;
        else if (estimatedRows > 1000) complexityScore += 10;

        return new QueryComplexity
        {
            HasJoin = hasJoin,
            HasGroupBy = hasGroupBy,
            HasOrderBy = hasOrderBy,
            HasHaving = hasHaving,
            HasSubquery = hasSubquery,
            AggregationCount = aggregationCount,
            EstimatedRowCount = estimatedRows,
            ComplexityScore = Math.Min(complexityScore, 100)
        };
    }

    /// <summary>
    /// Decides if query should be executed on server
    /// </summary>
    private bool ShouldUseServer(QueryComplexity complexity)
    {
        // Rules:
        // 1. If has JOIN -> Server
        // 2. If >5k rows estimated -> Server
        // 3. If complexity score >40 -> Server
        // 4. Otherwise -> WASM
        
        if (complexity.HasJoin) return true;
        if (complexity.EstimatedRowCount > 5000) return true;
        if (complexity.ComplexityScore > 40) return true;
        
        return false;
    }

    /// <summary>
    /// Estimates row count from loaded tables
    /// </summary>
    private int EstimateRowCount(string sql)
    {
        // Simple heuristic: find table names in query and sum their row counts
        var totalRows = 0;
        
        foreach (var table in _appState.LoadedTables)
        {
            // Check if table name appears in query
            if (sql.Contains(table, StringComparison.OrdinalIgnoreCase))
            {
                // Try to get row count estimate from AppState or use a default
                totalRows += _appState.GetTableRowCount(table);
            }
        }
        
        return totalRows;
    }

    private async Task<QueryResult> ExecuteOnServerAsync(string sql, string? sessionId)
    {
        _logger.LogInformation("Executing query on server");
        
        // Ensure we have a session
        if (string.IsNullOrEmpty(sessionId))
        {
            var session = await _serverApiClient.CreateSessionAsync();
            sessionId = session.SessionId;
            
            // Load tables from WASM to server session
            foreach (var tableName in _appState.LoadedTables)
            {
                var tableData = _wasmQueryService.GetTableData(tableName);
                if (tableData != null)
                {
                    await _serverApiClient.LoadTableAsync(sessionId, tableData, tableName);
                }
            }
        }
        
        var result = await _serverApiClient.ExecuteQueryAsync(sessionId, sql);
        
        // Convert SqliteQueryResult to QueryResult
        return new QueryResult
        {
            IsSuccess = result.IsSuccess,
            ErrorMessage = result.ErrorMessage,
            Columns = result.Columns,
            Rows = result.Rows,
            ExecutionTimeMs = result.ExecutionTimeMs,
            ExecutionTime = TimeSpan.FromMilliseconds(result.ExecutionTimeMs),
            ExecutionLocation = "Server"
        };
    }

    private async Task<QueryResult> ExecuteOnWasmAsync(string sql)
    {
        _logger.LogInformation("Executing query on WASM");
        
        var result = await _wasmQueryService.ExecuteQueryAsync(sql);
        result.ExecutionLocation = "WASM";
        return result;
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Checks if server is available
    /// </summary>
    public async Task<bool> CheckServerAvailabilityAsync()
    {
        try
        {
            _serverAvailable = await _serverApiClient.PingAsync();
            return _serverAvailable;
        }
        catch
        {
            _serverAvailable = false;
            return false;
        }
    }
}

/// <summary>
/// Query complexity analysis result
/// </summary>
public class QueryComplexity
{
    public bool HasJoin { get; set; }
    public bool HasGroupBy { get; set; }
    public bool HasOrderBy { get; set; }
    public bool HasHaving { get; set; }
    public bool HasSubquery { get; set; }
    public int AggregationCount { get; set; }
    public int EstimatedRowCount { get; set; }
    public int ComplexityScore { get; set; }
}
