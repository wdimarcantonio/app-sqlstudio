using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models.Connections;

public class CsvConnection : Connection
{
    [Required]
    public string FilePath { get; set; } = string.Empty;
    
    public bool HasHeaders { get; set; } = true;
    
    public string Delimiter { get; set; } = ",";
    
    public string? Encoding { get; set; } = "UTF-8";
    
    public override string GetConnectionString()
    {
        return FilePath;
    }
    
    public override Task<ConnectionTestResult> TestConnectionAsync()
    {
        try
        {
            var fileExists = File.Exists(FilePath);
            
            return Task.FromResult(new ConnectionTestResult
            {
                Success = fileExists,
                Message = fileExists ? "File found" : "File not found",
                ResponseTime = TimeSpan.Zero
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ConnectionTestResult
            {
                Success = false,
                Message = ex.Message,
                ResponseTime = TimeSpan.Zero
            });
        }
    }
}
