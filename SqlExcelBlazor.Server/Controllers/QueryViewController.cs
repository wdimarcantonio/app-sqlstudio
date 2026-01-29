using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Data;
using SqlExcelBlazor.Server.Models;

namespace SqlExcelBlazor.Server.Controllers;

/// <summary>
/// Controller for managing query views
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QueryViewController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<QueryViewController> _logger;

    public QueryViewController(ApplicationDbContext dbContext, ILogger<QueryViewController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all query views
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var queryViews = await _dbContext.QueryViews
                .Include(qv => qv.Parameters)
                .OrderBy(qv => qv.Name)
                .ToListAsync();

            return Ok(queryViews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting query views");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific query view by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var queryView = await _dbContext.QueryViews
                .Include(qv => qv.Parameters)
                .FirstOrDefaultAsync(qv => qv.Id == id);

            if (queryView == null)
            {
                return NotFound(new { error = $"QueryView {id} not found" });
            }

            return Ok(queryView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting query view {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new query view
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] QueryView queryView)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            queryView.CreatedAt = DateTime.UtcNow;
            _dbContext.QueryViews.Add(queryView);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Created query view {queryView.Id}: {queryView.Name}");
            return CreatedAtAction(nameof(GetById), new { id = queryView.Id }, queryView);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating query view");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing query view
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] QueryView queryView)
    {
        try
        {
            if (id != queryView.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existing = await _dbContext.QueryViews
                .Include(qv => qv.Parameters)
                .FirstOrDefaultAsync(qv => qv.Id == id);

            if (existing == null)
            {
                return NotFound(new { error = $"QueryView {id} not found" });
            }

            // Update properties
            existing.Name = queryView.Name;
            existing.Description = queryView.Description;
            existing.SqlQuery = queryView.SqlQuery;
            existing.ConnectionString = queryView.ConnectionString;

            // Update parameters
            _dbContext.QueryParameters.RemoveRange(existing.Parameters);
            existing.Parameters = queryView.Parameters;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Updated query view {id}: {queryView.Name}");
            return Ok(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating query view {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a query view
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var queryView = await _dbContext.QueryViews.FindAsync(id);

            if (queryView == null)
            {
                return NotFound(new { error = $"QueryView {id} not found" });
            }

            _dbContext.QueryViews.Remove(queryView);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Deleted query view {id}");
            return Ok(new { success = true, message = "Query view deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting query view {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a query view and return results
    /// </summary>
    [HttpPost("{id}/execute")]
    public async Task<IActionResult> Execute(int id, [FromBody] Dictionary<string, string>? parameters = null)
    {
        try
        {
            var queryView = await _dbContext.QueryViews
                .Include(qv => qv.Parameters)
                .FirstOrDefaultAsync(qv => qv.Id == id);

            if (queryView == null)
            {
                return NotFound(new { error = $"QueryView {id} not found" });
            }

            // Replace parameters
            var sqlQuery = queryView.SqlQuery;
            parameters ??= new Dictionary<string, string>();

            foreach (var param in queryView.Parameters)
            {
                var value = parameters.TryGetValue(param.Name, out var v) 
                    ? v 
                    : param.DefaultValue ?? string.Empty;
                
                sqlQuery = sqlQuery.Replace(param.Name, value);
            }

            // Execute query (simplified - using SQLite)
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(queryView.ConnectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = sqlQuery;

            var dataTable = new System.Data.DataTable();
            using var reader = await command.ExecuteReaderAsync();
            dataTable.Load(reader);

            // Update last executed time
            queryView.LastExecuted = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Executed query view {id}: {queryView.Name}");

            return Ok(new
            {
                success = true,
                rowCount = dataTable.Rows.Count,
                columns = dataTable.Columns.Cast<System.Data.DataColumn>().Select(c => c.ColumnName).ToList(),
                rows = dataTable.Rows.Cast<System.Data.DataRow>().Select(r => r.ItemArray).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing query view {id}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
