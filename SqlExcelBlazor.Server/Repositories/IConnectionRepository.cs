using SqlExcelBlazor.Server.Models.Connections;

namespace SqlExcelBlazor.Server.Repositories;

public interface IConnectionRepository
{
    Task<List<Connection>> GetAllAsync();
    Task<Connection?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync<T>(int id) where T : Connection;
    Task<Connection> CreateAsync(Connection connection);
    Task<Connection> UpdateAsync(Connection connection);
    Task DeleteAsync(int id);
    Task<int> GetCountAsync();
}
