namespace SqlExcelBlazor.Models;

public class SqlLoadConnectionRequest
{
    public string Server { get; set; } = "";
    public string Database { get; set; } = "";
    public string AuthType { get; set; } = "sql"; // "sql" | "windows"
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool TrustServerCertificate { get; set; } = true;
}

public class SqlLoadRequest
{
    public string ConnectionString { get; set; } = "";
    public string TargetSchema { get; set; } = "dbo";
    public string TargetTable { get; set; } = "";
    public LoadMode Mode { get; set; } = LoadMode.Append;
    public bool TestMode { get; set; } = true;
    public Dictionary<string, string> ColumnMapping { get; set; } = new();
    public List<Dictionary<string, object?>> Data { get; set; } = new();
}

public enum LoadMode
{
    Append,
    TruncateInsert
}

public class SqlLoadResult
{
    public bool Success { get; set; }
    public int RowsAffected { get; set; }
    public string Message { get; set; } = "";
    public List<string> Errors { get; set; } = new();
    public bool WasTestMode { get; set; }
}

public class TableSchemaInfo
{
    public string Schema { get; set; } = "";
    public string TableName { get; set; } = "";
    public List<ColumnInfo> Columns { get; set; } = new();
}

public class ColumnInfo
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int? MaxLength { get; set; }
}

public class TableListRequest
{
    public string ConnectionString { get; set; } = "";
}

public class TableSchemaRequest
{
    public string ConnectionString { get; set; } = "";
    public string Schema { get; set; } = "dbo";
    public string TableName { get; set; } = "";
}
