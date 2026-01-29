using SqlExcelBlazor.Server.Models;
using SqlExcelBlazor.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.Sqlite;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Executes QueryView steps in a workflow
/// </summary>
public class ExecuteQueryStepExecutor : IStepExecutor
{
    private readonly ApplicationDbContext _dbContext;

    public ExecuteQueryStepExecutor(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public StepType StepType => StepType.ExecuteQuery;

    public async Task<StepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, ILogger logger)
    {
        var result = new StepResult
        {
            StepOrder = step.Order,
            StepName = step.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Parse configuration
            var config = step.GetConfiguration<ExecuteQueryStepConfig>();
            if (config == null)
            {
                throw new Exception("Invalid ExecuteQuery configuration");
            }

            logger.LogInformation($"Executing QueryView {config.QueryViewId}");

            // Load QueryView
            var queryView = await _dbContext.QueryViews
                .Include(qv => qv.Parameters)
                .FirstOrDefaultAsync(qv => qv.Id == config.QueryViewId, context.CancellationToken);

            if (queryView == null)
            {
                throw new Exception($"QueryView {config.QueryViewId} not found");
            }

            // Replace parameters in query
            var sqlQuery = queryView.SqlQuery;
            foreach (var param in queryView.Parameters)
            {
                var value = config.ParameterValues.TryGetValue(param.Name, out var v) 
                    ? v 
                    : param.DefaultValue ?? string.Empty;
                
                sqlQuery = sqlQuery.Replace(param.Name, value);
            }

            logger.LogInformation($"Executing query: {sqlQuery}");

            // Execute query
            var dataTable = await ExecuteQueryAsync(queryView.ConnectionString, sqlQuery, context.CancellationToken);

            result.RecordsProcessed = dataTable.Rows.Count;

            // Store result in context
            context.SetDataTable(config.ResultKey, dataTable);

            // Update last executed time
            queryView.LastExecuted = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(context.CancellationToken);

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.LogDetails = $"Query executed successfully. {dataTable.Rows.Count} rows returned.";

            logger.LogInformation($"Query executed successfully. {dataTable.Rows.Count} rows returned.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;
            result.LogDetails = $"Error: {ex.Message}\nStack Trace: {ex.StackTrace}";
            logger.LogError(ex, $"Error executing query step: {step.Name}");
        }

        return result;
    }

    private async Task<DataTable> ExecuteQueryAsync(string connectionString, string query, CancellationToken cancellationToken)
    {
        var dataTable = new DataTable();

        // Determine connection type based on connection string
        if (connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) 
            && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            // SQLite
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = query;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            dataTable.Load(reader);
        }
        else
        {
            // SQL Server
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            using var command = connection.CreateCommand();
            command.CommandText = query;
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            dataTable.Load(reader);
        }

        return dataTable;
    }
}
