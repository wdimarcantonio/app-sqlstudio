using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SqlExcelBlazor.Server.Models.Connections.Enums;

namespace SqlExcelBlazor.Server.Models.Connections;

public class WebServiceConnection : Connection
{
    [Required]
    [Url]
    public string BaseUrl { get; set; } = string.Empty;
    
    public AuthenticationType AuthType { get; set; }
    
    // JSON serialized auth configuration
    public string? AuthConfigJson { get; set; }
    
    [NotMapped]
    public Dictionary<string, string> AuthConfig
    {
        get => string.IsNullOrEmpty(AuthConfigJson) 
            ? new Dictionary<string, string>() 
            : JsonSerializer.Deserialize<Dictionary<string, string>>(AuthConfigJson) ?? new Dictionary<string, string>();
        set => AuthConfigJson = JsonSerializer.Serialize(value);
    }
    
    // JSON serialized default headers
    public string? DefaultHeadersJson { get; set; }
    
    [NotMapped]
    public Dictionary<string, string> DefaultHeaders
    {
        get => string.IsNullOrEmpty(DefaultHeadersJson) 
            ? new Dictionary<string, string>() 
            : JsonSerializer.Deserialize<Dictionary<string, string>>(DefaultHeadersJson) ?? new Dictionary<string, string>();
        set => DefaultHeadersJson = JsonSerializer.Serialize(value);
    }
    
    public int TimeoutSeconds { get; set; } = 30;
    
    public override string GetConnectionString()
    {
        return BaseUrl;
    }
    
    public override async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(TimeoutSeconds) };
            
            var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl);
            
            // Add authentication
            AddAuthentication(request);
            
            // Add default headers
            foreach (var header in DefaultHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            
            var response = await httpClient.SendAsync(request);
            sw.Stop();
            
            return new ConnectionTestResult
            {
                Success = response.IsSuccessStatusCode,
                Message = $"HTTP {(int)response.StatusCode} {response.StatusCode}",
                ResponseTime = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                ResponseTime = sw.Elapsed
            };
        }
    }
    
    private void AddAuthentication(HttpRequestMessage request)
    {
        switch (AuthType)
        {
            case AuthenticationType.ApiKey:
                var headerName = AuthConfig.GetValueOrDefault("HeaderName", "X-API-Key");
                var apiKey = AuthConfig.GetValueOrDefault("ApiKey", "");
                request.Headers.TryAddWithoutValidation(headerName, apiKey);
                break;
                
            case AuthenticationType.BearerToken:
                var token = AuthConfig.GetValueOrDefault("Token", "");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                break;
                
            case AuthenticationType.BasicAuth:
                var username = AuthConfig.GetValueOrDefault("Username", "");
                var password = AuthConfig.GetValueOrDefault("Password", "");
                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                break;
        }
    }
}
