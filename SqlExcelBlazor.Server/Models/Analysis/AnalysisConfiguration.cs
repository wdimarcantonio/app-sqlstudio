namespace SqlExcelBlazor.Server.Models.Analysis;

/// <summary>
/// Configuration options for data analysis
/// </summary>
public class AnalysisConfiguration
{
    public int TopValueCount { get; set; } = 20;
    public int SamplingThreshold { get; set; } = 100000;
    public decimal MinQualityScore { get; set; } = 80;
    public decimal MaxNullPercentage { get; set; } = 20;
    public bool EnablePatternDetection { get; set; } = true;
    public bool EnableParallelProcessing { get; set; } = true;
}
