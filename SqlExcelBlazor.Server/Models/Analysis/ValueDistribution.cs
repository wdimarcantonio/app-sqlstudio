namespace SqlExcelBlazor.Server.Models.Analysis;

/// <summary>
/// Represents the distribution of a specific value in a column
/// </summary>
public class ValueDistribution
{
    public int Id { get; set; }
    public int ColumnAnalysisId { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public int Rank { get; set; }
}
