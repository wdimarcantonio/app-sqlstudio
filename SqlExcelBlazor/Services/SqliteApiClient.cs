using System.Net.Http.Json;
using System.Text.Json;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Client per comunicare con l'API SQLite del backend
/// </summary>
public class SqliteApiClient
{
    private readonly HttpClient _httpClient;

    public SqliteApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Carica un file Excel in SQLite nel backend
    /// </summary>
    public async Task<UploadResult> UploadExcelAsync(Stream fileStream, string fileName, string? tableName = null)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            
            content.Add(streamContent, "file", fileName);
            if (!string.IsNullOrEmpty(tableName))
            {
                content.Add(new StringContent(tableName), "tableName");
            }

            var response = await _httpClient.PostAsync("api/sqlite/upload", content);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<UploadSuccessResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult
                {
                    Success = true,
                    TableName = result?.TableName ?? "",
                    RowCount = result?.RowCount ?? 0,
                    Columns = result?.Columns ?? new List<string>()
                };
            }
            else
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult { Success = false, ErrorMessage = error?.Error ?? "Errore sconosciuto" };
            }
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Carica un file CSV in SQLite nel backend
    /// </summary>
    public async Task<UploadResult> UploadCsvAsync(Stream fileStream, string fileName, string? tableName = null, string separator = ";", string dateFormat = "dd/MM/yyyy", string decimalSeparator = ",")
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var streamContent = new StreamContent(fileStream);
            
            content.Add(streamContent, "file", fileName);
            if (!string.IsNullOrEmpty(tableName))
            {
                content.Add(new StringContent(tableName), "tableName");
            }
            content.Add(new StringContent(separator), "separator");
            content.Add(new StringContent(dateFormat), "dateFormat");
            content.Add(new StringContent(decimalSeparator), "decimalSeparator");

            var response = await _httpClient.PostAsync("api/sqlite/upload-csv", content);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<UploadSuccessResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult
                {
                    Success = true,
                    TableName = result?.TableName ?? "",
                    RowCount = result?.RowCount ?? 0,
                    Columns = result?.Columns ?? new List<string>()
                };
            }
            else
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult { Success = false, ErrorMessage = error?.Error ?? "Errore sconosciuto" };
            }
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Carica dati JSON in SQLite
    /// </summary>
    public async Task<UploadResult> UploadJsonAsync(string tableName, List<string> columns, List<Dictionary<string, object?>> rows)
    {
        try
        {
            var request = new { TableName = tableName, Columns = columns, Rows = rows };
            var response = await _httpClient.PostAsJsonAsync("api/sqlite/upload-json", request);
            var json = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<UploadSuccessResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult
                {
                    Success = true,
                    TableName = result?.TableName ?? "",
                    RowCount = result?.RowCount ?? 0,
                    Columns = result?.Columns ?? new List<string>()
                };
            }
            else
            {
                var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new UploadResult { Success = false, ErrorMessage = error?.Error ?? "Errore sconosciuto" };
            }
        }
        catch (Exception ex)
        {
            return new UploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Esegue una query SQL sul backend
    /// </summary>
    public async Task<SqliteQueryResult> ExecuteQueryAsync(string sql)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/sqlite/query", new { sql });
            var result = await response.Content.ReadFromJsonAsync<SqliteQueryResult>();
            return result ?? new SqliteQueryResult { IsSuccess = false, ErrorMessage = "Risposta vuota" };
        }
        catch (Exception ex)
        {
            return new SqliteQueryResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Ottiene la lista delle tabelle caricate
    /// </summary>
    public async Task<TablesResponse> GetTablesAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TablesResponse>("api/sqlite/tables");
            return response ?? new TablesResponse();
        }
        catch
        {
            return new TablesResponse();
        }
    }

    /// <summary>
    /// Analyzes a table and returns detailed statistics
    /// </summary>
    public async Task<AnalysisResponse> AnalyzeTableAsync(string tableName, int topValueCount = 20)
    {
        try
        {
            var request = new { TableName = tableName, TopValueCount = topValueCount, EnablePatternDetection = true, EnableParallelProcessing = true };
            var response = await _httpClient.PostAsJsonAsync("api/dataanalysis/table", request);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AnalysisResponse>();
                return result ?? new AnalysisResponse { Success = false, Error = "Empty response" };
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                return new AnalysisResponse { Success = false, Error = error };
            }
        }
        catch (Exception ex)
        {
            return new AnalysisResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Gets an analysis by ID
    /// </summary>
    public async Task<AnalysisResponse> GetAnalysisAsync(int analysisId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AnalysisResponse>($"api/dataanalysis/{analysisId}");
            return response ?? new AnalysisResponse { Success = false, Error = "Not found" };
        }
        catch (Exception ex)
        {
            return new AnalysisResponse { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Rinomina una tabella nel backend
    /// </summary>
    public async Task<bool> RenameTableAsync(string oldName, string newName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/sqlite/rename-table", new { oldName, newName });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Elimina una tabella dal backend
    /// </summary>
    public async Task<bool> DropTableAsync(string tableName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/sqlite/drop-table", new { tableName });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    public async Task<(Guid FileId, string FileName)> UploadTempExcelAsync(Stream fileStream, string fileName)
    {
         using var content = new MultipartFormDataContent();
         using var streamContent = new StreamContent(fileStream);
         content.Add(streamContent, "file", fileName);

         var response = await _httpClient.PostAsync("api/sqlite/excel/upload-temp", content);
         if (response.IsSuccessStatusCode)
         {
             var json = await response.Content.ReadFromJsonAsync<JsonElement>();
             if (json.TryGetProperty("fileId", out var idProp))
             {
                 return (Guid.Parse(idProp.GetString()!), fileName);
             }
         }
         throw new Exception("Upload failed");
    }

    public async Task<List<string>> GetExcelSheetsAsync(Guid fileId)
    {
        return await _httpClient.GetFromJsonAsync<List<string>>($"api/sqlite/excel/sheets/{fileId}") ?? new List<string>();
    }

    public async Task<ExcelPreviewResult> PreviewExcelDataAsync(Guid fileId, string sheetName)
    {
         var response = await _httpClient.PostAsJsonAsync("api/sqlite/excel/preview", new { FileId = fileId, SheetName = sheetName });
         if (response.IsSuccessStatusCode)
         {
             return await response.Content.ReadFromJsonAsync<ExcelPreviewResult>() ?? new ExcelPreviewResult();
         }
         throw new Exception("Preview failed");
    }

    public async Task<UploadResult> ImportExcelSheetAsync(Guid fileId, string sheetName, string tableName)
    {
        var response = await _httpClient.PostAsJsonAsync("api/sqlite/excel/import-sheet", new { FileId = fileId, SheetName = sheetName, TableName = tableName });
         var json = await response.Content.ReadAsStringAsync();

         if (response.IsSuccessStatusCode)
         {
             var result = JsonSerializer.Deserialize<UploadSuccessResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
             return new UploadResult
             {
                 Success = true,
                 TableName = result?.TableName ?? "",
                 RowCount = result?.RowCount ?? 0,
                 Columns = result?.Columns ?? new List<string>()
             };
         }
         else
         {
             var error = JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
             return new UploadResult { Success = false, ErrorMessage = error?.Error ?? "Errore sconosciuto" };
         }
    }
}

// DTOs
public class ExcelPreviewResult
{
    public List<string> Columns { get; set; } = new();
    public List<object[]> Rows { get; set; } = new(); // Changed to object[] to handle mixed types if JSON serializer supports it, or back to string? Server sends object[].
}

public class UploadResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string TableName { get; set; } = "";
    public int RowCount { get; set; }
    public List<string> Columns { get; set; } = new();
}

public class UploadSuccessResponse
{
    public bool Success { get; set; }
    public string TableName { get; set; } = "";
    public int RowCount { get; set; }
    public List<string> Columns { get; set; } = new();
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class SqliteQueryResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public double ExecutionTimeMs { get; set; }
}

public class TablesResponse
{
    public List<string> Tables { get; set; } = new();
}

public class AnalysisResponse
{
    public bool Success { get; set; }
    public object? Analysis { get; set; }
    public string? Error { get; set; }
}
