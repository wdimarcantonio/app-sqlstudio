using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SqlExcelBlazor.Models;
using System.Data;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqlLoadController : ControllerBase
{
    private readonly ILogger<SqlLoadController> _logger;

    public SqlLoadController(ILogger<SqlLoadController> logger)
    {
        _logger = logger;
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromBody] SqlLoadConnectionRequest request)
    {
        try
        {
            var connectionString = BuildConnectionString(request);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            return Ok(new { success = true, message = "Connection successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpPost("list-tables")]
    public async Task<IActionResult> ListTables([FromBody] TableListRequest request)
    {
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    TABLE_SCHEMA as [Schema],
                    TABLE_NAME as TableName
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_SCHEMA, TABLE_NAME";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var tables = new List<TableSchemaInfo>();
            while (await reader.ReadAsync())
            {
                tables.Add(new TableSchemaInfo
                {
                    Schema = reader.GetString(0),
                    TableName = reader.GetString(1)
                });
            }

            return Ok(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list tables");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("get-table-schema")]
    public async Task<IActionResult> GetTableSchema([FromBody] TableSchemaRequest request)
    {
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    c.COLUMN_NAME as Name,
                    c.DATA_TYPE as DataType,
                    c.IS_NULLABLE as IsNullable,
                    c.CHARACTER_MAXIMUM_LENGTH as MaxLength,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                    AND c.TABLE_NAME = pk.TABLE_NAME 
                    AND c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_SCHEMA = @Schema AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Schema", request.Schema);
            command.Parameters.AddWithValue("@TableName", request.TableName);

            using var reader = await command.ExecuteReaderAsync();

            var columns = new List<ColumnInfo>();
            while (await reader.ReadAsync())
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetString(2) == "YES",
                    MaxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    IsPrimaryKey = reader.GetInt32(4) == 1
                });
            }

            if (columns.Count == 0)
            {
                return NotFound(new { error = "Table not found" });
            }

            return Ok(new TableSchemaInfo
            {
                Schema = request.Schema,
                TableName = request.TableName,
                Columns = columns
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get table schema");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("execute-load")]
    public async Task<IActionResult> ExecuteLoad([FromBody] SqlLoadRequest request)
    {
        try
        {
            if (request.Data == null || request.Data.Count == 0)
            {
                return BadRequest(new { error = "No data to load" });
            }

            if (request.ColumnMapping == null || request.ColumnMapping.Count == 0)
            {
                return BadRequest(new { error = "Column mapping is required" });
            }

            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            
            try
            {
                int totalRowsAffected = 0;

                // Truncate if requested
                if (request.Mode == LoadMode.TruncateInsert)
                {
                    var truncateCmd = new SqlCommand(
                        $"TRUNCATE TABLE [{request.TargetSchema}].[{request.TargetTable}]", 
                        connection, 
                        transaction);
                    await truncateCmd.ExecuteNonQueryAsync();
                    _logger.LogInformation("Table truncated: {Schema}.{Table}", request.TargetSchema, request.TargetTable);
                }

                // Insert data in batches
                const int batchSize = 1000;
                for (int i = 0; i < request.Data.Count; i += batchSize)
                {
                    var batch = request.Data.Skip(i).Take(batchSize).ToList();
                    var rowsAffected = await InsertBatch(batch, request.ColumnMapping, request.TargetSchema, request.TargetTable, connection, transaction);
                    totalRowsAffected += rowsAffected;
                    
                    _logger.LogInformation("Inserted batch {BatchNum}: {Rows} rows", i / batchSize + 1, rowsAffected);
                }

                if (request.TestMode)
                {
                    transaction.Rollback();
                    _logger.LogInformation("Test mode: Transaction rolled back");
                    
                    return Ok(new SqlLoadResult
                    {
                        Success = true,
                        RowsAffected = totalRowsAffected,
                        Message = $"Test successful! {totalRowsAffected} rows would be inserted (rolled back)",
                        WasTestMode = true
                    });
                }
                else
                {
                    transaction.Commit();
                    _logger.LogInformation("Transaction committed: {Rows} rows inserted", totalRowsAffected);
                    
                    return Ok(new SqlLoadResult
                    {
                        Success = true,
                        RowsAffected = totalRowsAffected,
                        Message = $"Load completed successfully! {totalRowsAffected} rows inserted",
                        WasTestMode = false
                    });
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Load failed, transaction rolled back");
                
                return Ok(new SqlLoadResult
                {
                    Success = false,
                    Message = "Load failed: " + ex.Message,
                    Errors = new List<string> { ex.ToString() },
                    WasTestMode = request.TestMode
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execute load failed");
            return StatusCode(500, new SqlLoadResult
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.ToString() }
            });
        }
    }

    private async Task<int> InsertBatch(
        List<Dictionary<string, object?>> rows,
        Dictionary<string, string> columnMapping,
        string schema,
        string table,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        if (rows.Count == 0) return 0;

        var targetColumns = columnMapping.Values.ToList();
        var sourceColumns = columnMapping.Keys.ToList();

        var columnList = string.Join(", ", targetColumns.Select(c => $"[{c}]"));
        var valuesList = new List<string>();
        var parameters = new List<SqlParameter>();

        int paramIndex = 0;
        foreach (var row in rows)
        {
            var rowParams = new List<string>();
            foreach (var sourceCol in sourceColumns)
            {
                var paramName = $"@p{paramIndex}";
                rowParams.Add(paramName);
                
                var value = row.ContainsKey(sourceCol) ? row[sourceCol] : null;
                parameters.Add(new SqlParameter(paramName, value ?? DBNull.Value));
                paramIndex++;
            }
            valuesList.Add($"({string.Join(", ", rowParams)})");
        }

        var sql = $"INSERT INTO [{schema}].[{table}] ({columnList}) VALUES {string.Join(", ", valuesList)}";
        
        using var command = new SqlCommand(sql, connection, transaction);
        command.Parameters.AddRange(parameters.ToArray());
        
        return await command.ExecuteNonQueryAsync();
    }

    private string BuildConnectionString(SqlLoadConnectionRequest request)
    {
        var parts = new List<string>
        {
            $"Server={request.Server}",
            $"Database={request.Database}",
            $"TrustServerCertificate={request.TrustServerCertificate}"
        };

        if (request.AuthType == "windows")
        {
            parts.Add("Integrated Security=True");
        }
        else
        {
            parts.Add($"User Id={request.Username}");
            parts.Add($"Password={request.Password}");
            parts.Add("Integrated Security=False");
        }

        return string.Join(";", parts);
    }
}
