using System.ComponentModel.DataAnnotations;
using SqlExcelBlazor.Server.Models.Connections.Enums;

namespace SqlExcelBlazor.Server.Models.Connections;

public abstract class Connection
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public ConnectionType Type { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastTested { get; set; }
    
    public string? Description { get; set; }
    
    // Discriminator for EF Core TPH (Table-Per-Hierarchy)
    public string Discriminator { get; set; } = string.Empty;
    
    // Abstract method for connection testing
    public abstract Task<ConnectionTestResult> TestConnectionAsync();
    
    // Abstract method for getting connection string
    public abstract string GetConnectionString();
}
