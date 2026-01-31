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
    private readonly ServerExcelService _excelService;

    public SqliteController(SqliteService sqliteService, ServerExcelService excelService)
    {
        _sqliteService = sqliteService;
        _excelService = excelService;
    }

    /// <summary>
    /// Uploads file to temp storage and returns ID for further processing
    /// </summary>
    [HttpPost("excel/upload-temp")]
    public async Task<IActionResult> UploadTempExcel(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        var data = stream.ToArray();
        
        var id = _excelService.AddTempFile(data, file.FileName);
        return Ok(new { fileId = id, fileName = file.FileName });
    }

    [HttpGet("excel/sheets/{fileId}")]
    public IActionResult GetExcelSheets(Guid fileId)
    {
        var temp = _excelService.GetTempFile(fileId);
        if (temp == null) return NotFound("File not found or expired");
        
        try 
        {
            using var stream = new MemoryStream(temp.Value.Data);
            var sheets = _excelService.GetSheets(stream, temp.Value.FileName);
            return Ok(sheets);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("excel/preview")]
    public IActionResult PreviewExcelData([FromBody] ExcelPreviewRequest request)
    {
        var temp = _excelService.GetTempFile(request.FileId);
        if (temp == null) return NotFound("File not found or expired");

        try
        {
            using var stream = new MemoryStream(temp.Value.Data);
            var dt = _excelService.GetPreview(stream, temp.Value.FileName, request.SheetName, 20);
            
             return Ok(new
            {
                columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList(),
                rows = dt.Rows.Cast<DataRow>().Select(r => r.ItemArray).ToList()
            });
        }
         catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("excel/import-sheet")]
    public async Task<IActionResult> ImportExcelSheet([FromBody] ImportSheetRequest request)
    {
        var temp = _excelService.GetTempFile(request.FileId);
        if (temp == null) return NotFound("File not found or expired");

        try
        {
            using var stream = new MemoryStream(temp.Value.Data);
            var dt = _excelService.GetAllData(stream, temp.Value.FileName, request.SheetName);
            
            await _sqliteService.LoadTableAsync(dt, request.TableName);
            
            return Ok(new
            {
                success = true,
                tableName = request.TableName,
                rowCount = dt.Rows.Count,
                columns = dt.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            });
        }
         catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Legacy/Direct upload (keeps backward compatibility but uses new service)
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadExcel(IFormFile file, [FromForm] string? tableName = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nessun file caricato");

        try
        {
            var name = tableName ?? Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_").Replace("-", "_");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            // Simple default: get first sheet
            var sheets = _excelService.GetSheets(stream, file.FileName);
            if (sheets.Count == 0) throw new Exception("No sheets found");
            
            stream.Position = 0; 
            var dataTable = _excelService.GetAllData(stream, file.FileName, sheets[0]);

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
    public async Task<IActionResult> UploadCsv(
        IFormFile file, 
        [FromForm] string? tableName = null, 
        [FromForm] string separator = ";",
        [FromForm] string dateFormat = "dd/MM/yyyy",
        [FromForm] string decimalSeparator = ",")
    {
        if (file == null || file.Length == 0)
            return BadRequest("Nessun file caricato");

        try
        {
            var name = tableName ?? Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_").Replace("-", "_");

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var dataTable = ImportCsv(content, separator, dateFormat, decimalSeparator);

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
    /// Carica dati JSON in SQLite
    /// </summary>
    [HttpPost("upload-json")]
    public async Task<IActionResult> UploadJson([FromBody] UploadJsonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest(new { success = false, error = "Nome tabella mancante" });

        try
        {
            var dataTable = new DataTable();
            
            // Colonne
            foreach (var col in request.Columns)
            {
                dataTable.Columns.Add(col, typeof(string)); 
            }

            // Righe
            foreach (var rowDict in request.Rows)
            {
                var row = dataTable.NewRow();
                foreach (var col in request.Columns)
                {
                    if (rowDict.TryGetValue(col, out var val) && val != null)
                    {
                        row[col] = val.ToString();
                    }
                    else
                    {
                        row[col] = DBNull.Value;
                    }
                }
                dataTable.Rows.Add(row);
            }

            await _sqliteService.LoadTableAsync(dataTable, request.TableName);

            return Ok(new
            {
                success = true,
                tableName = request.TableName,
                rowCount = dataTable.Rows.Count,
                columns = request.Columns
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPost("query")]
    public async Task<IActionResult> ExecuteQuery([FromBody] QueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sql))
            return BadRequest(new { success = false, error = "Query SQL vuota" });

        var result = await _sqliteService.ExecuteQueryAsync(request.Sql);
        return Ok(result);
    }

    [HttpGet("tables")]
    public IActionResult GetTables()
    {
        return Ok(new { tables = _sqliteService.LoadedTables });
    }

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

    private DataTable ImportCsv(string content, string separator, string dateFormat, string decimalSeparator)
    {
        var dataTable = new DataTable();
        
        // Store regional settings as extended properties for later use in type detection
        dataTable.ExtendedProperties["DateFormat"] = dateFormat;
        dataTable.ExtendedProperties["DecimalSeparator"] = decimalSeparator;
        
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return dataTable;

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

public class UploadJsonRequest
{
    public string TableName { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

public class ExcelPreviewRequest
{
    public Guid FileId { get; set; }
    public string SheetName { get; set; } = "";
}

public class ImportSheetRequest
{
    public Guid FileId { get; set; }
    public string SheetName { get; set; } = "";
    public string TableName { get; set; } = "";
}
