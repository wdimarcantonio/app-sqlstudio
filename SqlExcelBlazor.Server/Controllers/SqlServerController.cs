using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace SqlExcelBlazor.Server.Controllers;

public class SqlServerConnectionRequest
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SchemaItem
{
    public string Schema { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "TABLE" or "VIEW"
    public string FullName => $"[{Schema}].[{Name}]";
}

[ApiController]
[Route("api/[controller]")]
public class SqlServerController : ControllerBase
{
    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] SqlServerConnectionRequest request)
    {
        try
        {
            using var conn = new SqlConnection(request.ConnectionString);
            await conn.OpenAsync();
            return Ok(new { Message = "Connessione riuscita" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("tables")]
    public async Task<IActionResult> GetTables([FromBody] SqlServerConnectionRequest request)
    {
        try
        {
            using var conn = new SqlConnection(request.ConnectionString);
            await conn.OpenAsync();

            var items = new List<SchemaItem>();

            // Get Tables
            string sqlTables = @"
                SELECT TABLE_SCHEMA, TABLE_NAME, 'TABLE' as TYPE 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'";
            
            using (var cmd = new SqlCommand(sqlTables, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new SchemaItem
                    {
                        Schema = reader.GetString(0),
                        Name = reader.GetString(1),
                        Type = "TABLE"
                    });
                }
            }

            // Get Views
            string sqlViews = @"
                SELECT TABLE_SCHEMA, TABLE_NAME, 'VIEW' as TYPE 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'VIEW'";
            
            using (var cmd = new SqlCommand(sqlViews, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    items.Add(new SchemaItem
                    {
                        Schema = reader.GetString(0),
                        Name = reader.GetString(1),
                        Type = "VIEW"
                    });
                }
            }

            return Ok(items);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("query")]
    public async Task<IActionResult> ExecuteQuery([FromBody] SqlQueryRequest request)
    {
        try
        {
            using var conn = new SqlConnection(request.ConnectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(request.Query, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            var result = new List<Dictionary<string, object?>>();
            var columns = new List<string>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                result.Add(row);
            }

            return Ok(new { Columns = columns, Rows = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}

public class SqlQueryRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
}
