using System;
using System.Data;
using System.Linq;
using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

namespace Reproduction
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var files = Directory.GetFiles("Dummyfiles", "*.xlsx");
                foreach (var file in files)
                {
                    Console.WriteLine($"Processing {file}...");
                    var start = DateTime.Now;
                    var data = await ImportExcelAsync(file);
                    var duration = DateTime.Now - start;
                    Console.WriteLine($"Imported {file}. Rows: {data.Rows.Count}. Time: {duration.TotalSeconds}s");
                    
                    Console.WriteLine("Loading into SQLite...");
                    var qService = new QueryService();
                     var tableName = System.IO.Path.GetFileNameWithoutExtension(file)
                        .Replace(" ", "_").Replace("-", "_"); // simple alias logic
                    var startSql = DateTime.Now;
                    await qService.LoadDataAsync(data, tableName);
                    var durationSql = DateTime.Now - startSql;
                    Console.WriteLine($"Loaded into SQLite. Time: {durationSql.TotalSeconds}s");
                    
                    qService.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static async Task<DataTable> ImportExcelAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                using var workbook = new XLWorkbook(filePath);
                var worksheet = workbook.Worksheets.First();
                var dataTable = new DataTable();
                
                var headerRow = worksheet.FirstRowUsed();
                if (headerRow == null) return dataTable;
                
                foreach (var cell in headerRow.Cells())
                {
                    string columnName = cell.GetString();
                    if (string.IsNullOrWhiteSpace(columnName))
                        columnName = $"Column{cell.Address.ColumnNumber}";
                    string uniqueName = columnName;
                    int counter = 1;
                    while (dataTable.Columns.Contains(uniqueName)) uniqueName = $"{columnName}_{counter++}";
                    dataTable.Columns.Add(uniqueName, typeof(string));
                }
                
                var dataRows = worksheet.RowsUsed().Skip(1);
                foreach (var row in dataRows)
                {
                    var dataRow = dataTable.NewRow();
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        var cell = row.Cell(i + 1);
                        dataRow[i] = cell.GetString();
                    }
                    dataTable.Rows.Add(dataRow);
                }
                
                return dataTable;
            });
        }
    }

    public class ColumnDefinition
    {
        public string OriginalName { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        
        public string ToSqlExpression()
        {
            if (OriginalName == Alias)
                return $"[{OriginalName}]";
            return $"[{OriginalName}] AS [{Alias}]";
        }
    }

    public class QueryService : IDisposable
    {
        private SqliteConnection? _connection;
        private readonly List<string> _loadedTables = new();
        
        private void EnsureConnection()
        {
            if (_connection == null)
            {
                _connection = new SqliteConnection("Data Source=:memory:");
                _connection.Open();
            }
        }
        
        public async Task LoadDataAsync(DataTable data, string tableName = "ExcelData")
        {
            await Task.Run(() =>
            {
                EnsureConnection();
                
                if (_loadedTables.Contains(tableName))
                {
                    using var dropCmd = new SqliteCommand($"DROP TABLE IF EXISTS [{tableName}]", _connection);
                    dropCmd.ExecuteNonQuery();
                    _loadedTables.Remove(tableName);
                }
                
                var createTableSql = GenerateCreateTableSql(data, tableName);
                using var createCmd = new SqliteCommand(createTableSql, _connection);
                createCmd.ExecuteNonQuery();
                
                InsertData(data, tableName);
                
                _loadedTables.Add(tableName);
            });
        }
        
        private string GenerateCreateTableSql(DataTable data, string tableName)
        {
            var columns = new List<string>();
            foreach (DataColumn col in data.Columns)
            {
                columns.Add($"[{col.ColumnName}] TEXT");
            }
            return $"CREATE TABLE [{tableName}] ({string.Join(", ", columns)})";
        }
        
        private void InsertData(DataTable data, string tableName)
        {
            if (_connection == null) return;
            
            using var transaction = _connection.BeginTransaction();
            
            var columnNames = string.Join(", ", 
                data.Columns.Cast<DataColumn>().Select(c => $"[{c.ColumnName}]"));
            var parameters = string.Join(", ", 
                Enumerable.Range(0, data.Columns.Count).Select(i => $"@p{i}"));
            
            var insertSql = $"INSERT INTO [{tableName}] ({columnNames}) VALUES ({parameters})";
            
            foreach (DataRow row in data.Rows)
            {
                using var cmd = new SqliteCommand(insertSql, _connection, transaction);
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    cmd.Parameters.AddWithValue($"@p{i}", row[i]?.ToString() ?? "");
                }
                cmd.ExecuteNonQuery();
            }
            
            transaction.Commit();
        }
        
        public void Dispose()
        {
            _connection?.Close();
            _connection?.Dispose();
            _loadedTables.Clear();
        }
    }
}
