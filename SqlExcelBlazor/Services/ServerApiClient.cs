using System.Net.Http.Json;
using System.Text.Json;
using System.Data;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Client for communicating with the hybrid server API for session-based queries
/// </summary>
public class ServerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServerApiClient> _logger;
    private readonly string _serverBaseUrl;

    public ServerApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ServerApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Get server URL from configuration
        _serverBaseUrl = configuration["ServerApiUrl"] ?? "http://localhost:5001";
        _httpClient.BaseAddress = new Uri(_serverBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Long timeout for large queries
    }

    /// <summary>
    /// Pings the server to check availability
    /// </summary>
    public async Task<bool> PingAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/session", CancellationToken.None);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Server ping failed");
            return false;
        }
    }

    /// <summary>
    /// Creates a new session on the server
    /// </summary>
    public async Task<SessionInfo> CreateSessionAsync(string? userId = null)
    {
        try
        {
            var request = new { userId = userId };
            var response = await _httpClient.PostAsJsonAsync("api/session/create", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CreateSessionResponse>();
                if (result != null && result.Success)
                {
                    _logger.LogInformation("Created server session: {SessionId}", result.SessionId);
                    return new SessionInfo
                    {
                        SessionId = result.SessionId,
                        UserId = userId,
                        CreatedAt = result.CreatedAt,
                        LastAccessedAt = result.CreatedAt
                    };
                }
            }
            
            throw new Exception("Failed to create session");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            throw;
        }
    }

    /// <summary>
    /// Gets session information
    /// </summary>
    public async Task<SessionInfo?> GetSessionAsync(string sessionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/session/{sessionId}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SessionInfoResponse>();
                if (result != null && result.Success)
                {
                    return new SessionInfo
                    {
                        SessionId = result.SessionId,
                        UserId = result.UserId,
                        CreatedAt = result.CreatedAt,
                        LastAccessedAt = result.LastAccessedAt,
                        LoadedTables = result.LoadedTables
                    };
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting session {SessionId}", sessionId);
            return null;
        }
    }

    /// <summary>
    /// Closes a session on the server
    /// </summary>
    public async Task<bool> CloseSessionAsync(string sessionId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/session/{sessionId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing session {SessionId}", sessionId);
            return false;
        }
    }

    /// <summary>
    /// Loads a table into a server session
    /// </summary>
    public async Task<bool> LoadTableAsync(string sessionId, DataTable data, string tableName)
    {
        try
        {
            // Convert DataTable to JSON structure
            var columns = new List<string>();
            foreach (DataColumn col in data.Columns)
            {
                columns.Add(col.ColumnName);
            }

            var rows = new List<Dictionary<string, object?>>();
            foreach (DataRow row in data.Rows)
            {
                var rowDict = new Dictionary<string, object?>();
                foreach (DataColumn col in data.Columns)
                {
                    rowDict[col.ColumnName] = row[col];
                }
                rows.Add(rowDict);
            }

            // Use file controller to upload JSON data
            var request = new { sessionId = sessionId, tableName = tableName, columns = columns, rows = rows };
            
            // Note: We're using a custom endpoint that loads data into a specific session
            // This would need to be added to the server or we can use the existing upload-json with modification
            var response = await _httpClient.PostAsJsonAsync("api/sqlite/upload-json", request);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading table {TableName} into session {SessionId}", tableName, sessionId);
            return false;
        }
    }

    /// <summary>
    /// Executes a query on the server
    /// </summary>
    public async Task<SqliteQueryResult> ExecuteQueryAsync(string sessionId, string sql)
    {
        try
        {
            var request = new { sessionId = sessionId, sql = sql };
            var response = await _httpClient.PostAsJsonAsync("api/query/execute", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SqliteQueryResult>();
                return result ?? new SqliteQueryResult { IsSuccess = false, ErrorMessage = "Empty response" };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new SqliteQueryResult { IsSuccess = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query on server");
            return new SqliteQueryResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Gets query complexity analysis from server
    /// </summary>
    public async Task<QueryComplexityAnalysis?> AnalyzeQueryAsync(string sql)
    {
        try
        {
            var request = new { sql = sql };
            var response = await _httpClient.PostAsJsonAsync("api/query/analyze", request);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("analysis", out var analysisProp))
                {
                    return JsonSerializer.Deserialize<QueryComplexityAnalysis>(analysisProp.GetRawText());
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing query");
            return null;
        }
    }

    /// <summary>
    /// Uploads an Excel file to a server session
    /// </summary>
    public async Task<UploadResult> UploadExcelToSessionAsync(
        string sessionId,
        Stream fileStream,
        string fileName,
        string? tableName = null,
        string? sheetName = null)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            
            content.Add(streamContent, "file", fileName);
            content.Add(new StringContent(sessionId), "sessionId");
            
            if (!string.IsNullOrEmpty(tableName))
                content.Add(new StringContent(tableName), "tableName");
            
            if (!string.IsNullOrEmpty(sheetName))
                content.Add(new StringContent(sheetName), "sheetName");

            var response = await _httpClient.PostAsync("api/file/upload-excel", content);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadResult>();
                return result ?? new UploadResult { Success = false, ErrorMessage = "Empty response" };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new UploadResult { Success = false, ErrorMessage = error };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file to session");
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }
}

// DTOs
public class SessionInfo
{
    public string SessionId { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<string> LoadedTables { get; set; } = new();
}

public class CreateSessionResponse
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class SessionInfoResponse
{
    public bool Success { get; set; }
    public string SessionId { get; set; } = "";
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }
    public List<string> LoadedTables { get; set; } = new();
}

public class QueryComplexityAnalysis
{
    public bool HasJoin { get; set; }
    public bool HasGroupBy { get; set; }
    public bool HasOrderBy { get; set; }
    public bool HasHaving { get; set; }
    public bool HasSubquery { get; set; }
    public int AggregationCount { get; set; }
    public int ComplexityScore { get; set; }
    public string RecommendedExecution { get; set; } = "";
}
