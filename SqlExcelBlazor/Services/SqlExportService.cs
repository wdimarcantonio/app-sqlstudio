using System.Text;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Servizio per generare script SQL di inserimento
/// </summary>
public class SqlExportService
{
    public string GenerateInsertScript(QueryResult result, string tableName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"-- Script generato per tabella [{tableName}]");
        sb.AppendLine($"-- Data: {DateTime.Now}");
        sb.AppendLine();
        
        // Crea tabella se non esiste
        sb.AppendLine($"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '{tableName}')");
        sb.AppendLine("BEGIN");
        sb.AppendLine($"    CREATE TABLE [{tableName}] (");
        sb.AppendLine("        [Id] INT IDENTITY(1,1) PRIMARY KEY,");
        
        for (int i = 0; i < result.Columns.Count; i++)
        {
            var col = result.Columns[i];
            var safeCol = col.Replace("]", "]]");
            var comma = i < result.Columns.Count - 1 ? "," : "";
            sb.AppendLine($"        [{safeCol}] NVARCHAR(MAX){comma}");
        }
        
        sb.AppendLine("    );");
        sb.AppendLine("END");
        sb.AppendLine("GO");
        sb.AppendLine();
        
        // Inserimenti
        sb.AppendLine($"SET IDENTITY_INSERT [{tableName}] OFF;");
        sb.AppendLine();
        
        var columns = string.Join(", ", result.Columns.Select(c => $"[{c}]"));
        
        foreach (var row in result.Rows)
        {
            var values = new List<string>();
            foreach (var col in result.Columns)
            {
                var val = row.GetValueOrDefault(col);
                if (val == null)
                    values.Add("NULL");
                else
                    values.Add($"'{val.ToString()?.Replace("'", "''")}'");
            }
            
            sb.AppendLine($"INSERT INTO [{tableName}] ({columns}) VALUES ({string.Join(", ", values)});");
        }
        
        return sb.ToString();
    }
}
