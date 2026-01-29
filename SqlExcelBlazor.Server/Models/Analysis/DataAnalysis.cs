namespace SqlExcelBlazor.Server.Models.Analysis;

/// <summary>
/// Represents a complete data analysis of a table or query result
/// </summary>
public class DataAnalysis
{
    public int Id { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // "Table", "QueryView", "CustomQuery"
    public int? SourceId { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public List<ColumnAnalysis> ColumnAnalyses { get; set; } = new();
    public TimeSpan AnalysisDuration { get; set; }
    public decimal OverallQualityScore { get; set; }
}
