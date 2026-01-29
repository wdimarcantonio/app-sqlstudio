using Microsoft.EntityFrameworkCore;
using SqlExcelBlazor.Server.Models;

namespace SqlExcelBlazor.Server.Data;

/// <summary>
/// Database context for the workflow system
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Query views that can be reused
    /// </summary>
    public DbSet<QueryView> QueryViews { get; set; } = null!;

    /// <summary>
    /// Parameters for query views
    /// </summary>
    public DbSet<QueryParameter> QueryParameters { get; set; } = null!;

    /// <summary>
    /// Workflows
    /// </summary>
    public DbSet<Workflow> Workflows { get; set; } = null!;

    /// <summary>
    /// Workflow steps
    /// </summary>
    public DbSet<WorkflowStep> WorkflowSteps { get; set; } = null!;

    /// <summary>
    /// Workflow execution results
    /// </summary>
    public DbSet<WorkflowExecutionResult> WorkflowExecutionResults { get; set; } = null!;

    /// <summary>
    /// Step results
    /// </summary>
    public DbSet<StepResult> StepResults { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure QueryView relationships
        modelBuilder.Entity<QueryView>()
            .HasMany(qv => qv.Parameters)
            .WithOne(qp => qp.QueryView)
            .HasForeignKey(qp => qp.QueryViewId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Workflow relationships
        modelBuilder.Entity<Workflow>()
            .HasMany(w => w.Steps)
            .WithOne(ws => ws.Workflow)
            .HasForeignKey(ws => ws.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Workflow>()
            .HasMany(w => w.Executions)
            .WithOne(we => we.Workflow)
            .HasForeignKey(we => we.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure WorkflowExecutionResult relationships
        modelBuilder.Entity<WorkflowExecutionResult>()
            .HasMany(wer => wer.StepResults)
            .WithOne(sr => sr.WorkflowExecutionResult)
            .HasForeignKey(sr => sr.WorkflowExecutionResultId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes for better query performance
        modelBuilder.Entity<QueryView>()
            .HasIndex(qv => qv.Name);

        modelBuilder.Entity<Workflow>()
            .HasIndex(w => w.Name);

        modelBuilder.Entity<Workflow>()
            .HasIndex(w => w.IsActive);

        modelBuilder.Entity<WorkflowStep>()
            .HasIndex(ws => new { ws.WorkflowId, ws.Order });

        modelBuilder.Entity<WorkflowExecutionResult>()
            .HasIndex(wer => wer.StartTime);
    }
}
