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
            cmd.CommandTimeout = 300; // 5 minuti timeout per query grandi
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

    /// <summary>
    /// OTTIMIZZAZIONE: Importa tabella SQL Server con paginazione per migliori performance
    /// Usa ROW_NUMBER() per paginazione efficiente anche senza chiave primaria
    /// </summary>
    [HttpPost("import-table")]
    public async Task<IActionResult> ImportTable([FromBody] ImportTableRequest request)
    {
        try
        {
            var sqliteService = HttpContext.RequestServices.GetRequiredService<SqlExcelBlazor.Server.Services.SqliteService>();
            
            using var conn = new SqlConnection(request.ConnectionString);
            await conn.OpenAsync();

            // Conta totale righe per progress report
            string countQuery = $"SELECT COUNT(*) FROM [{request.Schema}].[{request.TableName}]";
            int totalRows;
            using (var countCmd = new SqlCommand(countQuery, conn))
            {
                totalRows = (int)await countCmd.ExecuteScalarAsync();
            }

            // Se tabella piccola (< 10k righe), usa metodo classico
            if (totalRows < 10000)
            {
                string simpleQuery = $"SELECT * FROM [{request.Schema}].[{request.TableName}]";
                var dataTable = new DataTable();
                using var adapter = new SqlDataAdapter(simpleQuery, conn);
                adapter.Fill(dataTable);
                
                await sqliteService.LoadTableAsync(dataTable, request.TargetTableName);
                
                return Ok(new { 
                    Success = true, 
                    TotalRows = totalRows,
                    Message = $"Importate {totalRows} righe"
                });
            }

            // Per tabelle grandi, usa paginazione
            const int pageSize = 10000;
            int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);
            
            // Crea struttura tabella leggendo prima pagina
            string firstPageQuery = $@"
                SELECT TOP {pageSize} *
                FROM [{request.Schema}].[{request.TableName}]";
            
            var firstPageData = new DataTable();
            using (var adapter = new SqlDataAdapter(firstPageQuery, conn))
            {
                adapter.Fill(firstPageData);
            }
            
            // Crea tabella SQLite con struttura
            await sqliteService.LoadTableAsync(firstPageData, request.TargetTableName);
            
            int importedRows = firstPageData.Rows.Count;

            // Importa pagine successive
            for (int page = 1; page < totalPages; page++)
            {
                int offset = page * pageSize;
                
                // Query con OFFSET/FETCH per paginazione efficiente
                string pageQuery = $@"
                    SELECT *
                    FROM [{request.Schema}].[{request.TableName}]
                    ORDER BY (SELECT NULL)
                    OFFSET {offset} ROWS
                    FETCH NEXT {pageSize} ROWS ONLY";
                
                var pageData = new DataTable();
                using (var adapter = new SqlDataAdapter(pageQuery, conn))
                {
                    adapter.Fill(pageData);
                }
                
                // Append alla tabella esistente
                if (pageData.Rows.Count > 0)
                {
                    await sqliteService.AppendDataToTableAsync(pageData, request.TargetTableName);
                    importedRows += pageData.Rows.Count;
                }
            }

            return Ok(new { 
                Success = true, 
                TotalRows = importedRows,
                Message = $"Importate {importedRows} righe in {totalPages} batch"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Message = ex.Message });
        }
    }
}

public class ImportTableRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string TargetTableName { get; set; } = string.Empty;
}

public class SqlQueryRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
}
