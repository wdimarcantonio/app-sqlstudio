using SqlExcelBlazor.Server.Models;
using SqlExcelBlazor.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Workflow execution engine
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IStepExecutor> _stepExecutors;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        ApplicationDbContext dbContext,
        IEnumerable<IStepExecutor> stepExecutors,
        ILogger<WorkflowEngine> logger)
    {
        _dbContext = dbContext;
        _stepExecutors = stepExecutors;
        _logger = logger;
    }

    public Task<WorkflowExecutionResult> ExecuteAsync(int workflowId, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(workflowId, new Dictionary<string, object>(), cancellationToken);
    }

    public async Task<WorkflowExecutionResult> ExecuteAsync(int workflowId, Dictionary<string, object> initialVariables, 
        CancellationToken cancellationToken = default)
    {
        var executionResult = new WorkflowExecutionResult
        {
            WorkflowId = workflowId,
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation($"Starting workflow execution for workflow {workflowId}");

            // Load workflow with steps
            var workflow = await _dbContext.Workflows
                .Include(w => w.Steps.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken);

            if (workflow == null)
            {
                throw new Exception($"Workflow {workflowId} not found");
            }

            if (!workflow.IsActive)
            {
                throw new Exception($"Workflow {workflowId} is not active");
            }

            executionResult.TotalSteps = workflow.Steps.Count;

            // Initialize workflow context
            var context = new WorkflowContext
            {
                Variables = new Dictionary<string, object>(initialVariables),
                CancellationToken = cancellationToken,
                StartTime = DateTime.UtcNow
            };

            _logger.LogInformation($"Executing {workflow.Steps.Count} steps");

            // Execute steps sequentially
            foreach (var step in workflow.Steps.OrderBy(s => s.Order))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("Workflow execution cancelled");
                    break;
                }

                _logger.LogInformation($"Executing step {step.Order}: {step.Name} ({step.Type})");

                StepResult stepResult;

                try
                {
                    // Find appropriate executor
                    var executor = _stepExecutors.FirstOrDefault(e => e.StepType == step.Type);
                    if (executor == null)
                    {
                        throw new Exception($"No executor found for step type {step.Type}");
                    }

                    // Execute with timeout
                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(step.TimeoutSeconds));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
                    var timeoutContext = new WorkflowContext
                    {
                        Variables = context.Variables,
                        DataTables = context.DataTables,
                        StartTime = context.StartTime,
                        CancellationToken = linkedCts.Token
                    };

                    stepResult = await executor.ExecuteAsync(step, timeoutContext, _logger);
                    
                    // Update main context with any changes
                    context.Variables = timeoutContext.Variables;
                    context.DataTables = timeoutContext.DataTables;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning($"Step {step.Name} was cancelled");
                    stepResult = new StepResult
                    {
                        StepOrder = step.Order,
                        StepName = step.Name,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = "Step execution cancelled"
                    };
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError($"Step {step.Name} timed out after {step.TimeoutSeconds} seconds");
                    stepResult = new StepResult
                    {
                        StepOrder = step.Order,
                        StepName = step.Name,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = $"Step timed out after {step.TimeoutSeconds} seconds"
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Unexpected error executing step {step.Name}");
                    stepResult = new StepResult
                    {
                        StepOrder = step.Order,
                        StepName = step.Name,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                        Success = false,
                        ErrorMessage = ex.Message,
                        LogDetails = $"Error: {ex.Message}\nStack Trace: {ex.StackTrace}"
                    };
                }

                executionResult.StepResults.Add(stepResult);

                if (stepResult.Success)
                {
                    executionResult.CompletedSteps++;
                    _logger.LogInformation($"Step {step.Name} completed successfully");

                    // Handle OnSuccess action
                    if (!string.IsNullOrEmpty(step.OnSuccess))
                    {
                        if (step.OnSuccess.Equals("end", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("OnSuccess action is 'end', stopping workflow");
                            break;
                        }
                        else if (step.OnSuccess.StartsWith("skip_to:", StringComparison.OrdinalIgnoreCase))
                        {
                            var skipToStepStr = step.OnSuccess.Substring("skip_to:".Length);
                            if (int.TryParse(skipToStepStr, out var skipToStep))
                            {
                                _logger.LogInformation($"OnSuccess action: skipping to step {skipToStep}");
                                // Skip to the specified step (implementation would need to handle this in the loop)
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogError($"Step {step.Name} failed: {stepResult.ErrorMessage}");

                    // Handle retry logic
                    if (stepResult.RetryCount < step.MaxRetries)
                    {
                        stepResult.RetryCount++;
                        _logger.LogInformation($"Retrying step {step.Name} (attempt {stepResult.RetryCount}/{step.MaxRetries})");
                        
                        // Retry after a delay
                        await Task.Delay(1000 * stepResult.RetryCount, cancellationToken);
                        
                        // Re-execute the step (would need to implement retry logic here)
                        // For now, we'll just continue
                    }

                    // Handle OnError action
                    if (!string.IsNullOrEmpty(step.OnError))
                    {
                        if (step.OnError.Equals("end", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("OnError action is 'end', stopping workflow");
                            break;
                        }
                        else if (step.OnError.Equals("continue", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("OnError action is 'continue', proceeding to next step");
                            continue;
                        }
                    }
                    else
                    {
                        // Default behavior: stop on error
                        _logger.LogError("Step failed and no OnError action specified, stopping workflow");
                        break;
                    }
                }
            }

            // Determine overall success
            executionResult.Success = executionResult.StepResults.All(sr => sr.Success) 
                && executionResult.CompletedSteps == executionResult.TotalSteps;
            executionResult.EndTime = DateTime.UtcNow;

            if (!executionResult.Success)
            {
                var failedSteps = executionResult.StepResults.Where(sr => !sr.Success).ToList();
                executionResult.ErrorMessage = $"{failedSteps.Count} step(s) failed: " + 
                    string.Join(", ", failedSteps.Select(s => s.StepName));
            }

            _logger.LogInformation($"Workflow execution completed. Success: {executionResult.Success}, " +
                $"Completed: {executionResult.CompletedSteps}/{executionResult.TotalSteps}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during workflow execution");
            executionResult.Success = false;
            executionResult.EndTime = DateTime.UtcNow;
            executionResult.ErrorMessage = ex.Message;
        }

        // Save execution result to database
        try
        {
            _dbContext.WorkflowExecutionResults.Add(executionResult);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Workflow execution result saved with ID {executionResult.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save workflow execution result");
        }

        return executionResult;
    }
}
