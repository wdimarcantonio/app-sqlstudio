using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using MySqlConnector;

namespace SqlExcelBlazor.Server.Models.Connections;

public class MySqlConnection : Connection
{
    [Required]
    public string Server { get; set; } = string.Empty;
    
    public int Port { get; set; } = 3306;
    
    [Required]
    public string Database { get; set; } = string.Empty;
    
    [Required]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public int ConnectionTimeout { get; set; } = 30;
    
    public override string GetConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Server,
            Port = (uint)Port,
            Database = Database,
            UserID = Username,
            Password = Password,
            ConnectionTimeout = (uint)ConnectionTimeout
        };
        
        return builder.ConnectionString;
    }
    
    public override async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var connection = new MySqlConnector.MySqlConnection(GetConnectionString());
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
