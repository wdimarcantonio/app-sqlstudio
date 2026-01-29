using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Result of a single step execution
/// </summary>
public class StepResult
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to WorkflowExecutionResult
    /// </summary>
    public int WorkflowExecutionResultId { get; set; }

    /// <summary>
    /// Step order
    /// </summary>
    public int StepOrder { get; set; }

    /// <summary>
    /// Step name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Start time of step execution
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// End time of step execution
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Whether the step succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of records processed
    /// </summary>
    public int RecordsProcessed { get; set; }

    /// <summary>
    /// Number of records that failed
    /// </summary>
    public int RecordsFailed { get; set; }

    /// <summary>
    /// Additional log information
    /// </summary>
    public string? LogDetails { get; set; }

    /// <summary>
    /// Number of retries attempted
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Navigation property to parent WorkflowExecutionResult
    /// </summary>
    public WorkflowExecutionResult? WorkflowExecutionResult { get; set; }

    /// <summary>
    /// Duration of step execution in seconds
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
