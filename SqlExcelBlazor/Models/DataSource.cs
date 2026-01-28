namespace SqlExcelBlazor.Models;

/// <summary>
/// Rappresenta un'origine dati (Excel, CSV)
/// </summary>
public class DataSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DataSourceType Type { get; set; }
    public string TableAlias { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public int RowCount { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, string>> Data { get; set; } = new();
}

/// <summary>
/// Tipo di origine dati
/// </summary>
public enum DataSourceType
{
    Excel,
    Csv,
    SqlServer
}
