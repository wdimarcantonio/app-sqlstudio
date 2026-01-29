using SqlExcelBlazor.Server.Models.Analysis;
using System.Globalization;

namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Service for analyzing individual columns
/// </summary>
public class ColumnAnalyzer
{
    private readonly PatternDetector _patternDetector;
    private readonly StatisticsCalculator _statisticsCalculator;
    private readonly QualityScoreCalculator _qualityScoreCalculator;

    public ColumnAnalyzer(
        PatternDetector patternDetector,
        StatisticsCalculator statisticsCalculator,
        QualityScoreCalculator qualityScoreCalculator)
    {
        _patternDetector = patternDetector;
        _statisticsCalculator = statisticsCalculator;
        _qualityScoreCalculator = qualityScoreCalculator;
    }

    /// <summary>
    /// Analyze a single column from data
    /// </summary>
    public ColumnAnalysis AnalyzeColumn(string columnName, int columnIndex, List<object?> values, int topValueCount = 20)
    {
        var analysis = new ColumnAnalysis
        {
            ColumnName = columnName,
            ColumnIndex = columnIndex,
            DataType = "TEXT",
            TotalValues = values.Count
        };

        // Basic counts
        analysis.NullCount = values.Count(v => v == null || v == DBNull.Value);
        
        var nonNullValues = values.Where(v => v != null && v != DBNull.Value).Select(v => v!.ToString()!).ToList();
        
        analysis.EmptyStringCount = nonNullValues.Count(v => string.IsNullOrEmpty(v));
        analysis.WhitespaceOnlyCount = nonNullValues.Count(v => !string.IsNullOrEmpty(v) && string.IsNullOrWhiteSpace(v));
        analysis.UniqueCount = nonNullValues.Distinct().Count();

        // Percentages
        analysis.NullPercentage = analysis.TotalValues > 0 
            ? (decimal)analysis.NullCount / analysis.TotalValues * 100m 
            : 0m;
        int nonNullCount = analysis.TotalValues - analysis.NullCount;
        analysis.UniquePercentage = nonNullCount > 0 
            ? (decimal)analysis.UniqueCount / nonNullCount * 100m 
            : 0m;
        analysis.CompletenessPercentage = 100m - analysis.NullPercentage;

        // Infer type and compute type-specific statistics
        InferTypeAndComputeStatistics(analysis, nonNullValues);

        // Value distribution (top N)
        ComputeValueDistribution(analysis, nonNullValues, topValueCount);

        // Pattern detection for strings
        if (analysis.InferredType == "String" && nonNullValues.Any())
        {
            analysis.PatternCounts = _patternDetector.DetectPatterns(nonNullValues);
            analysis.DetectedPatterns = _patternDetector.GetDetectedPatternNames(analysis.PatternCounts);
        }

        // Calculate quality score
        analysis.QualityScore = _qualityScoreCalculator.CalculateQualityScore(analysis);
        analysis.QualityIssues = _qualityScoreCalculator.IdentifyQualityIssues(analysis);

        return analysis;
    }

    private void InferTypeAndComputeStatistics(ColumnAnalysis analysis, List<string> nonNullValues)
    {
        if (nonNullValues.Count == 0)
        {
            analysis.InferredType = "Unknown";
            return;
        }

        var numericValues = new List<decimal>();
        var dateValues = new List<DateTime>();
        var stringValues = nonNullValues.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();

        int numericCount = 0;
        int dateCount = 0;
        int boolCount = 0;

        foreach (var value in stringValues)
        {
            // Try numeric
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numValue))
            {
                numericValues.Add(numValue);
                numericCount++;
            }
            // Try date
            else if (DateTime.TryParse(value, out DateTime dateValue))
            {
                dateValues.Add(dateValue);
                dateCount++;
            }
            // Try boolean
            else if (value.ToLower() is "true" or "false" or "yes" or "no" or "1" or "0")
            {
                boolCount++;
            }
        }

        int totalNonEmpty = stringValues.Count;

        // Determine inferred type (need >80% consistency)
        if (numericCount > totalNonEmpty * 0.8)
        {
            analysis.InferredType = "Numeric";
            ComputeNumericStatistics(analysis, numericValues);
        }
        else if (dateCount > totalNonEmpty * 0.8)
        {
            analysis.InferredType = "DateTime";
            ComputeDateStatistics(analysis, dateValues);
        }
        else if (boolCount > totalNonEmpty * 0.8)
        {
            analysis.InferredType = "Boolean";
        }
        else if (numericCount > 0 || dateCount > 0 || boolCount > 0)
        {
            analysis.InferredType = "Mixed";
        }
        else
        {
            analysis.InferredType = "String";
            ComputeStringStatistics(analysis, stringValues);
        }
    }

    private void ComputeNumericStatistics(ColumnAnalysis analysis, List<decimal> values)
    {
        if (values.Count == 0) return;

        analysis.MinValue = values.Min();
        analysis.MaxValue = values.Max();
        analysis.AvgValue = values.Average();
        analysis.Sum = values.Sum();
        analysis.MedianValue = _statisticsCalculator.CalculateMedian(values);
        
        if (values.Count > 1)
        {
            analysis.StdDeviation = _statisticsCalculator.CalculateStdDeviation(values, analysis.AvgValue.Value);
        }
    }

    private void ComputeDateStatistics(ColumnAnalysis analysis, List<DateTime> values)
    {
        if (values.Count == 0) return;

        analysis.MinDate = values.Min();
        analysis.MaxDate = values.Max();
        analysis.DateRange = analysis.MaxDate - analysis.MinDate;
    }

    private void ComputeStringStatistics(ColumnAnalysis analysis, List<string> values)
    {
        if (values.Count == 0) return;

        var lengths = values.Select(v => v.Length).ToList();
        analysis.MinLength = lengths.Min();
        analysis.MaxLength = lengths.Max();
        analysis.AvgLength = lengths.Average();

        analysis.ShortestValue = values.OrderBy(v => v.Length).First();
        analysis.LongestValue = values.OrderBy(v => v.Length).Last();
    }

    private void ComputeValueDistribution(ColumnAnalysis analysis, List<string> values, int topN)
    {
        if (values.Count == 0)
        {
            analysis.ValueDistributions = new List<ValueDistribution>();
            return;
        }

        var distribution = values
            .GroupBy(v => v)
            .Select(g => new
            {
                Value = g.Key,
                Count = g.Count(),
                Percentage = (decimal)g.Count() / values.Count * 100m
            })
            .OrderByDescending(x => x.Count)
            .Take(topN)
            .Select((x, index) => new ValueDistribution
            {
                Value = x.Value,
                Count = x.Count,
                Percentage = x.Percentage,
                Rank = index + 1
            })
            .ToList();

        analysis.ValueDistributions = distribution;
    }
}
