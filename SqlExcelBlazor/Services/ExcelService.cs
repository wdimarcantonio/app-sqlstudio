using ClosedXML.Excel;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Servizio per parsing file Excel in Blazor WebAssembly
/// </summary>
public class ExcelService
{
    // Limiti di sicurezza
    public const int MAX_ROWS_HARD_LIMIT = 50000;
    public const int MAX_ROWS_WARNING = 10000;
    public const int MAX_FILE_SIZE_MB = 50;
    
    /// <summary>
    /// Parsa un file Excel da uno stream
    /// </summary>
    public async Task<ImportResult> ParseExcelWithValidationAsync(Stream stream, string fileName)
    {
        return await Task.Run(() =>
        {
            var result = new ImportResult();
            
            // 1. Verifica dimensione file
            if (stream.Length > MAX_FILE_SIZE_MB * 1024 * 1024)
            {
                result.ErrorMessage = $"Il file supera il limite di {MAX_FILE_SIZE_MB}MB. " +
                                      $"Dimensione: {stream.Length / 1024 / 1024}MB";
                return result;
            }
            
            var dataSource = new DataSource
            {
                Name = fileName,
                Type = DataSourceType.Excel,
                TableAlias = GenerateAlias(fileName)
            };
            
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            
            // Prima riga come header
            var headerRow = worksheet.FirstRowUsed();
            if (headerRow == null) 
            {
                result.ErrorMessage = "Il file non contiene righe di intestazione.";
                return result;
            }
            
            var columns = new List<string>();
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in headerRow.Cells())
            {
                string columnName = cell.GetString();
                if (string.IsNullOrWhiteSpace(columnName))
                    columnName = $"Column{cell.Address.ColumnNumber}";
                
                // Assicura nomi colonna unici
                string uniqueName = columnName;
                int counter = 1;
                while (existingColumns.Contains(uniqueName))
                {
                    uniqueName = $"{columnName}_{counter++}";
                }
                
                columns.Add(uniqueName);
                existingColumns.Add(uniqueName);
            }
            
            dataSource.Columns = columns;
            
            // Righe dati
            var dataRows = worksheet.RowsUsed().Skip(1);
            var totalRows = dataRows.Count();
            
            // 2. Verifica numero righe
            if (totalRows > MAX_ROWS_HARD_LIMIT)
            {
                result.ErrorMessage = $"Il file contiene {totalRows:N0} righe. " +
                                      $"Limite massimo: {MAX_ROWS_HARD_LIMIT:N0}. " +
                                      "Filtra i dati in Excel prima dell'import.";
                return result;
            }
            
            // 3. Warning per dataset grandi
            if (totalRows > MAX_ROWS_WARNING)
            {
                result.WarningMessage = $"⚠️ Il file contiene {totalRows:N0} righe. " +
                                        "L'elaborazione potrebbe richiedere tempo.";
            }
            
            foreach (var row in dataRows)
            {
                var rowData = new Dictionary<string, string>();
                for (int i = 0; i < columns.Count; i++)
                {
                    var cell = row.Cell(i + 1);
                    rowData[columns[i]] = cell.GetString();
                }
                dataSource.Data.Add(rowData);
            }
            
            dataSource.RowCount = dataSource.Data.Count;
            dataSource.IsLoaded = true;
            
            result.Success = true;
            result.DataSource = dataSource;
            return result;
        });
    }
    
    /// <summary>
    /// Parsa un file Excel da uno stream (metodo legacy per compatibilità)
    /// </summary>
    public async Task<DataSource> ParseExcelAsync(Stream stream, string fileName)
    {
        var result = await ParseExcelWithValidationAsync(stream, fileName);
        if (result.Success && result.DataSource != null)
        {
            return result.DataSource;
        }
        
        // In caso di errore, ritorna un DataSource vuoto
        return new DataSource
        {
            Name = fileName,
            Type = DataSourceType.Excel,
            TableAlias = GenerateAlias(fileName)
        };
    }
    
    /// <summary>
    /// Genera i bytes per un file Excel dai dati (con chunking e gestione memoria)
    /// </summary>
    public byte[] GenerateExcel(QueryResult result)
    {
        return GenerateExcelAsync(result, null).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Genera i bytes per un file Excel dai dati con progress reporting
    /// </summary>
    public async Task<byte[]> GenerateExcelAsync(
        QueryResult result, 
        IProgress<ExportProgress>? progress = null)
    {
        const int CHUNK_SIZE = 1000;
        const int GC_INTERVAL = 10000;
        
        using var stream = new MemoryStream();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Risultati");
        
        try
        {
            // 1. Headers
            for (int i = 0; i < result.Columns.Count; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = result.Columns[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
                cell.Style.Font.FontColor = XLColor.White;
            }
            
            progress?.Report(new ExportProgress { Current = 0, Total = result.Rows.Count, Phase = "Scrittura dati" });
            
            // 2. Rows in chunks
            int rowIdx = 2;
            for (int chunk = 0; chunk < result.Rows.Count; chunk += CHUNK_SIZE)
            {
                var rows = result.Rows.Skip(chunk).Take(CHUNK_SIZE);
                
                foreach (var row in rows)
                {
                    for (int colIdx = 0; colIdx < result.Columns.Count; colIdx++)
                    {
                        var value = row.GetValueOrDefault(result.Columns[colIdx]);
                        
                        if (value != null)
                        {
                            var cell = worksheet.Cell(rowIdx, colIdx + 1);
                            
                            // Type-aware export
                            if (value is DateTime dt)
                                cell.Value = dt;
                            else if (double.TryParse(value.ToString(), out var num))
                                cell.Value = num;
                            else
                                cell.Value = value.ToString();
                        }
                    }
                    rowIdx++;
                }
                
                // Progress report
                progress?.Report(new ExportProgress 
                { 
                    Current = Math.Min(chunk + CHUNK_SIZE, result.Rows.Count),
                    Total = result.Rows.Count,
                    Phase = "Scrittura dati"
                });
                
                // Force GC ogni 10k righe per liberare memoria
                if (chunk % GC_INTERVAL == 0 && chunk > 0)
                {
                    GC.Collect();
                    await Task.Delay(10); // Yield
                }
            }
            
            progress?.Report(new ExportProgress { Current = result.Rows.Count, Total = result.Rows.Count, Phase = "Salvataggio file" });
            
            // 3. Auto-fit columns (opzionale, costoso per molte colonne)
            if (result.Columns.Count < 50)
            {
                worksheet.Columns().AdjustToContents();
            }
            
            workbook.SaveAs(stream);
            progress?.Report(new ExportProgress { Current = result.Rows.Count, Total = result.Rows.Count, Phase = "Completato" });
            
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Errore durante l'export Excel: {ex.Message}", ex);
        }
    }
    
    private string GenerateAlias(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var alias = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(alias)) alias = "Table";
        if (char.IsDigit(alias[0])) alias = "T" + alias;
        return alias;
    }
}

/// <summary>
/// Risultato dell'importazione con validazione
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningMessage { get; set; }
    public DataSource? DataSource { get; set; }
}

/// <summary>
/// Progress di export Excel
/// </summary>
public class ExportProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string Phase { get; set; } = "";
    public int Percentage => Total > 0 ? (Current * 100 / Total) : 0;
}
