using SqlExcelBlazor.Server.Models.Analysis;

namespace SqlExcelBlazor.Server.Services.Analysis;

/// <summary>
/// Interface for data analysis service
/// </summary>
public interface IDataAnalyzerService
{
    Task<DataAnalysis> AnalyzeTableAsync(SqliteService sqliteService, string tableName, AnalysisConfiguration? config = null);
    DataAnalysis? GetAnalysis(int analysisId);
    List<DataAnalysis> GetAnalysisHistory(string sourceName);
}
