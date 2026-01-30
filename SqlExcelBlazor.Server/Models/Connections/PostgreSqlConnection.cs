using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Npgsql;

namespace SqlExcelBlazor.Server.Models.Connections;

public class PostgreSqlConnection : Connection
{
    [Required]
    public string Host { get; set; } = string.Empty;
    
    public int Port { get; set; } = 5432;
    
    [Required]
    public string Database { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public int ConnectionTimeout { get; set; } = 30;
    
    public override string GetConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Database,
            Username = Username,
            Password = Password,
            Timeout = ConnectionTimeout
        };
        
        return builder.ConnectionString;
    }
    
    public override async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var connection = new NpgsqlConnection(GetConnectionString());
            await connection.OpenAsync();
            sw.Stop();
            
            return new ConnectionTestResult
            {
                Success = true,
                Message = "Connection successful",
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
}
