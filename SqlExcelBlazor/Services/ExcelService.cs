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
    /// Genera i bytes per un file Excel dai dati
    /// </summary>
    public byte[] GenerateExcel(QueryResult result)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Risultati");
        
        // Header
        for (int i = 0; i < result.Columns.Count; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = result.Columns[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2196F3");
            cell.Style.Font.FontColor = XLColor.White;
        }
        
        // Dati
        for (int row = 0; row < result.Rows.Count; row++)
        {
            var rowData = result.Rows[row];
            for (int col = 0; col < result.Columns.Count; col++)
            {
                var colName = result.Columns[col];
                worksheet.Cell(row + 2, col + 1).Value = rowData.GetValueOrDefault(colName)?.ToString() ?? "";
            }
        }
        
        worksheet.Columns().AdjustToContents();
        
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
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
