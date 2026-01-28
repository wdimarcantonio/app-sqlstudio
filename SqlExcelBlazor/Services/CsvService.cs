using System.Text;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Servizio per parsing file CSV in Blazor WebAssembly
/// </summary>
public class CsvService
{
    /// <summary>
    /// Parsa un file CSV da uno stream
    /// </summary>
    public async Task<DataSource> ParseCsvAsync(Stream stream, string fileName, char delimiter = ',')
    {
        return await Task.Run(() =>
        {
            var dataSource = new DataSource
            {
                Name = fileName,
                Type = DataSourceType.Csv,
                TableAlias = GenerateAlias(fileName)
            };
            
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var content = reader.ReadToEnd();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            if (lines.Length == 0) return dataSource;
            
            // Prima riga come header
            var headers = ParseCsvLine(lines[0].TrimEnd('\r'), delimiter);
            var columns = new List<string>();
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var header in headers)
            {
                string columnName = string.IsNullOrWhiteSpace(header) 
                    ? $"Column{columns.Count + 1}" 
                    : header;
                
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
            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i].TrimEnd('\r'), delimiter);
                var rowData = new Dictionary<string, string>();
                
                for (int j = 0; j < Math.Min(values.Length, columns.Count); j++)
                {
                    rowData[columns[j]] = values[j];
                }
                
                // Fill missing columns with empty string
                foreach (var col in columns.Where(c => !rowData.ContainsKey(c)))
                {
                    rowData[col] = "";
                }
                
                dataSource.Data.Add(rowData);
            }
            
            dataSource.RowCount = dataSource.Data.Count;
            dataSource.IsLoaded = true;
            
            return dataSource;
        });
    }
    
    /// <summary>
    /// Genera CSV come string dai risultati
    /// </summary>
    public string GenerateCsv(QueryResult result, char delimiter = ',')
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine(string.Join(delimiter, result.Columns.Select(c => EscapeCsvField(c, delimiter))));
        
        // Dati
        foreach (var row in result.Rows)
        {
            var values = result.Columns.Select(col => 
                EscapeCsvField(row.GetValueOrDefault(col)?.ToString() ?? "", delimiter));
            sb.AppendLine(string.Join(delimiter, values));
        }
        
        return sb.ToString();
    }
    
    private string[] ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == delimiter && !inQuotes)
            {
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        result.Add(currentField.ToString());
        return result.ToArray();
    }
    
    private string EscapeCsvField(string field, char delimiter)
    {
        if (field.Contains(delimiter) || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
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
