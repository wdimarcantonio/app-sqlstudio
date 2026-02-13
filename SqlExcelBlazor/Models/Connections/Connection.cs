namespace SqlExcelBlazor.Models.Connections;

public class Connection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConnectionType Type { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastTested { get; set; }
    public string? Description { get; set; }
}

public class ConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}
