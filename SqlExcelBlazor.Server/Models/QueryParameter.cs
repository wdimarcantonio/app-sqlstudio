using System.ComponentModel.DataAnnotations;

namespace SqlExcelBlazor.Server.Models;

/// <summary>
/// Represents a parameter that can be used in a QueryView
/// </summary>
public class QueryParameter
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to QueryView
    /// </summary>
    public int QueryViewId { get; set; }

    /// <summary>
    /// Parameter name (e.g., @CustomerId)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Data type of the parameter (String, Int, DateTime, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// Default value for the parameter
    /// </summary>
    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Navigation property to parent QueryView
    /// </summary>
    public QueryView? QueryView { get; set; }
}
