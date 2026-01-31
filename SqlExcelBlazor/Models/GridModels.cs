namespace SqlExcelBlazor.Models;

public class GridState
{
    public string? SortColumn { get; set; }
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
    public List<FilterDef> Filters { get; set; } = new();
}

public enum SortDirection
{
    Ascending,
    Descending
}

public class FilterDef
{
    public string Column { get; set; } = "";
    public string Operator { get; set; } = "Contains"; // Contains, Equals, StartsWith, EndsWith, GreaterThan, LessThan, Between
    public string Value { get; set; } = "";
    public string? Value2 { get; set; } // For Between operator
}
