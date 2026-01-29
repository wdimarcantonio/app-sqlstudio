using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Represents a saved SQL query that can be reused as a view
/// </summary>
public class QueryView
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the query view
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this query does
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// The SQL query to execute
    /// </summary>
    [Required]
    public string SqlQuery { get; set; } = string.Empty;

    /// <summary>
    /// Connection string to the database
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when this view was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time of last execution
    /// </summary>
    public DateTime? LastExecuted { get; set; }

    /// <summary>
    /// Parameters that can be used in the query
    /// </summary>
    public List<QueryParameter> Parameters { get; set; } = new();
}
