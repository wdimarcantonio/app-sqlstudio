using System.Diagnostics;
using System.Text.RegularExpressions;
using SqlExcelBlazor.Models;

namespace SqlExcelBlazor.Services;

/// <summary>
/// Servizio per esecuzione query SQL in-memory in Blazor WebAssembly
/// Implementa un parser SQL semplificato che funziona interamente nel browser
/// </summary>
public class QueryService
{
    private readonly Dictionary<string, DataSource> _tables = new();
    
    public IReadOnlyDictionary<string, DataSource> Tables => _tables;
    public bool HasData => _tables.Count > 0;
    
    /// <summary>
    /// Carica una DataSource come tabella
    /// </summary>
    public void LoadTable(DataSource dataSource)
    {
        _tables[dataSource.TableAlias] = dataSource;
    }
    
    /// <summary>
    /// Rimuove una tabella
    /// </summary>
    public void RemoveTable(string alias)
    {
        _tables.Remove(alias);
    }
    
    /// <summary>
    /// Rimuove tutte le tabelle
    /// </summary>
    public void ClearAll()
    {
        _tables.Clear();
    }
    
    /// <summary>
    /// Esegue una query SQL semplificata
    /// Supporta: SELECT, FROM, WHERE, JOIN, ORDER BY
    /// </summary>
    public QueryResult ExecuteQuery(string sql)
    {
        var result = new QueryResult();
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Parse della query
            var normalizedSql = sql.Trim().Replace("\r", " ").Replace("\n", " ");
            
            // Estrai parti della query (Migliorato regex per supportare meglio i casi)
            var selectMatch = Regex.Match(normalizedSql, @"SELECT\s+(.+?)\s+FROM", RegexOptions.IgnoreCase);
            var fromMatch = Regex.Match(normalizedSql, @"FROM\s+\[?(\w+)\]?(?:\s+AS\s+\[?(\w+)\]?)?", RegexOptions.IgnoreCase);
            var joinMatches = Regex.Matches(normalizedSql, @"(INNER|LEFT|RIGHT)?\s*JOIN\s+\[?(\w+)\]?(?:\s+AS\s+\[?(\w+)\]?)?\s+ON\s+(.+?)(?=(?:INNER|LEFT|RIGHT)?\s*JOIN|WHERE|ORDER|$)", RegexOptions.IgnoreCase);
            var whereMatch = Regex.Match(normalizedSql, @"WHERE\s+(.+?)(?=ORDER\s+BY|$)", RegexOptions.IgnoreCase);
            var orderMatch = Regex.Match(normalizedSql, @"ORDER\s+BY\s+(.+?)$", RegexOptions.IgnoreCase);
            
            if (!selectMatch.Success || !fromMatch.Success)
            {
                result.ErrorMessage = "Query non valida. Formato: SELECT colonne FROM tabella [WHERE condizione] [ORDER BY colonna]";
                return result;
            }
            
            // Ottieni tabella principale
            var mainTableName = fromMatch.Groups[1].Value;
            if (!_tables.TryGetValue(mainTableName, out var mainTable))
            {
                result.ErrorMessage = $"Tabella '{mainTableName}' non trovata. Tabelle disponibili: {string.Join(", ", _tables.Keys)}";
                return result;
            }
            
            // Inizia con i dati della tabella principale
            var workingData = mainTable.Data.Select(row => 
                row.ToDictionary(kvp => $"{mainTableName}.{kvp.Key}", kvp => (object?)kvp.Value)
            ).ToList();
            
            // Aggiungi anche colonne senza prefisso per compatibilità
            for (int i = 0; i < workingData.Count; i++)
            {
                var original = mainTable.Data[i];
                foreach (var kvp in original)
                {
                    if (!workingData[i].ContainsKey(kvp.Key))
                    {
                        workingData[i][kvp.Key] = kvp.Value;
                    }
                }
            }
            
            // Gestisci JOIN
            foreach (Match joinMatch in joinMatches)
            {
                var joinTableName = joinMatch.Groups[2].Value;
                var joinCondition = joinMatch.Groups[4].Value.Trim();
                
                if (!_tables.TryGetValue(joinTableName, out var joinTable))
                {
                    result.ErrorMessage = $"Tabella JOIN '{joinTableName}' non trovata";
                    return result;
                }
                
                workingData = ApplyJoin(workingData, joinTable, joinTableName, joinCondition);
            }
            
            // Applica WHERE
            if (whereMatch.Success)
            {
                var whereCondition = whereMatch.Groups[1].Value.Trim();
                workingData = ApplyWhere(workingData, whereCondition);
            }
            
            // Determina le colonne da selezionare
            var selectPart = selectMatch.Groups[1].Value.Trim();
            List<string> selectedColumns;
            
            if (selectPart == "*")
            {
                selectedColumns = workingData.FirstOrDefault()?.Keys.Where(k => !k.Contains('.')).ToList() 
                    ?? mainTable.Columns;
            }
            else
            {
                selectedColumns = ParseSelectColumns(selectPart, workingData);
            }
            
            result.Columns = selectedColumns;
            
            // Costruisci righe risultato
            foreach (var row in workingData)
            {
                var resultRow = new Dictionary<string, object?>();
                foreach (var col in selectedColumns)
                {
                    // Prova vari formati di nome colonna
                    var val = FindValue(row, col, mainTableName);
                    resultRow[col] = val;
                }
                result.Rows.Add(resultRow);
            }
            
            // Applica ORDER BY (semplificato)
            if (orderMatch.Success)
            {
                var orderPart = orderMatch.Groups[1].Value.Trim();
                var descending = orderPart.EndsWith(" DESC", StringComparison.OrdinalIgnoreCase);
                var orderColumn = Regex.Replace(orderPart, @"\s+(ASC|DESC)$", "", RegexOptions.IgnoreCase).Trim().Trim('[', ']');
                
                result.Rows = descending
                    ? result.Rows.OrderByDescending(r => r.GetValueOrDefault(orderColumn)?.ToString()).ToList()
                    : result.Rows.OrderBy(r => r.GetValueOrDefault(orderColumn)?.ToString()).ToList();
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Errore nell'esecuzione della query: {ex.Message}";
        }
        
        stopwatch.Stop();
        result.ExecutionTime = stopwatch.Elapsed;
        return result;
    }
    
    private object? FindValue(Dictionary<string, object?> row, string col, string defaultTable)
    {
        if (row.TryGetValue(col, out var val)) return val;
        if (row.TryGetValue($"{defaultTable}.{col}", out val)) return val;
        
        // Cerca qualsiasi chiave che finisce con .col
        var match = row.Keys.FirstOrDefault(k => k.EndsWith($".{col}"));
        if (match != null) return row[match];
        
        return null;
    }
    
    private List<Dictionary<string, object?>> ApplyJoin(
        List<Dictionary<string, object?>> leftData, 
        DataSource rightTable, 
        string rightAlias,
        string condition)
    {
        var result = new List<Dictionary<string, object?>>();
        
        // Parse condizione (es: [Table1].[Col1] = [Table2].[Col2])
        // Semplificato: assume formato T1.C1 = T2.C2
        var parts = condition.Split('=', StringSplitOptions.TrimEntries);
        if (parts.Length != 2) return leftData;
        
        var leftRef = parts[0].Trim('[', ']');
        var rightRef = parts[1].Trim('[', ']');
        
        // Normalizza riferimenti
        // Es: Table1.ID -> Table1.ID
        
        foreach (var leftRow in leftData)
        {
            var leftValue = GetValueFromRow(leftRow, leftRef);
            if (leftValue == null) continue;
            
            var matchingRightRows = rightTable.Data.Where(r => 
            {
                // Qui dobbiamo capire quale parte della condizione si riferisce alla tabella destra
                // Assumiamo che rightRef sia quello della tabella destra se contiene il suo alias
                // O se leftRef contiene il suo alias
                
                string rightCol;
                if (rightRef.StartsWith(rightAlias + "."))
                    rightCol = rightRef.Substring(rightAlias.Length + 1).Trim('[', ']');
                else if (leftRef.StartsWith(rightAlias + "."))
                    rightCol = leftRef.Substring(rightAlias.Length + 1).Trim('[', ']');
                else
                    // Fallback: cerca la colonna pure
                     rightCol = rightTable.Columns.Contains(rightRef) ? rightRef : leftRef;

                var rightVal = r.GetValueOrDefault(rightCol);
                return rightVal == leftValue;
            });
            
            foreach (var rightRow in matchingRightRows)
            {
                var combinedRow = new Dictionary<string, object?>(leftRow);
                foreach (var kvp in rightRow)
                {
                    combinedRow[$"{rightAlias}.{kvp.Key}"] = kvp.Value;
                    if (!combinedRow.ContainsKey(kvp.Key))
                        combinedRow[kvp.Key] = kvp.Value;
                }
                result.Add(combinedRow);
            }
        }
        
        return result;
    }
    
    private string? GetValueFromRow(Dictionary<string, object?> row, string colRef)
    {
        // colRef può essere "T1.C1" o "C1"
        if (row.TryGetValue(colRef, out var val)) return val?.ToString();
        
        // Se è "C1" ma nel dizionario è "T1.C1"
        var match = row.Keys.FirstOrDefault(k => k == colRef || k.EndsWith($".{colRef}"));
        if (match != null) return row[match]?.ToString();
        
        return null;
    }
    
    private List<Dictionary<string, object?>> ApplyWhere(
        List<Dictionary<string, object?>> data, 
        string condition)
    {
        return data.Where(row => EvaluateCondition(row, condition)).ToList();
    }
    
    private bool EvaluateCondition(Dictionary<string, object?> row, string condition)
    {
        // Gestione molto base di AND
        if (condition.Contains(" AND ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = Regex.Split(condition, @"\s+AND\s+", RegexOptions.IgnoreCase);
            return parts.All(p => EvaluateCondition(row, p.Trim()));
        }
        
        var match = Regex.Match(condition, @"\[?([\w\.]+)\]?\s*(=|<>|!=|LIKE)\s*'?([^']*)'?", RegexOptions.IgnoreCase);
        if (!match.Success) return true;
        
        var colRef = match.Groups[1].Value;
        var op = match.Groups[2].Value.ToUpper();
        var value = match.Groups[3].Value;
        
        var rowValue = GetValueFromRow(row, colRef) ?? "";
        
        return op switch
        {
            "=" => rowValue.Equals(value, StringComparison.OrdinalIgnoreCase),
            "<>" or "!=" => !rowValue.Equals(value, StringComparison.OrdinalIgnoreCase),
            "LIKE" => Regex.IsMatch(rowValue, "^" + Regex.Escape(value).Replace("%", ".*") + "$", RegexOptions.IgnoreCase),
            _ => true
        };
    }
    
    private List<string> ParseSelectColumns(string selectPart, List<Dictionary<string, object?>> data)
    {
        var columns = new List<string>();
        var parts = selectPart.Split(',').Select(p => p.Trim());
        
        foreach (var part in parts)
        {
            var asMatch = Regex.Match(part, @"(.+?)\s+AS\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
            if (asMatch.Success)
            {
                columns.Add(asMatch.Groups[2].Value);
            }
            else
            {
                var colName = Regex.Replace(part, @"\[?(\w+)\]?\.\[?(\w+)\]?", "$2");
                colName = colName.Trim('[', ']');
                columns.Add(colName);
            }
        }
        
        return columns;
    }
    
    public string GenerateSelectQuery(IEnumerable<ColumnDefinition> columns, string tableName)
    {
        var selectedColumns = columns.Where(c => c.IsSelected).ToList();
        
        if (!selectedColumns.Any())
            return $"SELECT * FROM [{tableName}]";
        
        var columnExpressions = selectedColumns.Select(c => c.ToSqlExpression());
        return $"SELECT {string.Join(", ", columnExpressions)} FROM [{tableName}]";
    }
    
    public string GenerateJoinExample()
    {
        if (_tables.Count < 2) return "-- Carica almeno 2 tabelle per usare JOIN";
        
        var tables = _tables.Values.ToList();
        var first = tables[0];
        var second = tables[1];
        
        var commonColumns = first.Columns.Intersect(second.Columns).ToList();
        
        string joinCondition = commonColumns.Any()
            ? $"[{first.TableAlias}].[{commonColumns.First()}] = [{second.TableAlias}].[{commonColumns.First()}]"
            : $"[{first.TableAlias}].[ColonnaX] = [{second.TableAlias}].[ColonnaY]";
        
        return $@"SELECT 
    [{first.TableAlias}].*,
    [{second.TableAlias}].*
FROM [{first.TableAlias}]
INNER JOIN [{second.TableAlias}] ON {joinCondition}";
    }
}
