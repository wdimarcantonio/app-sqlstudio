using SqlExcelBlazor.Server.Models.Analysis;

namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Service for calculating quality scores for columns
/// </summary>
public class QualityScoreCalculator
{
    /// <summary>
    /// Calculate quality score for a column (0-100)
    /// </summary>
    public decimal CalculateQualityScore(ColumnAnalysis analysis)
    {
        decimal score = 0;

        // Completeness (30%)
        score += analysis.CompletenessPercentage * 0.3m;

        // Type consistency (25%)
        decimal typeConsistencyScore = CalculateTypeConsistency(analysis);
        score += typeConsistencyScore * 0.25m;

        // Uniqueness where appropriate (20%)
        decimal uniquenessScore = CalculateUniquenessScore(analysis);
        score += uniquenessScore * 0.20m;

        // Pattern conformity (15%)
        decimal patternScore = CalculatePatternScore(analysis);
        score += patternScore * 0.15m;

        // No outliers (10%)
        decimal outlierScore = 100m; // Assume no outliers for now
        score += outlierScore * 0.10m;

        return Math.Min(100, Math.Max(0, score));
    }

    private decimal CalculateTypeConsistency(ColumnAnalysis analysis)
    {
        // If inferred type is "Mixed", reduce score
        if (analysis.InferredType == "Mixed")
            return 50m;

        return 100m;
    }

    private decimal CalculateUniquenessScore(ColumnAnalysis analysis)
    {
        // For ID-like columns, high uniqueness is good
        // For categorical columns, lower uniqueness is fine
        
        if (analysis.ColumnName.ToLower().Contains("id"))
        {
            return analysis.UniquePercentage;
        }

        // For other columns, moderate uniqueness is okay
        return 80m;
    }

    private decimal CalculatePatternScore(ColumnAnalysis analysis)
    {
        if (!analysis.DetectedPatterns.Any())
            return 100m;

        // If patterns are detected, check consistency
        int totalPatternMatches = analysis.PatternCounts.Values.Sum();
        int nonNullValues = analysis.TotalValues - analysis.NullCount;

        if (nonNullValues == 0)
            return 100m;

        decimal patternMatchPercentage = (decimal)totalPatternMatches / nonNullValues * 100m;
        return Math.Min(100m, patternMatchPercentage);
    }

    /// <summary>
    /// Identify quality issues for a column
    /// </summary>
    public List<string> IdentifyQualityIssues(ColumnAnalysis analysis)
    {
        var issues = new List<string>();

        if (analysis.NullPercentage > 20m)
        {
            issues.Add($"High null rate ({analysis.NullPercentage:F1}% > threshold 20%)");
        }

        if (analysis.InferredType == "Mixed")
        {
            issues.Add("Mixed data types detected");
        }

        if (analysis.EmptyStringCount > 0 && analysis.InferredType == "String")
        {
            decimal emptyPercentage = (decimal)analysis.EmptyStringCount / analysis.TotalValues * 100m;
            if (emptyPercentage > 5m)
            {
                issues.Add($"High empty string rate ({emptyPercentage:F1}%)");
            }
        }

        // Check for very low uniqueness in ID columns
        if (analysis.ColumnName.ToLower().Contains("id") && analysis.UniquePercentage < 95m)
        {
            issues.Add($"Low uniqueness for ID column ({analysis.UniquePercentage:F1}%)");
        }

        return issues;
    }
}
