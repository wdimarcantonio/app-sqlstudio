namespace SqlExcelBlazor.Models;

/// <summary>
/// Contiene il risultato di una query SQL
/// </summary>
public class QueryResult
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount => Rows.Count;
    public int ColumnCount => Columns.Count;
    public TimeSpan ExecutionTime { get; set; }
    public double ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string ExecutionLocation { get; set; } = ""; // "WASM" or "Server"

    // Helper se si vuole controllare validità
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
}
