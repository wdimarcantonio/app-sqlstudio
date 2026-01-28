using ClosedXML.Excel;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Servizio per parsing file Excel in Blazor WebAssembly
/// </summary>
public class ExcelService
{
    /// <summary>
    /// Parsa un file Excel da uno stream
    /// </summary>
    /// <summary>
    /// Parsa un file Excel da uno stream
    /// </summary>
    public async Task<DataSource> ParseExcelAsync(Stream stream, string fileName)
    {
        return await Task.Run(() =>
        {
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
            if (headerRow == null) return dataSource;
            
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
            
            return dataSource;
        });
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
