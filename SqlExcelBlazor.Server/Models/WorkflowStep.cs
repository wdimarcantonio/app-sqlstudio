using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Types of workflow steps
/// </summary>
public enum StepType
{
    ExecuteQuery,
    DataTransfer,
    WebServiceCall,
    Transformation,
    Validation,
    Notification
}

/// <summary>
/// Represents a single step in a workflow
/// </summary>
public class WorkflowStep
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Workflow
    /// </summary>
    public int WorkflowId { get; set; }

    /// <summary>
    /// Order of execution (1-based)
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Name of this step
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of step
    /// </summary>
    public StepType Type { get; set; }

    /// <summary>
    /// Configuration as JSON
    /// </summary>
    [Required]
    public string Configuration { get; set; } = "{}";

    /// <summary>
    /// What to do on success (continue, skip to step, end)
    /// </summary>
    [MaxLength(100)]
    public string? OnSuccess { get; set; }

    /// <summary>
    /// What to do on error (continue, skip to step, end, retry)
    /// </summary>
    [MaxLength(100)]
    public string? OnError { get; set; }

    /// <summary>
    /// Maximum number of retries on error
    /// </summary>
    public int MaxRetries { get; set; } = 0;

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Navigation property to parent Workflow
    /// </summary>
    public Workflow? Workflow { get; set; }

    /// <summary>
    /// Parse configuration as specific type
    /// </summary>
    public T? GetConfiguration<T>() where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(Configuration);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Set configuration from object
    /// </summary>
    public void SetConfiguration<T>(T config) where T : class
    {
        Configuration = JsonSerializer.Serialize(config);
    }
}
