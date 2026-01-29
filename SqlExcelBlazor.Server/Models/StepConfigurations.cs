namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Configuration for ExecuteQuery step
/// </summary>
public class ExecuteQueryStepConfig
{
    /// <summary>
    /// ID of the QueryView to execute
    /// </summary>
    public int QueryViewId { get; set; }

    /// <summary>
    /// Parameter values to pass to the query
    /// </summary>
    public Dictionary<string, string> ParameterValues { get; set; } = new();

    /// <summary>
    /// Key to store the result in the workflow context
    /// </summary>
    public string ResultKey { get; set; } = "QueryResult";
}

/// <summary>
/// Configuration for DataTransfer step
/// </summary>
public class DataTransferStepConfig
{
    /// <summary>
    /// Source query view ID
    /// </summary>
    public int SourceQueryViewId { get; set; }

    /// <summary>
    /// Destination connection string
    /// </summary>
    public string DestinationConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Destination table name
    /// </summary>
    public string DestinationTableName { get; set; } = string.Empty;

    /// <summary>
    /// Transfer mode: Insert, Upsert, Truncate
    /// </summary>
    public string Mode { get; set; } = "Insert";

    /// <summary>
    /// Primary key columns for Upsert mode
    /// </summary>
    public List<string> PrimaryKeyColumns { get; set; } = new();

    /// <summary>
    /// Batch size for bulk insert
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}

/// <summary>
/// Configuration for WebServiceCall step
/// </summary>
public class WebServiceStepConfig
{
    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE)
    /// </summary>
    public string Method { get; set; } = "POST";

    /// <summary>
    /// Endpoint URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Execution mode: PerRecord or Batch
    /// </summary>
    public string Mode { get; set; } = "Batch";

    /// <summary>
    /// Source query view ID or context key for data
    /// </summary>
    public string? DataSource { get; set; }

    /// <summary>
    /// Body template with placeholders (e.g., {"id": "{CustomerId}"})
    /// </summary>
    public string? BodyTemplate { get; set; }

    /// <summary>
    /// Custom headers
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum retries per call
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether to save responses to database
    /// </summary>
    public bool SaveResponses { get; set; } = false;

    /// <summary>
    /// Table name to save responses (if SaveResponses is true)
    /// </summary>
    public string? ResponseTableName { get; set; }
}
