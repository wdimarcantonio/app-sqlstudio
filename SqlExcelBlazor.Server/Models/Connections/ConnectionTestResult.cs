namespace SqlExcelBlazor.Server.Models.Connections;

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
