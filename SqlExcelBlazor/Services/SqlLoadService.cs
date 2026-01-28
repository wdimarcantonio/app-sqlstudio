using System.Net.Http.Json;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

public class SqlLoadService
{
    private readonly HttpClient _http;

    public SqlLoadService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(SqlLoadConnectionRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/SqlLoad/test-connection", request);
            var result = await response.Content.ReadFromJsonAsync<dynamic>();
            
            if (result != null)
            {
                bool success = result.GetProperty("success").GetBoolean();
                string message = result.GetProperty("message").GetString() ?? "";
                return (success, message);
            }
            
            return (false, "Invalid response");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<TableSchemaInfo>> ListTablesAsync(string connectionString)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/SqlLoad/list-tables", new TableListRequest { ConnectionString = connectionString });
            response.EnsureSuccessStatusCode();
            
            var tables = await response.Content.ReadFromJsonAsync<List<TableSchemaInfo>>();
            return tables ?? new List<TableSchemaInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing tables: {ex.Message}");
            return new List<TableSchemaInfo>();
        }
    }

    public async Task<TableSchemaInfo?> GetTableSchemaAsync(string connectionString, string schema, string tableName)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/SqlLoad/get-table-schema", new TableSchemaRequest
            {
                ConnectionString = connectionString,
                Schema = schema,
                TableName = tableName
            });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TableSchemaInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting table schema: {ex.Message}");
            return null;
        }
    }

    public async Task<SqlLoadResult> ExecuteLoadAsync(SqlLoadRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/SqlLoad/execute-load", request);
            var result = await response.Content.ReadFromJsonAsync<SqlLoadResult>();
            
            return result ?? new SqlLoadResult
            {
                Success = false,
                Message = "Invalid response from server"
            };
        }
        catch (Exception ex)
        {
            return new SqlLoadResult
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.ToString() }
            };
        }
    }

    public string BuildConnectionString(SqlLoadConnectionRequest request)
    {
        var parts = new List<string>
        {
            $"Server={request.Server}",
            $"Database={request.Database}",
            $"TrustServerCertificate={request.TrustServerCertificate}"
        };

        if (request.AuthType == "windows")
        {
            parts.Add("Integrated Security=True");
        }
        else
        {
            parts.Add($"User Id={request.Username}");
            parts.Add($"Password={request.Password}");
            parts.Add("Integrated Security=False");
        }

        return string.Join(";", parts);
    }
}
