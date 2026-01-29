namespace SqlExcelBlazor.Server.Models.Analysis;

/// <summary>
/// Represents the analysis of a single column
/// </summary>
public class ColumnAnalysis
{
    public int Id { get; set; }
    public int DataAnalysisId { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public int ColumnIndex { get; set; }
    
    // Type information
    public string DataType { get; set; } = string.Empty; // SQL type
    public string InferredType { get; set; } = string.Empty; // "Numeric", "String", "DateTime", "Boolean", "Mixed"
    
    // Basic statistics
    public int TotalValues { get; set; }
    public int NullCount { get; set; }
    public int UniqueCount { get; set; }
    public int EmptyStringCount { get; set; }
    public int WhitespaceOnlyCount { get; set; }
    
    // Percentages
    public decimal NullPercentage { get; set; }
    public decimal UniquePercentage { get; set; }
    public decimal CompletenessPercentage { get; set; }
    
    // String statistics
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public double? AvgLength { get; set; }
    public string? ShortestValue { get; set; }
    public string? LongestValue { get; set; }
    
    // Numeric statistics
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public decimal? AvgValue { get; set; }
    public decimal? MedianValue { get; set; }
    public decimal? StdDeviation { get; set; }
    public decimal? Sum { get; set; }
    
    // Date statistics
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public TimeSpan? DateRange { get; set; }
    
    // Distribution
    public List<ValueDistribution> ValueDistributions { get; set; } = new();
    
    // Pattern detection
    public List<string> DetectedPatterns { get; set; } = new();
    public Dictionary<string, int> PatternCounts { get; set; } = new();
    
    // Quality indicators
    public decimal QualityScore { get; set; }
    public List<string> QualityIssues { get; set; } = new();
}
