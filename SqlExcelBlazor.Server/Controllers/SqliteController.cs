using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;
using ClosedXML.Excel;
using System.Data;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqliteController : ControllerBase
{
    private readonly SqliteService _sqliteService;

    public SqliteController(SqliteService sqliteService)
    {
        _sqliteService = sqliteService;
    }

    /// <summary>
    /// Carica un file Excel in SQLite
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file, [FromForm] string? tableName = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nessun file caricato");

        try
        {
            // Determina nome tabella
            var name = tableName ?? Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_").Replace("-", "_");

            // Leggi Excel in DataTable
            using var stream = file.OpenReadStream();
            var dataTable = await ImportExcelAsync(stream);

            // Carica in SQLite
            await _sqliteService.LoadTableAsync(dataTable, name);

            return Ok(new
            {
                success = true,
                tableName = name,
                rowCount = dataTable.Rows.Count,
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Carica un file CSV in SQLite
    /// </summary>
    [HttpPost("upload-csv")]
    public async Task<IActionResult> UploadCsv(IFormFile file, [FromForm] string? tableName = null, [FromForm] string separator = ";")
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nessun file caricato");

        try
        {
            var name = tableName ?? Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_").Replace("-", "_");

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var dataTable = ImportCsv(content, separator);

            await _sqliteService.LoadTableAsync(dataTable, name);

            return Ok(new
            {
                success = true,
                tableName = name,
                rowCount = dataTable.Rows.Count,
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Esegue una query SQL
    /// </summary>
    [HttpPost("query")]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
            return BadRequest(new { success = false, error = "Query SQL vuota" });

        var result = await _sqliteService.ExecuteQueryAsync(request.Sql);
        return Ok(result);
    }

    /// <summary>
    /// Ritorna le tabelle caricate
    /// </summary>
    [HttpGet("tables")]
    public IActionResult GetTables()
    {
        return Ok(new { tables = _sqliteService.LoadedTables });
    }

    /// <summary>
    /// Rinomina una tabella
    /// </summary>
    [HttpPost("rename-table")]
    public async Task<IActionResult> RenameTable([FromBody] RenameTableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OldName) || string.IsNullOrWhiteSpace(request.NewName))
            return BadRequest(new { success = false, error = "Nome tabella mancante" });

        try
        {
            await _sqliteService.RenameTableAsync(request.OldName, request.NewName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una tabella
    /// </summary>
    [HttpPost("drop-table")]
    public async Task<IActionResult> DropTable([FromBody] DropTableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest(new { success = false, error = "Nome tabella mancante" });

        try
        {
            await _sqliteService.DropTableAsync(request.TableName);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private async Task<DataTable> ImportExcelAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var dataTable = new DataTable();

            var headerRow = worksheet.FirstRowUsed();
            if (headerRow == null) return dataTable;

            // Colonne
            foreach (var cell in headerRow.Cells())
            {
                string columnName = cell.GetString();
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = $"Column{cell.Address.ColumnNumber}";
                
                string uniqueName = columnName;
                int counter = 1;
                while (dataTable.Columns.Contains(uniqueName))
                    uniqueName = $"{columnName}_{counter++}";
                
                dataTable.Columns.Add(uniqueName, typeof(string));
            }

            // Righe
            var dataRows = worksheet.RowsUsed().Skip(1);
            foreach (var row in dataRows)
            {
                var dataRow = dataTable.NewRow();
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    var cell = row.Cell(i + 1);
                    dataRow[i] = cell.GetString();
                }
                dataTable.Rows.Add(dataRow);
            }

            return dataTable;
        });
    }

    private DataTable ImportCsv(string content, string separator)
    {
        var dataTable = new DataTable();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return dataTable;

        // Header
        var headers = lines[0].Split(separator);
        foreach (var header in headers)
        {
            var columnName = header.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(columnName))
                columnName = $"Column{dataTable.Columns.Count + 1}";
            
            string uniqueName = columnName;
            int counter = 1;
            while (dataTable.Columns.Contains(uniqueName))
                uniqueName = $"{columnName}_{counter++}";
            
            dataTable.Columns.Add(uniqueName, typeof(string));
        }

        // Data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(separator);
            var dataRow = dataTable.NewRow();
            for (int j = 0; j < Math.Min(values.Length, dataTable.Columns.Count); j++)
            {
                dataRow[j] = values[j].Trim().Trim('"');
            }
            dataTable.Rows.Add(dataRow);
        }

        return dataTable;
    }
}

public class QueryRequest
{
    public string Sql { get; set; } = "";
}

public class RenameTableRequest
{
    public string OldName { get; set; } = "";
    public string NewName { get; set; } = "";
}
public class DropTableRequest { public string TableName { get; set; } = ""; }
