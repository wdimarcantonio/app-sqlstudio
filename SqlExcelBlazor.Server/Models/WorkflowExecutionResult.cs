using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Result of a workflow execution
/// </summary>
public class WorkflowExecutionResult
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
    /// Start time of execution
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End time of execution
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Whether the workflow succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of steps
    /// </summary>
    public int TotalSteps { get; set; }

    /// <summary>
    /// Number of steps completed
    /// </summary>
    public int CompletedSteps { get; set; }

    /// <summary>
    /// Navigation property to parent Workflow
    /// </summary>
    public Workflow? Workflow { get; set; }

    /// <summary>
    /// Results for each step
    /// </summary>
    public List<StepResult> StepResults { get; set; } = new();

    /// <summary>
    /// Duration of execution in seconds
    /// </summary>
    public double DurationSeconds
    {
        get
        {
            if (EndTime.HasValue)
            {
                return (EndTime.Value - StartTime).TotalSeconds;
            }
            return (DateTime.UtcNow - StartTime).TotalSeconds;
        }
    }
}
