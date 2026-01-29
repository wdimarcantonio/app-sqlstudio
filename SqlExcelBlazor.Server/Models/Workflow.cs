using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Represents a workflow with multiple steps
/// </summary>
public class Workflow
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the workflow
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the workflow
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this workflow is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional schedule in cron format
    /// </summary>
    [MaxLength(100)]
    public string? Schedule { get; set; }

    /// <summary>
    /// Date and time when this workflow was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when this workflow was last modified
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// List of steps in this workflow
    /// </summary>
    public List<WorkflowStep> Steps { get; set; } = new();

    /// <summary>
    /// List of execution results for this workflow
    /// </summary>
    public List<WorkflowExecutionResult> Executions { get; set; } = new();
}
