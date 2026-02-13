using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace SqlExcelBlazor.Server.Models.Connections;

public class SqlServerConnection : Connection
{
    [Required]
    public string Server { get; set; } = string.Empty;
    
    public int Port { get; set; } = 1433;
    
    [Required]
    public string Database { get; set; } = string.Empty;
    
    public bool IntegratedSecurity { get; set; }
    
    public string? Username { get; set; }
    
    public string? Password { get; set; }
    
    public bool TrustServerCertificate { get; set; }
    
    public int ConnectionTimeout { get; set; } = 30;
    
    public override string GetConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = Port == 1433 ? Server : $"{Server},{Port}",
            InitialCatalog = Database,
            IntegratedSecurity = IntegratedSecurity,
            ConnectTimeout = ConnectionTimeout,
            TrustServerCertificate = TrustServerCertificate
        };
        
        if (!IntegratedSecurity && !string.IsNullOrEmpty(Username))
        {
            builder.UserID = Username;
            builder.Password = Password;
        }
        
        return builder.ConnectionString;
    }
    
    public override async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
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
