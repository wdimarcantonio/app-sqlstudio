using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Models.Connections;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionController : ControllerBase
{
    private readonly IConnectionService _connectionService;
    private readonly ILogger<ConnectionController> _logger;
    
    public ConnectionController(IConnectionService connectionService, ILogger<ConnectionController> logger)
    {
        _connectionService = connectionService;
        _logger = logger;
    }
    
    [HttpGet]
    public async Task<ActionResult<List<Connection>>> GetAll()
    {
        try
        {
            var connections = await _connectionService.GetAllAsync();
            return Ok(connections);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all connections");
            return StatusCode(500, new { error = "An error occurred while retrieving connections" });
        }
    }
    
    [HttpGet("{id}")]
    public async Task<ActionResult<Connection>> GetById(int id)
    {
        try
        {
            var connection = await _connectionService.GetByIdAsync(id);
            if (connection == null)
                return NotFound(new { error = "Connection not found" });
            
            return Ok(connection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the connection" });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Connection>> Create([FromBody] Connection connection)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var created = await _connectionService.CreateAsync(connection);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connection");
            return StatusCode(500, new { error = "An error occurred while creating the connection" });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Connection>> Update(int id, [FromBody] Connection connection)
    {
        try
        {
            if (id != connection.Id)
                return BadRequest(new { error = "ID mismatch" });
            
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            var updated = await _connectionService.UpdateAsync(connection);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connection {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the connection" });
        }
    }
    
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _connectionService.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting connection {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the connection" });
        }
    }
    
    [HttpPost("{id}/test")]
    public async Task<ActionResult<ConnectionTestResult>> TestConnection(int id)
    {
        try
        {
            var result = await _connectionService.TestConnectionAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection {Id}", id);
            return StatusCode(500, new { error = "An error occurred while testing the connection" });
        }
    }
    
    [HttpGet("count")]
    public async Task<ActionResult<int>> GetCount()
    {
        try
        {
            var count = await _connectionService.GetCountAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection count");
            return StatusCode(500, new { error = "An error occurred while counting connections" });
        }
    }
}
