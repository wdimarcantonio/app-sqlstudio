using System.Net.Http.Json;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

public class SqlServerClientService
{
    private readonly HttpClient _http;
    
    public SqlServerClientService(HttpClient http)
    {
        _http = http;
    }
    
    public async Task<bool> TestConnection(string connectionString)
    {
        var response = await _http.PostAsJsonAsync("api/SqlServer/test", new { ConnectionString = connectionString });
        return response.IsSuccessStatusCode;
    }
    
    public async Task<List<SchemaItem>> GetTables(string connectionString)
    {
        var response = await _http.PostAsJsonAsync("api/SqlServer/tables", new { ConnectionString = connectionString });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<List<SchemaItem>>() ?? new List<SchemaItem>();
        }
        string? error = await response.Content.ReadAsStringAsync();
        throw new Exception(string.IsNullOrEmpty(error) ? "Errore sconosciuto" : error);
    }
    
    public async Task<QueryResult> ExecuteQuery(string connectionString, string query)
    {
        var response = await _http.PostAsJsonAsync("api/SqlServer/query", new { ConnectionString = connectionString, Query = query });
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<RemoteQueryResult>();
            
            var columns = data?.Columns ?? new List<string>();
            var rawRows = data?.Rows ?? new List<Dictionary<string, object?>>();
            
            var processedRows = new List<Dictionary<string, object?>>();
            
            foreach (var rawRow in rawRows)
            {
                var newRow = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in rawRow)
                {
                    // Handle JsonElement if present (System.Text.Json default for object)
                    if (kvp.Value is System.Text.Json.JsonElement element)
                    {
                        newRow[kvp.Key] = element.ValueKind switch
                        {
                            System.Text.Json.JsonValueKind.String => element.GetString(),
                            System.Text.Json.JsonValueKind.Number => element.GetDouble(),
                            System.Text.Json.JsonValueKind.True => true,
                            System.Text.Json.JsonValueKind.False => false,
                            System.Text.Json.JsonValueKind.Null => null,
                            _ => element.ToString()
                        };
                    }
                    else
                    {
                        newRow[kvp.Key] = kvp.Value;
                    }
                }
                processedRows.Add(newRow);
            }

            return new QueryResult 
            {
                Columns = columns,
                Rows = processedRows,
                ExecutionTime = TimeSpan.Zero 
            };
        }
        string? error = await response.Content.ReadAsStringAsync();
        throw new Exception(string.IsNullOrEmpty(error) ? "Errore query" : error);
    }
}

public class LocalSchemaItem
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string FullName => $"[{Schema}].[{Name}]";
    public bool IsSelected { get; set; }
    public string Alias { get; set; } = string.Empty;
}

public class SchemaItem
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class RemoteQueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}
