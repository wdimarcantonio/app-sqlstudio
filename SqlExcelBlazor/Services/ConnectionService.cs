using System.Net.Http.Json;
using SqlExcelBlazor.Models.Connections;

namespace SqlExcelBlazor.Services;

public interface IConnectionService
{
    Task<List<Connection>> GetAllAsync();
    Task<Connection?> GetByIdAsync(int id);
    Task<Connection> CreateAsync(Connection connection);
    Task<Connection> UpdateAsync(Connection connection);
    Task DeleteAsync(int id);
    Task<ConnectionTestResult> TestConnectionAsync(int id);
    Task<int> GetCountAsync();
}

public class ConnectionService : IConnectionService
{
    private readonly HttpClient _http;
    
    public ConnectionService(HttpClient http)
    {
        _http = http;
    }
    
    public async Task<List<Connection>> GetAllAsync()
    {
        try
        {
            var connections = await _http.GetFromJsonAsync<List<Connection>>("api/Connection");
            return connections ?? new List<Connection>();
        }
        catch
        {
            return new List<Connection>();
        }
    }
    
    public async Task<Connection?> GetByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<Connection>($"api/Connection/{id}");
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<Connection> CreateAsync(Connection connection)
    {
        var response = await _http.PostAsJsonAsync("api/Connection", connection);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Connection>() ?? connection;
    }
    
    public async Task<Connection> UpdateAsync(Connection connection)
    {
        var response = await _http.PutAsJsonAsync($"api/Connection/{connection.Id}", connection);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Connection>() ?? connection;
    }
    
    public async Task DeleteAsync(int id)
    {
        var response = await _http.DeleteAsync($"api/Connection/{id}");
        response.EnsureSuccessStatusCode();
    }
    
    public async Task<ConnectionTestResult> TestConnectionAsync(int id)
    {
        try
        {
            var response = await _http.PostAsync($"api/Connection/{id}/test", null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ConnectionTestResult>() 
                ?? new ConnectionTestResult { Success = false, Message = "Unknown error" };
        }
        catch (Exception ex)
        {
            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message
            };
        }
    }
    
    public async Task<int> GetCountAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<int>("api/Connection/count");
        }
        catch
        {
            return 0;
        }
    }
}
