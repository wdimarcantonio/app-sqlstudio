using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Models.Connections;

namespace SqlExcelBlazor.Server.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Connection> Connections { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure TPH (Table-Per-Hierarchy) for Connection
        modelBuilder.Entity<Connection>()
            .HasDiscriminator<string>("Discriminator")
            .HasValue<SqlServerConnection>("SqlServer")
            .HasValue<PostgreSqlConnection>("PostgreSQL")
            .HasValue<MySqlConnection>("MySQL")
            .HasValue<WebServiceConnection>("WebService")
            .HasValue<ExcelConnection>("Excel")
            .HasValue<CsvConnection>("CSV");
        
        // Indexes
        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.Name)
            .IsUnique();
        
        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.Type);
        
        modelBuilder.Entity<Connection>()
            .HasIndex(c => c.IsActive);
    }
}
