using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Data;
using SqlExcelBlazor.Server.Models;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

/// <summary>
/// Controller for managing workflows
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ILogger<WorkflowController> _logger;

    public WorkflowController(
        ApplicationDbContext dbContext,
        IWorkflowEngine workflowEngine,
        ILogger<WorkflowController> logger)
    {
        _dbContext = dbContext;
        _workflowEngine = workflowEngine;
        _logger = logger;
    }

    /// <summary>
    /// Get all workflows
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var workflows = await _dbContext.Workflows
                .Include(w => w.Steps)
                .OrderBy(w => w.Name)
                .ToListAsync();

            return Ok(workflows);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workflows");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific workflow by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var workflow = await _dbContext.Workflows
                .Include(w => w.Steps.OrderBy(s => s.Order))
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workflow == null)
            {
                return NotFound(new { error = $"Workflow {id} not found" });
            }

            return Ok(workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new workflow
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Workflow workflow)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            workflow.CreatedAt = DateTime.UtcNow;
            _dbContext.Workflows.Add(workflow);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Created workflow {workflow.Id}: {workflow.Name}");
            return CreatedAtAction(nameof(GetById), new { id = workflow.Id }, workflow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing workflow
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Workflow workflow)
    {
        try
        {
            if (id != workflow.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _dbContext.Workflows
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (existing == null)
            {
                return NotFound(new { error = $"Workflow {id} not found" });
            }

            // Update properties
            existing.Name = workflow.Name;
            existing.Description = workflow.Description;
            existing.IsActive = workflow.IsActive;
            existing.Schedule = workflow.Schedule;
            existing.ModifiedAt = DateTime.UtcNow;

            // Update steps
            _dbContext.WorkflowSteps.RemoveRange(existing.Steps);
            existing.Steps = workflow.Steps;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Updated workflow {id}: {workflow.Name}");
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a workflow
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var workflow = await _dbContext.Workflows.FindAsync(id);

            if (workflow == null)
            {
                return NotFound(new { error = $"Workflow {id} not found" });
            }

            _dbContext.Workflows.Remove(workflow);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Deleted workflow {id}");
            return Ok(new { success = true, message = "Workflow deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a workflow
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] Dictionary<string, object>? initialVariables = null)
    {
        try
        {
            _logger.LogInformation($"Starting execution of workflow {id}");

            var result = await _workflowEngine.ExecuteAsync(id, initialVariables ?? new Dictionary<string, object>());

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get execution history for a workflow
    /// </summary>
    [HttpGet("{id}/executions")]
    public async Task<IActionResult> GetExecutions(int id, [FromQuery] int? limit = 50)
    {
        try
        {
            var executions = await _dbContext.WorkflowExecutionResults
                .Include(e => e.StepResults)
                .Where(e => e.WorkflowId == id)
                .OrderByDescending(e => e.StartTime)
                .Take(limit ?? 50)
                .ToListAsync();

            return Ok(executions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting executions for workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get details of a specific execution
    /// </summary>
    [HttpGet("executions/{executionId}")]
    public async Task<IActionResult> GetExecutionDetails(int executionId)
    {
        try
        {
            var execution = await _dbContext.WorkflowExecutionResults
                .Include(e => e.Workflow)
                .Include(e => e.StepResults.OrderBy(sr => sr.StepOrder))
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution == null)
            {
                return NotFound(new { error = $"Execution {executionId} not found" });
            }

            return Ok(execution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting execution details for {executionId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get workflow statistics
    /// </summary>
    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetStatistics(int id)
    {
        try
        {
            var executions = await _dbContext.WorkflowExecutionResults
                .Where(e => e.WorkflowId == id)
                .ToListAsync();

            var stats = new
            {
                totalExecutions = executions.Count,
                successfulExecutions = executions.Count(e => e.Success),
                failedExecutions = executions.Count(e => !e.Success),
                averageDurationSeconds = executions.Any() 
                    ? executions.Average(e => e.DurationSeconds) 
                    : 0,
                lastExecution = executions.OrderByDescending(e => e.StartTime).FirstOrDefault()?.StartTime
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting statistics for workflow {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
