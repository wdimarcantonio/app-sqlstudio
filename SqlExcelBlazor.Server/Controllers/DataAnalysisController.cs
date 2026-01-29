using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Models.Analysis;
using SqlExcelBlazor.Server.Services.Analysis;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataAnalysisController : ControllerBase
{
    private readonly IDataAnalyzerService _analyzerService;

    public DataAnalysisController(IDataAnalyzerService analyzerService)
    {
        _analyzerService = analyzerService;
    }

    /// <summary>
    /// Analyze a table
    /// </summary>
    [HttpPost("table")]
    public async Task<IActionResult> AnalyzeTable([FromBody] AnalyzeTableRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TableName))
            return BadRequest(new { success = false, error = "Table name is required" });

        try
        {
            var config = new AnalysisConfiguration
            {
                TopValueCount = request.TopValueCount ?? 20,
                EnablePatternDetection = request.EnablePatternDetection ?? true,
                EnableParallelProcessing = request.EnableParallelProcessing ?? true
            };

            var analysis = await _analyzerService.AnalyzeTableAsync(request.TableName, config);
            
            return Ok(new { success = true, analysis });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get analysis by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetAnalysis(int id)
    {
        var analysis = _analyzerService.GetAnalysis(id);
        
        if (analysis == null)
            return NotFound(new { success = false, error = "Analysis not found" });

        return Ok(new { success = true, analysis });
    }

    /// <summary>
    /// Get analysis history for a source
    /// </summary>
    [HttpGet("source/{sourceName}/history")]
    public IActionResult GetAnalysisHistory(string sourceName)
    {
        var history = _analyzerService.GetAnalysisHistory(sourceName);
        return Ok(new { success = true, history });
    }

    /// <summary>
    /// Get analysis summary
    /// </summary>
    [HttpGet("{id}/summary")]
    public IActionResult GetAnalysisSummary(int id)
    {
        var analysis = _analyzerService.GetAnalysis(id);
        
        if (analysis == null)
            return NotFound(new { success = false, error = "Analysis not found" });

        var summary = new
        {
            id = analysis.Id,
            sourceName = analysis.SourceName,
            sourceType = analysis.SourceType,
            analyzedAt = analysis.AnalyzedAt,
            totalRows = analysis.TotalRows,
            totalColumns = analysis.TotalColumns,
            overallQualityScore = analysis.OverallQualityScore,
            analysisDuration = analysis.AnalysisDuration.TotalSeconds,
            columnSummaries = analysis.ColumnAnalyses.Select(c => new
            {
                columnName = c.ColumnName,
                inferredType = c.InferredType,
                qualityScore = c.QualityScore,
                completenessPercentage = c.CompletenessPercentage,
                uniquePercentage = c.UniquePercentage,
                hasIssues = c.QualityIssues.Any()
            }).ToList()
        };

        return Ok(new { success = true, summary });
    }

    /// <summary>
    /// Get quality report for an analysis
    /// </summary>
    [HttpGet("{id}/quality-report")]
    public IActionResult GetQualityReport(int id)
    {
        var analysis = _analyzerService.GetAnalysis(id);
        
        if (analysis == null)
            return NotFound(new { success = false, error = "Analysis not found" });

        var report = new
        {
            overallScore = analysis.OverallQualityScore,
            totalIssues = analysis.ColumnAnalyses.Sum(c => c.QualityIssues.Count),
            columnIssues = analysis.ColumnAnalyses
                .Where(c => c.QualityIssues.Any())
                .Select(c => new
                {
                    columnName = c.ColumnName,
                    qualityScore = c.QualityScore,
                    issues = c.QualityIssues
                })
                .ToList(),
            recommendations = GenerateRecommendations(analysis)
        };

        return Ok(new { success = true, report });
    }

    private List<string> GenerateRecommendations(DataAnalysis analysis)
    {
        var recommendations = new List<string>();

        foreach (var column in analysis.ColumnAnalyses)
        {
            if (column.NullPercentage > 20)
            {
                recommendations.Add($"Consider making '{column.ColumnName}' required or provide default values");
            }

            if (column.InferredType == "Mixed")
            {
                recommendations.Add($"Standardize data type for '{column.ColumnName}' column");
            }

            if (column.DetectedPatterns.Contains("Email") && column.PatternCounts.TryGetValue("Email", out int emailCount))
            {
                int nonNullCount = column.TotalValues - column.NullCount;
                if (nonNullCount > 0 && (decimal)emailCount / nonNullCount < 0.95m)
                {
                    recommendations.Add($"Add email validation for '{column.ColumnName}' column");
                }
            }
        }

        return recommendations;
    }
}

public class AnalyzeTableRequest
{
    public string TableName { get; set; } = string.Empty;
    public int? TopValueCount { get; set; }
    public bool? EnablePatternDetection { get; set; }
    public bool? EnableParallelProcessing { get; set; }
}
