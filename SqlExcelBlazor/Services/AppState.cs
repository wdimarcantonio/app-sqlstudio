using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Stato dell'applicazione per sessione utente (Scoped)
/// </summary>
public class AppState
{
    private readonly QueryService _queryService = new();
    private readonly ExcelService _excelService = new();
    private readonly CsvService _csvService = new();
    private readonly SqlExportService _sqlExportService = new();
    
    // API Client per SQLite backend (iniettato)
    public SqliteApiClient? SqliteApi { get; set; }
    
    // Flag per usare SQLite backend invece del parser locale
    public bool UseSqliteBackend { get; set; } = true;
    
    public event Action? OnChange;
    
    // Origini dati
    public List<DataSource> DataSources { get; } = new();
    public DataSource? SelectedDataSource { get; set; }
    
    // Query Builder State (Persistente)
    public List<TableNode> VisualNodes { get; set; } = new();
    public List<ConnectionLink> VisualLinks { get; set; } = new();
    public List<DesignGridColumn> GridColumns { get; set; } = new(); // Colonne selezionate e trasformazioni
    
    // Query
    public string SqlQuery { get; set; } = "";
    public QueryResult? LastResult { get; set; }
    
    // UI State
    private bool _isLoading;
    public bool IsLoading 
    { 
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                NotifyStateChanged();
            }
        }
    }
    
    private string _statusMessage = "Pronto";
    public string StatusMessage 
    { 
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                NotifyStateChanged();
            }
        }
    }
    
    // Servizi
    public QueryService QueryService => _queryService;
    public ExcelService ExcelService => _excelService;
    public CsvService CsvService => _csvService;
    public SqlExportService SqlExportService => _sqlExportService;
    
    public void AddDataSource(DataSource dataSource)
    {
        // Assicura alias unico
        var alias = dataSource.TableAlias;
        int counter = 1;
        while (DataSources.Any(ds => ds.TableAlias.Equals(alias, StringComparison.OrdinalIgnoreCase)))
        {
            alias = $"{dataSource.TableAlias}{counter++}";
        }
        dataSource.TableAlias = alias;
        
        DataSources.Add(dataSource);
        _queryService.LoadTable(dataSource);
        
        if (SelectedDataSource == null)
            SelectedDataSource = dataSource;
        
        NotifyStateChanged();
    }
    
    public void RemoveDataSource(DataSource dataSource)
    {
        DataSources.Remove(dataSource);
        _queryService.RemoveTable(dataSource.TableAlias);
        
        // Rimuovi anche dallo stato visuale
        var removeNodes = VisualNodes.Where(n => n.DataSource.Id == dataSource.Id).ToList();
        foreach (var node in removeNodes) {
             VisualNodes.Remove(node);
             VisualLinks.RemoveAll(l => l.SourceTableId == node.Id || l.TargetTableId == node.Id);
        }
        
        GridColumns.RemoveAll(c => c.TableAlias == dataSource.TableAlias);
        
        if (SelectedDataSource == dataSource)
            SelectedDataSource = DataSources.FirstOrDefault();
        
        NotifyStateChanged();
    }
    
    public void ClearAll()
    {
        DataSources.Clear();
        _queryService.ClearAll();
        
        VisualNodes.Clear();
        VisualLinks.Clear();
        GridColumns.Clear();
        
        SelectedDataSource = null;
        LastResult = null;
        SqlQuery = "";
        NotifyStateChanged();
    }
    
    /// <summary>
    /// Esegue query usando il parser locale (vecchio metodo)
    /// </summary>
    public void ExecuteQuery()
    {
        if (string.IsNullOrWhiteSpace(SqlQuery))
        {
            StatusMessage = "Inserisci una query SQL";
            return;
        }
        
        IsLoading = true;
        NotifyStateChanged();
        
        try 
        {
            LastResult = _queryService.ExecuteQuery(SqlQuery);
            
            if (LastResult.IsSuccess)
            {
                StatusMessage = $"Query eseguita: {LastResult.RowCount} righe in {LastResult.ExecutionTime.TotalMilliseconds:F2}ms";
            }
            else
            {
                StatusMessage = LastResult.ErrorMessage ?? "Errore sconosciuto";
            }
        } 
        catch (Exception ex) 
        {
            LastResult = new QueryResult { IsSuccess = false, ErrorMessage = ex.Message };
        }
        
        IsLoading = false;
        NotifyStateChanged();
    }
    
    /// <summary>
    /// Esegue query usando SQLite backend (nuovo metodo)
    /// </summary>
    public async Task ExecuteQueryWithSqliteAsync()
    {
        if (string.IsNullOrWhiteSpace(SqlQuery))
        {
            StatusMessage = "Inserisci una query SQL";
            return;
        }
        
        if (SqliteApi == null)
        {
            StatusMessage = "SQLite API non disponibile";
            return;
        }
        
        IsLoading = true;
        NotifyStateChanged();
        
        try 
        {
            var result = await SqliteApi.ExecuteQueryAsync(SqlQuery);
            
            // Converti da SqliteQueryResult a QueryResult
            LastResult = new QueryResult
            {
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Columns = result.Columns,
                Rows = result.Rows,
                ExecutionTime = TimeSpan.FromMilliseconds(result.ExecutionTimeMs)
            };
            
            if (LastResult.IsSuccess)
            {
                StatusMessage = $"Query SQLite: {LastResult.RowCount} righe in {result.ExecutionTimeMs:F2}ms";
            }
            else
            {
                StatusMessage = LastResult.ErrorMessage ?? "Errore sconosciuto";
            }
        } 
        catch (Exception ex) 
        {
            LastResult = new QueryResult { IsSuccess = false, ErrorMessage = ex.Message };
            StatusMessage = ex.Message;
        }
        
        IsLoading = false;
        NotifyStateChanged();
    }
    
    public string GetLoadedTablesInfo()
    {
        if (DataSources.Count == 0)
            return "Nessuna tabella caricata";
        
        return $"Tabelle: {string.Join(", ", DataSources.Select(ds => $"[{ds.TableAlias}]"))}";
    }
    
    private void NotifyStateChanged() => OnChange?.Invoke();
}
