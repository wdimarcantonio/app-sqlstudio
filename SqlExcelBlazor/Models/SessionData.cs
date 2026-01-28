namespace SqlExcelBlazor.Models;

/// <summary>
/// Modello per salvare/caricare l'intera sessione
/// </summary>
public class SessionData
{
    public List<TableDefinition> Tables { get; set; } = new();
    public List<TableNode> VisualNodes { get; set; } = new();
    public List<ConnectionLink> VisualLinks { get; set; } = new();
    public List<DesignGridColumn> GridColumns { get; set; } = new();
    public string CurrentQuery { get; set; } = "";
}

public class TableDefinition
{
    public string TableName { get; set; } = "";
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}
