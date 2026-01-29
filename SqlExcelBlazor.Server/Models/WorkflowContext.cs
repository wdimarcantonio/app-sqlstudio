using System.Data;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Context that holds state during workflow execution
/// </summary>
public class WorkflowContext
{
    /// <summary>
    /// Variables that can be shared between steps
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Data tables that can be shared between steps
    /// </summary>
    public Dictionary<string, DataTable> DataTables { get; set; } = new();

    /// <summary>
    /// Execution start time
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cancellation token for the workflow
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Get a variable value
    /// </summary>
    public T? GetVariable<T>(string key)
    {
        if (Variables.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Set a variable value
    /// </summary>
    public void SetVariable(string key, object value)
    {
        Variables[key] = value;
    }

    /// <summary>
    /// Get a data table
    /// </summary>
    public DataTable? GetDataTable(string key)
    {
        return DataTables.TryGetValue(key, out var table) ? table : null;
    }

    /// <summary>
    /// Set a data table
    /// </summary>
    public void SetDataTable(string key, DataTable table)
    {
        DataTables[key] = table;
    }
}
