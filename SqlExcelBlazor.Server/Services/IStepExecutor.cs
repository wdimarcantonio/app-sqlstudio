using SqlExcelBlazor.Server.Models;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Interface for workflow step executors
/// </summary>
public interface IStepExecutor
{
    /// <summary>
    /// The type of step this executor handles
    /// </summary>
    StepType StepType { get; }

    /// <summary>
    /// Execute a workflow step
    /// </summary>
    /// <param name="step">The step to execute</param>
    /// <param name="context">The workflow context</param>
    /// <param name="logger">Logger for tracking execution</param>
    /// <returns>Result of the step execution</returns>
    Task<StepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, ILogger logger);
}
