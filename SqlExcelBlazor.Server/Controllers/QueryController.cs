using Microsoft.AspNetCore.Mvc;
using SqlExcelBlazor.Server.Services;

namespace SqlExcelBlazor.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueryController : ControllerBase
{
    private readonly WorkspaceManager _workspaceManager;
    private readonly ILogger<QueryController> _logger;

    public QueryController(WorkspaceManager workspaceManager, ILogger<QueryController> logger)
    {
        _workspaceManager = workspaceManager;
        _logger = logger;
    }

    /// <summary>
    /// Executes a query on a specific session (server-side execution)
    /// </summary>
    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteQuery([FromBody] ExecuteQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return BadRequest(new { success = false, error = "SessionId is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Sql))
        {
            return BadRequest(new { success = false, error = "SQL query is required" });
        }

        try
        {
            var result = await _workspaceManager.ExecuteQueryAsync(request.SessionId, request.Sql);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query in session {SessionId}", request.SessionId);
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes query complexity to help router make decisions
    /// </summary>
    [HttpPost("analyze")]
    public IActionResult AnalyzeQuery([FromBody] AnalyzeQueryRequest request)
    {
        try
        {
            var analysis = AnalyzeQueryComplexity(request.Sql);
            return Ok(new
            {
                success = true,
                analysis = analysis
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing query");
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private QueryComplexityAnalysis AnalyzeQueryComplexity(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant();
        
        var hasJoin = sqlUpper.Contains(" JOIN ");
        var hasGroupBy = sqlUpper.Contains(" GROUP BY ");
        var hasOrderBy = sqlUpper.Contains(" ORDER BY ");
        var hasHaving = sqlUpper.Contains(" HAVING ");
        var hasSubquery = sqlUpper.Split('(').Length > 1 && sqlUpper.Contains("SELECT");
        
        // Count aggregations
        var aggregationCount = 0;
        var aggregations = new[] { "COUNT(", "SUM(", "AVG(", "MIN(", "MAX(", "GROUP_CONCAT(" };
        foreach (var agg in aggregations)
        {
            aggregationCount += CountOccurrences(sqlUpper, agg);
        }

        // Estimate complexity score (0-100)
        var complexityScore = 0;
        if (hasJoin) complexityScore += 30;
        if (hasGroupBy) complexityScore += 20;
        if (hasHaving) complexityScore += 15;
        if (hasSubquery) complexityScore += 25;
        complexityScore += Math.Min(aggregationCount * 5, 20);
        if (hasOrderBy) complexityScore += 10;

        return new QueryComplexityAnalysis
        {
            HasJoin = hasJoin,
            HasGroupBy = hasGroupBy,
            HasOrderBy = hasOrderBy,
            HasHaving = hasHaving,
            HasSubquery = hasSubquery,
            AggregationCount = aggregationCount,
            ComplexityScore = Math.Min(complexityScore, 100),
            RecommendedExecution = complexityScore > 30 ? "Server" : "Client"
        };
    }

    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}

// Request/Response DTOs
public class ExecuteQueryRequest
{
    public string SessionId { get; set; } = "";
    public string Sql { get; set; } = "";
}

public class AnalyzeQueryRequest
{
    public string Sql { get; set; } = "";
}

public class QueryComplexityAnalysis
{
    public bool HasJoin { get; set; }
    public bool HasGroupBy { get; set; }
    public bool HasOrderBy { get; set; }
    public bool HasHaving { get; set; }
    public bool HasSubquery { get; set; }
    public int AggregationCount { get; set; }
    public int ComplexityScore { get; set; }
    public string RecommendedExecution { get; set; } = "";
}
