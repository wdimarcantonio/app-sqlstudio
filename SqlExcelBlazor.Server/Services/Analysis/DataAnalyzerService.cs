using SqlExcelBlazor.Server.Models.Analysis;
using System.Data;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Main service for analyzing data tables
/// </summary>
public class DataAnalyzerService : IDataAnalyzerService
{
    private readonly ColumnAnalyzer _columnAnalyzer;
    private readonly ConcurrentDictionary<int, DataAnalysis> _analysisCache = new();
    private int _nextId = 0;

    public DataAnalyzerService(ColumnAnalyzer columnAnalyzer)
    {
        _columnAnalyzer = columnAnalyzer;
    }

    /// <summary>
    /// Analyze a table from SQLite
    /// </summary>
    public async Task<DataAnalysis> AnalyzeTableAsync(SqliteService sqliteService, string tableName, AnalysisConfiguration? config = null)
    {
        config ??= new AnalysisConfiguration();
        var stopwatch = Stopwatch.StartNew();

        // Validate table name - only allow alphanumeric, underscore, and hyphen
        if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_-]+$"))
        {
            throw new ArgumentException("Invalid table name. Only alphanumeric characters, underscore, and hyphen are allowed.", nameof(tableName));
        }

        // Get table data
        var queryResult = await sqliteService.ExecuteQueryAsync($"SELECT * FROM [{tableName}]");
        
        if (!queryResult.IsSuccess)
        {
            throw new Exception($"Failed to query table: {queryResult.ErrorMessage}");
        }

        var analysis = new DataAnalysis
        {
            Id = System.Threading.Interlocked.Increment(ref _nextId),
            SourceName = tableName,
            SourceType = "Table",
            AnalyzedAt = DateTime.UtcNow,
            TotalRows = queryResult.Rows.Count,
            TotalColumns = queryResult.Columns.Count
        };

        // Analyze each column
        var columnTasks = new List<Task<ColumnAnalysis>>();
        
        for (int i = 0; i < queryResult.Columns.Count; i++)
        {
            int columnIndex = i;
            string columnName = queryResult.Columns[i];
            
            if (config.EnableParallelProcessing)
            {
                columnTasks.Add(Task.Run(() =>
                {
                    var columnValues = queryResult.Rows.Select(row => row[columnName]).ToList();
                    return _columnAnalyzer.AnalyzeColumn(columnName, columnIndex, columnValues, config.TopValueCount);
                }));
            }
            else
            {
                var columnValues = queryResult.Rows.Select(row => row[columnName]).ToList();
                var columnAnalysis = _columnAnalyzer.AnalyzeColumn(columnName, columnIndex, columnValues, config.TopValueCount);
                analysis.ColumnAnalyses.Add(columnAnalysis);
            }
        }

        if (config.EnableParallelProcessing)
        {
            analysis.ColumnAnalyses = (await Task.WhenAll(columnTasks)).ToList();
        }

        stopwatch.Stop();
        analysis.AnalysisDuration = stopwatch.Elapsed;

        // Calculate overall quality score (average of all columns)
        if (analysis.ColumnAnalyses.Any())
        {
            analysis.OverallQualityScore = analysis.ColumnAnalyses.Average(c => c.QualityScore);
        }

        // Cache the analysis
        _analysisCache[analysis.Id] = analysis;

        return analysis;
    }

    /// <summary>
    /// Get a cached analysis by ID
    /// </summary>
    public DataAnalysis? GetAnalysis(int analysisId)
    {
        return _analysisCache.TryGetValue(analysisId, out var analysis) ? analysis : null;
    }

    /// <summary>
    /// Get analysis history for a source
    /// </summary>
    public List<DataAnalysis> GetAnalysisHistory(string sourceName)
    {
        return _analysisCache.Values
            .Where(a => a.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.AnalyzedAt)
            .ToList();
    }
}
