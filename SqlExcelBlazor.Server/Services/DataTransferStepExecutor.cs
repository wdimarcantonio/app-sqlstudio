using SqlExcelBlazor.Server.Models;
using SqlExcelBlazor.Server.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Executes data transfer steps in a workflow
/// </summary>
public class DataTransferStepExecutor : IStepExecutor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ExecuteQueryStepExecutor _queryExecutor;

    public DataTransferStepExecutor(ApplicationDbContext dbContext, ExecuteQueryStepExecutor queryExecutor)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
    }

    public StepType StepType => StepType.DataTransfer;

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
            var config = step.GetConfiguration<DataTransferStepConfig>();
            if (config == null)
            {
                throw new Exception("Invalid DataTransfer configuration");
            }

            logger.LogInformation($"Starting data transfer from QueryView {config.SourceQueryViewId} to {config.DestinationTableName}");

            // Execute source query to get data
            var sourceStep = new WorkflowStep
            {
                Order = step.Order,
                Name = $"{step.Name}_Source",
                Type = StepType.ExecuteQuery
            };
            sourceStep.SetConfiguration(new ExecuteQueryStepConfig
            {
                QueryViewId = config.SourceQueryViewId,
                ResultKey = $"DataTransfer_{step.Order}_Source"
            });

            var sourceResult = await _queryExecutor.ExecuteAsync(sourceStep, context, logger);
            if (!sourceResult.Success)
            {
                throw new Exception($"Failed to execute source query: {sourceResult.ErrorMessage}");
            }

            var sourceData = context.GetDataTable($"DataTransfer_{step.Order}_Source");
            if (sourceData == null || sourceData.Rows.Count == 0)
            {
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                result.LogDetails = "No data to transfer.";
                logger.LogInformation("No data to transfer.");
                return result;
            }

            // Transfer data to destination
            using var connection = new SqlConnection(config.DestinationConnectionString);
            await connection.OpenAsync(context.CancellationToken);

            // Handle different modes
            if (config.Mode.Equals("Truncate", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation($"Truncating table {config.DestinationTableName}");
                using var truncateCmd = connection.CreateCommand();
                truncateCmd.CommandText = $"TRUNCATE TABLE {config.DestinationTableName}";
                await truncateCmd.ExecuteNonQueryAsync(context.CancellationToken);
            }

            // Perform bulk insert
            if (config.Mode.Equals("Upsert", StringComparison.OrdinalIgnoreCase) && config.PrimaryKeyColumns.Any())
            {
                result.RecordsProcessed = await UpsertDataAsync(connection, sourceData, config, context.CancellationToken, logger);
            }
            else
            {
                result.RecordsProcessed = await BulkInsertDataAsync(connection, sourceData, config, context.CancellationToken, logger);
            }

            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.LogDetails = $"Transferred {result.RecordsProcessed} records successfully.";
            logger.LogInformation($"Data transfer completed. {result.RecordsProcessed} records transferred.");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;
            result.LogDetails = $"Error: {ex.Message}\nStack Trace: {ex.StackTrace}";
            logger.LogError(ex, $"Error in data transfer step: {step.Name}");
        }

        return result;
    }

    private async Task<int> BulkInsertDataAsync(SqlConnection connection, DataTable data, 
        DataTransferStepConfig config, CancellationToken cancellationToken, ILogger logger)
    {
        logger.LogInformation($"Performing bulk insert to {config.DestinationTableName}");

        using var bulkCopy = new SqlBulkCopy(connection)
        {
            DestinationTableName = config.DestinationTableName,
            BatchSize = config.BatchSize,
            BulkCopyTimeout = 300
        };

        // Map columns
        foreach (DataColumn column in data.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(data, cancellationToken);
        return data.Rows.Count;
    }

    private async Task<int> UpsertDataAsync(SqlConnection connection, DataTable data, 
        DataTransferStepConfig config, CancellationToken cancellationToken, ILogger logger)
    {
        logger.LogInformation($"Performing upsert to {config.DestinationTableName}");

        int recordsProcessed = 0;
        var transaction = connection.BeginTransaction();

        try
        {
            foreach (DataRow row in data.Rows)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                // Build WHERE clause for primary keys
                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                foreach (var pkColumn in config.PrimaryKeyColumns)
                {
                    if (data.Columns.Contains(pkColumn))
                    {
                        whereClauses.Add($"{pkColumn} = @pk_{pkColumn}");
                        parameters.Add(new SqlParameter($"@pk_{pkColumn}", row[pkColumn] ?? DBNull.Value));
                    }
                }

                if (!whereClauses.Any())
                {
                    throw new Exception("No primary key columns found in source data");
                }

                var whereClause = string.Join(" AND ", whereClauses);

                // Check if record exists
                var checkSql = $"SELECT COUNT(*) FROM {config.DestinationTableName} WHERE {whereClause}";
                using var checkCmd = connection.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = checkSql;
                checkCmd.Parameters.AddRange(parameters.ToArray());

                var count = (int)await checkCmd.ExecuteScalarAsync(cancellationToken);

                if (count > 0)
                {
                    // Update
                    var setClauses = new List<string>();
                    var updateParameters = new List<SqlParameter>();

                    foreach (DataColumn column in data.Columns)
                    {
                        if (!config.PrimaryKeyColumns.Contains(column.ColumnName))
                        {
                            setClauses.Add($"{column.ColumnName} = @{column.ColumnName}");
                            updateParameters.Add(new SqlParameter($"@{column.ColumnName}", row[column.ColumnName] ?? DBNull.Value));
                        }
                    }

                    if (setClauses.Any())
                    {
                        var updateSql = $"UPDATE {config.DestinationTableName} SET {string.Join(", ", setClauses)} WHERE {whereClause}";
                        using var updateCmd = connection.CreateCommand();
                        updateCmd.Transaction = transaction;
                        updateCmd.CommandText = updateSql;
                        updateCmd.Parameters.AddRange(updateParameters.ToArray());
                        updateCmd.Parameters.AddRange(parameters.ToArray());
                        await updateCmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                else
                {
                    // Insert
                    var columnNames = new List<string>();
                    var valueParams = new List<string>();
                    var insertParameters = new List<SqlParameter>();

                    foreach (DataColumn column in data.Columns)
                    {
                        columnNames.Add(column.ColumnName);
                        valueParams.Add($"@{column.ColumnName}");
                        insertParameters.Add(new SqlParameter($"@{column.ColumnName}", row[column.ColumnName] ?? DBNull.Value));
                    }

                    var insertSql = $"INSERT INTO {config.DestinationTableName} ({string.Join(", ", columnNames)}) VALUES ({string.Join(", ", valueParams)})";
                    using var insertCmd = connection.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = insertSql;
                    insertCmd.Parameters.AddRange(insertParameters.ToArray());
                    await insertCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                recordsProcessed++;
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return recordsProcessed;
    }
}
