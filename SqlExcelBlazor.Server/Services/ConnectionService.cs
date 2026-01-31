using SqlExcelBlazor.Server.Models.Connections;
using SqlExcelBlazor.Server.Repositories;

namespace SqlExcelBlazor.Server.Services;

public class ConnectionService : IConnectionService
{
    private readonly IConnectionRepository _repository;
    private readonly ILogger<ConnectionService> _logger;
    
    public ConnectionService(IConnectionRepository repository, ILogger<ConnectionService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<List<Connection>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }
    
    public async Task<Connection?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
    
    public async Task<T?> GetByIdAsync<T>(int id) where T : Connection
    {
        return await _repository.GetByIdAsync<T>(id);
    }
    
    public async Task<Connection> CreateAsync(Connection connection)
    {
        _logger.LogInformation("Creating new connection: {Name} ({Type})", connection.Name, connection.Type);
        return await _repository.CreateAsync(connection);
    }
    
    public async Task<Connection> UpdateAsync(Connection connection)
    {
        _logger.LogInformation("Updating connection: {Id} - {Name}", connection.Id, connection.Name);
        return await _repository.UpdateAsync(connection);
    }
    
    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Deleting connection: {Id}", id);
        await _repository.DeleteAsync(id);
    }
    
    public async Task<ConnectionTestResult> TestConnectionAsync(int id)
    {
        var connection = await _repository.GetByIdAsync(id);
        if (connection == null)
        {
            return new ConnectionTestResult
            {
                Success = false,
                Message = "Connection not found"
            };
        }
        
        _logger.LogInformation("Testing connection: {Id} - {Name}", id, connection.Name);
        var result = await connection.TestConnectionAsync();
        
        // Update LastTested timestamp
        connection.LastTested = DateTime.UtcNow;
        await _repository.UpdateAsync(connection);
        
        return result;
    }
    
    public async Task<int> GetCountAsync()
    {
        return await _repository.GetCountAsync();
    }
}
