namespace SqlExcelBlazor.Models;

/// <summary>
/// Tipi di trasformazione applicabili alle colonne
/// </summary>
public enum TransformationType
{
    None,
    Upper,
    Lower,
    Trim,
    LeftTrim,
    RightTrim,
    Left,
    Right,
    Replace,
    Substring
}

/// <summary>
/// Definizione di una colonna con alias e trasformazioni
/// </summary>
public class ColumnDefinition
{
    public string OriginalName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public TransformationType Transformation { get; set; } = TransformationType.None;
    public string TransformationParameter { get; set; } = string.Empty;
    
    /// <summary>
    /// Genera l'espressione SQL per questa colonna
    /// </summary>
    public string ToSqlExpression()
    {
        string columnRef = $"[{OriginalName}]";
        string expression = Transformation switch
        {
            TransformationType.Upper => $"UPPER({columnRef})",
            TransformationType.Lower => $"LOWER({columnRef})",
            TransformationType.Trim => $"TRIM({columnRef})",
            TransformationType.LeftTrim => $"LTRIM({columnRef})",
            TransformationType.RightTrim => $"RTRIM({columnRef})",
            TransformationType.Left => $"SUBSTR({columnRef}, 1, {TransformationParameter})",
            TransformationType.Right => $"SUBSTR({columnRef}, -1 * {TransformationParameter})",
            TransformationType.Replace => GenerateReplaceExpression(columnRef),
            TransformationType.Substring => GenerateSubstringExpression(columnRef),
            _ => columnRef
        };
        
        string aliasName = string.IsNullOrWhiteSpace(Alias) ? OriginalName : Alias;
        return $"{expression} AS [{aliasName}]";
    }
    
    private string GenerateReplaceExpression(string columnRef)
    {
        var parts = TransformationParameter.Split('|');
        if (parts.Length >= 2)
            return $"REPLACE({columnRef}, '{parts[0]}', '{parts[1]}')";
        return columnRef;
    }
    
    private string GenerateSubstringExpression(string columnRef)
    {
        var parts = TransformationParameter.Split(',');
        if (parts.Length >= 2)
            return $"SUBSTR({columnRef}, {parts[0]}, {parts[1]})";
        return columnRef;
    }
}
