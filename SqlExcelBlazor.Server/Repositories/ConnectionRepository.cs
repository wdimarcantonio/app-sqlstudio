using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Data;
using SqlExcelBlazor.Server.Models.Connections;

namespace SqlExcelBlazor.Server.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly ApplicationDbContext _context;
    
    public ConnectionRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Connection>> GetAllAsync()
    {
        return await _context.Connections
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Connection?> GetByIdAsync(int id)
    {
        return await _context.Connections.FindAsync(id);
    }
    
    public async Task<T?> GetByIdAsync<T>(int id) where T : Connection
    {
        return await _context.Connections.OfType<T>().FirstOrDefaultAsync(c => c.Id == id);
    }
    
    public async Task<Connection> CreateAsync(Connection connection)
    {
        connection.CreatedAt = DateTime.UtcNow;
        _context.Connections.Add(connection);
        await _context.SaveChangesAsync();
        return connection;
    }
    
    public async Task<Connection> UpdateAsync(Connection connection)
    {
        _context.Entry(connection).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return connection;
    }
    
    public async Task DeleteAsync(int id)
    {
        var connection = await _context.Connections.FindAsync(id);
        if (connection != null)
        {
            _context.Connections.Remove(connection);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<int> GetCountAsync()
    {
        return await _context.Connections.CountAsync();
    }
}
