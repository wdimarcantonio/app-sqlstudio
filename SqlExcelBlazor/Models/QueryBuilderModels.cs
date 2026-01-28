using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Models;

public class TableNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DataSource DataSource { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 200;
    public double Height { get; set; } = 170; // Initial height for ~5 rows (30px header + 5*24px rows + 20px padding)
    
    // Helper to get minimum height based on content
    public double MinHeight => 50 + Math.Min(DataSource?.Columns.Count ?? 0, 2) * 24;
    public double MaxHeight => 50 + (DataSource?.Columns.Count ?? 0) * 24;
}

public class ConnectionLink
{
    public string SourceTableId { get; set; }
    public string SourceColumn { get; set; }
    public string TargetTableId { get; set; }
    public string TargetColumn { get; set; }
    public JoinType Type { get; set; } = JoinType.Inner;
}

public enum JoinType { Inner, Left, Right }

public class DesignGridColumn
{
    public string OriginalName { get; set; } = "";
    public string TableAlias { get; set; } = "";
    public string Alias { get; set; } = "";
    public bool IsSelected { get; set; } = true;
    public SortOrder SortOrder { get; set; } = SortOrder.None;
    
    public string TransformationType { get; set; } = "None";
    public string TransformationParam { get; set; } = "";
}

public enum SortOrder { None, Ascending, Descending }
