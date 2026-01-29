using System.Data;
using ExcelDataReader;

namespace SqlExcelBlazor.Server.Services;

public class ServerExcelService
{
    public ServerExcelService()
    {
        // Required for ExcelDataReader to handle older legacy encodings (CodePage 1252 etc)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public List<string> GetSheets(Stream stream, string fileName)
    {
        using var reader = CreateReader(stream, fileName);
        var sheets = new List<string>();
        
        // ExcelDataReader doesn't have a direct "GetSheetNames" without reading, 
        // but with AsDataSet we can get them, OR we can iterate the reader.
        // Iterating reader is more memory efficient for just names if we don't load everything.
        // However, AsDataSet is easier. For typical files < 50MB it's fine.
        // If we want to be persistent, we loop through results.
        
        // Actually, reader.Name gives the sheet name.
        do
        {
             sheets.Add(reader.Name);
        } while (reader.NextResult());
        
        return sheets;
    }
    
    public DataTable GetPreview(Stream stream, string fileName, string sheetName, int maxRows = 20)
    {
        using var reader = CreateReader(stream, fileName);
        
        // Move to correct sheet
        do
        {
            if (reader.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
            {
                // Found sheet, read it
                return ReadCurrentSheet(reader, maxRows);
            }
        } while (reader.NextResult());
        
        throw new Exception($"Sheet '{sheetName}' not found.");
    }
    
    public DataTable GetAllData(Stream stream, string fileName, string sheetName)
    {
        using var reader = CreateReader(stream, fileName);
        
        do
        {
            if (reader.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
            {
                 // Read all (pass -1 for maxRows)
                 return ReadCurrentSheet(reader, -1);
            }
        } while (reader.NextResult());
        
        throw new Exception($"Sheet '{sheetName}' not found.");
    }

    private IExcelDataReader CreateReader(Stream stream, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        if (extension == ".xls")
        {
            return ExcelReaderFactory.CreateBinaryReader(stream);
        }
        else
        {
            return ExcelReaderFactory.CreateOpenXmlReader(stream);
        }
    }
    
    private DataTable ReadCurrentSheet(IExcelDataReader reader, int maxRows)
    {
        // Use a clean config for reading
        var conf = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        };
        
        // We can't easily use AsDataSet for partial reading efficiently with the config applied 
        // because AsDataSet reads everything. 
        // So we manually read.
        
        var dt = new DataTable();
        dt.TableName = reader.Name;
        
        // First row is header
        if (!reader.Read()) return dt;
        
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var val = reader.GetValue(i)?.ToString();
            string colName = string.IsNullOrWhiteSpace(val) ? $"Column{i}" : val;
            
            // Dedupe
            int count = 1;
            string unique = colName;
            while(dt.Columns.Contains(unique))
            {
                unique = $"{colName}_{count++}";
            }
            dt.Columns.Add(unique);
        }
        
        int rowsRead = 0;
        while (reader.Read())
        {
            if (maxRows > 0 && rowsRead >= maxRows) break;
            
            var row = dt.NewRow();
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                row[i] = reader.GetValue(i);
            }
            dt.Rows.Add(row);
            rowsRead++;
        }
        
        return dt;
    }
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, (byte[] Data, string FileName)> _tempFiles = new();

    public Guid AddTempFile(byte[] data, string fileName)
    {
        var id = Guid.NewGuid();
        _tempFiles[id] = (data, fileName);
        return id;
    }

    public (byte[] Data, string FileName)? GetTempFile(Guid id)
    {
         if (_tempFiles.TryGetValue(id, out var val)) return val;
         return null;
    }

    public void RemoveTempFile(Guid id)
    {
        _tempFiles.TryRemove(id, out _);
    }
}
