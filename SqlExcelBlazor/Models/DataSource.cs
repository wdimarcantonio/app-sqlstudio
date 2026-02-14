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
    public GridState State { get; set; } = new();
    
    // NUOVO: Stati per import asincrono
    public ImportStatus ImportStatus { get; set; } = ImportStatus.Ready;
    public int ImportProgress { get; set; } = 0; // 0-100
    public string? ImportMessage { get; set; }
    public string? ImportError { get; set; }
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

/// <summary>
/// Stato di importazione di un'origine dati
/// </summary>
public enum ImportStatus
{
    Loading,    // Importazione in corso
    Ready,      // Disponibile per uso
    Error       // Errore durante importazione
}
