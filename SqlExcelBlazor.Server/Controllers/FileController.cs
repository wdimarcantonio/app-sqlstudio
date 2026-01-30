using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;
using System.Data;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly ServerExcelService _excelService;
    private readonly ILogger<FileController> _logger;
    private readonly string _uploadsPath;

    public FileController(
        WorkspaceManager workspaceManager,
        ServerExcelService excelService,
        ILogger<FileController> logger,
        IConfiguration configuration)
    {
        _workspaceManager = workspaceManager;
        _excelService = excelService;
        _logger = logger;
        
        var baseDataPath = configuration["DataPath"] ?? 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SqlStudio", "data");
        _uploadsPath = Path.Combine(baseDataPath, "uploads");
        
        Directory.CreateDirectory(_uploadsPath);
    }

    /// <summary>
    /// Uploads an Excel file and loads it into a session
    /// </summary>
    [HttpPost("upload-excel")]
    public async Task<IActionResult> UploadExcel(
        IFormFile file,
        [FromForm] string sessionId,
        [FromForm] string? tableName = null,
        [FromForm] string? sheetName = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, error = "No file uploaded" });
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { success = false, error = "SessionId is required" });
        }

        try
        {
            var session = _workspaceManager.GetSession(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, error = "Session not found" });
            }

            // Save file temporarily
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_uploadsPath, $"{sessionId}_{Guid.NewGuid()}_{fileName}");
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Read Excel data
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            // Get sheets
            var sheets = _excelService.GetSheets(fileStream, fileName);
            if (sheets.Count == 0)
            {
                return BadRequest(new { success = false, error = "No sheets found in Excel file" });
            }

            // Use specified sheet or first sheet
            var targetSheet = sheetName ?? sheets[0];
            
            // Reset stream position
            fileStream.Position = 0;
            var dataTable = _excelService.GetAllData(fileStream, fileName, targetSheet);

            // Determine table name
            var finalTableName = tableName ?? 
                Path.GetFileNameWithoutExtension(fileName).Replace(" ", "_").Replace("-", "_");

            // Load into session
            await _workspaceManager.LoadTableAsync(sessionId, dataTable, finalTableName);

            // Clean up temp file
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch { /* Ignore cleanup errors */ }

            return Ok(new
            {
                success = true,
                tableName = finalTableName,
                rowCount = dataTable.Rows.Count,
                columnCount = dataTable.Columns.Count,
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading Excel file to session {SessionId}", sessionId);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Uploads a CSV file and loads it into a session
    /// </summary>
    [HttpPost("upload-csv")]
    public async Task<IActionResult> UploadCsv(
        IFormFile file,
        [FromForm] string sessionId,
        [FromForm] string? tableName = null,
        [FromForm] string separator = ";")
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, error = "No file uploaded" });
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { success = false, error = "SessionId is required" });
        }

        try
        {
            var session = _workspaceManager.GetSession(sessionId);
            if (session == null)
            {
                return NotFound(new { success = false, error = "Session not found" });
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            var dataTable = ParseCsv(content, separator);

            var finalTableName = tableName ?? 
                Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_").Replace("-", "_");

            await _workspaceManager.LoadTableAsync(sessionId, dataTable, finalTableName);

            return Ok(new
            {
                success = true,
                tableName = finalTableName,
                rowCount = dataTable.Rows.Count,
                columnCount = dataTable.Columns.Count,
                columns = dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading CSV file to session {SessionId}", sessionId);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Downloads query results as Excel file
    /// </summary>
    [HttpPost("download-excel")]
    public async Task<IActionResult> DownloadExcel([FromBody] DownloadExcelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(new { success = false, error = "SessionId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new { success = false, error = "SQL query is required" });
        }

        try
        {
            // Execute query to get results
            var result = await _workspaceManager.ExecuteQueryAsync(request.SessionId, request.Sql);
            
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = false, error = result.ErrorMessage });
            }

            // Convert to DataTable
            var dataTable = new DataTable();
            foreach (var col in result.Columns)
            {
                dataTable.Columns.Add(col);
            }

            foreach (var row in result.Rows)
            {
                var dataRow = dataTable.NewRow();
                foreach (var col in result.Columns)
                {
                    dataRow[col] = row.TryGetValue(col, out var value) ? value ?? DBNull.Value : DBNull.Value;
                }
                dataTable.Rows.Add(dataRow);
            }

            // Create Excel file
            var excelBytes = _excelService.CreateExcel(dataTable, request.SheetName ?? "Query Results");
            
            var fileName = request.FileName ?? $"query_results_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading Excel from session {SessionId}", request.SessionId);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private DataTable ParseCsv(string content, string separator)
    {
        var dataTable = new DataTable();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0) return dataTable;

        // Parse headers
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

        // Parse data rows
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

// Request DTOs
public class DownloadExcelRequest
{
    public string SessionId { get; set; } = "";
    public string Sql { get; set; } = "";
    public string? FileName { get; set; }
    public string? SheetName { get; set; }
}
