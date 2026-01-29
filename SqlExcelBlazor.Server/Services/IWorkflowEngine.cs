using SqlExcelBlazor.Server.Models;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Interface for the workflow execution engine
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Execute a workflow
    /// </summary>
    /// <param name="workflowId">ID of the workflow to execute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteAsync(int workflowId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a workflow with initial context variables
    /// </summary>
    /// <param name="workflowId">ID of the workflow to execute</param>
    /// <param name="initialVariables">Initial variables for the context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution result</returns>
    Task<WorkflowExecutionResult> ExecuteAsync(int workflowId, Dictionary<string, object> initialVariables, 
        CancellationToken cancellationToken = default);
}
