using SqlExcelBlazor.Server.Models;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SqlExcelBlazor.Server.Services;

/// <summary>
/// Executes web service call steps in a workflow
/// </summary>
public class WebServiceStepExecutor : IStepExecutor
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SqliteService _sqliteService;

    public WebServiceStepExecutor(IHttpClientFactory httpClientFactory, SqliteService sqliteService)
    {
        _httpClientFactory = httpClientFactory;
        _sqliteService = sqliteService;
    }

    public StepType StepType => StepType.WebServiceCall;

    public async Task<StepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, ILogger logger)
    {
        var result = new StepResult
        {
            StepOrder = step.Order,
            StepName = step.Name,
            StartTime = DateTime.UtcNow
        };

        try
        {
            // Parse configuration
            var config = step.GetConfiguration<WebServiceStepConfig>();
            if (config == null)
            {
                throw new Exception("Invalid WebServiceCall configuration");
            }

            logger.LogInformation($"Starting web service call to {config.Url} in {config.Mode} mode");

            // Get source data
            DataTable? sourceData = null;
            if (!string.IsNullOrEmpty(config.DataSource))
            {
                sourceData = context.GetDataTable(config.DataSource);
                if (sourceData == null)
                {
                    throw new Exception($"Data source '{config.DataSource}' not found in context");
                }
            }

            if (config.Mode.Equals("PerRecord", StringComparison.OrdinalIgnoreCase))
            {
                await ExecutePerRecordAsync(config, sourceData, result, context, logger);
            }
            else if (config.Mode.Equals("Batch", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteBatchAsync(config, sourceData, result, context, logger);
            }
            else
            {
                throw new Exception($"Unknown mode: {config.Mode}");
            }

            result.Success = result.RecordsFailed == 0 || result.RecordsProcessed > 0;
            result.EndTime = DateTime.UtcNow;

            logger.LogInformation($"Web service call completed. Processed: {result.RecordsProcessed}, Failed: {result.RecordsFailed}");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.EndTime = DateTime.UtcNow;
            result.ErrorMessage = ex.Message;
            result.LogDetails = $"Error: {ex.Message}\nStack Trace: {ex.StackTrace}";
            logger.LogError(ex, $"Error in web service step: {step.Name}");
        }

        return result;
    }

    private async Task ExecutePerRecordAsync(WebServiceStepConfig config, DataTable? sourceData, 
        StepResult result, WorkflowContext context, ILogger logger)
    {
        if (sourceData == null || sourceData.Rows.Count == 0)
        {
            result.LogDetails = "No data to process in PerRecord mode.";
            return;
        }

        var responses = new DataTable();
        if (config.SaveResponses)
        {
            responses.Columns.Add("RecordIndex", typeof(int));
            responses.Columns.Add("StatusCode", typeof(int));
            responses.Columns.Add("ResponseBody", typeof(string));
            responses.Columns.Add("ErrorMessage", typeof(string));
            responses.Columns.Add("Timestamp", typeof(DateTime));
        }

        var logBuilder = new StringBuilder();
        int recordIndex = 0;

        foreach (DataRow row in sourceData.Rows)
        {
            if (context.CancellationToken.IsCancellationRequested)
                break;

            recordIndex++;
            var retries = 0;
            var success = false;

            while (retries <= config.MaxRetries && !success)
            {
                try
                {
                    // Build request body from template
                    var body = ReplacePlaceholders(config.BodyTemplate ?? "{}", row);

                    // Make HTTP call
                    var response = await MakeHttpCallAsync(config, body, context.CancellationToken);

                    result.RecordsProcessed++;
                    success = true;

                    logBuilder.AppendLine($"Record {recordIndex}: Success (Status: {response.StatusCode})");

                    if (config.SaveResponses)
                    {
                        var responseRow = responses.NewRow();
                        responseRow["RecordIndex"] = recordIndex;
                        responseRow["StatusCode"] = (int)response.StatusCode;
                        responseRow["ResponseBody"] = response.Body ?? string.Empty;
                        responseRow["ErrorMessage"] = DBNull.Value;
                        responseRow["Timestamp"] = DateTime.UtcNow;
                        responses.Rows.Add(responseRow);
                    }
                }
                catch (Exception ex)
                {
                    retries++;
                    if (retries > config.MaxRetries)
                    {
                        result.RecordsFailed++;
                        logBuilder.AppendLine($"Record {recordIndex}: Failed after {retries} attempts - {ex.Message}");
                        logger.LogWarning($"Record {recordIndex} failed: {ex.Message}");

                        if (config.SaveResponses)
                        {
                            var responseRow = responses.NewRow();
                            responseRow["RecordIndex"] = recordIndex;
                            responseRow["StatusCode"] = 0;
                            responseRow["ResponseBody"] = DBNull.Value;
                            responseRow["ErrorMessage"] = ex.Message;
                            responseRow["Timestamp"] = DateTime.UtcNow;
                            responses.Rows.Add(responseRow);
                        }
                    }
                    else
                    {
                        await Task.Delay(1000 * retries, context.CancellationToken); // Exponential backoff
                    }
                }
            }
        }

        result.LogDetails = logBuilder.ToString();

        // Save responses if configured
        if (config.SaveResponses && responses.Rows.Count > 0 && !string.IsNullOrEmpty(config.ResponseTableName))
        {
            try
            {
                await _sqliteService.LoadTableAsync(responses, config.ResponseTableName);
                logger.LogInformation($"Saved {responses.Rows.Count} responses to table {config.ResponseTableName}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to save responses: {ex.Message}");
            }
        }
    }

    private async Task ExecuteBatchAsync(WebServiceStepConfig config, DataTable? sourceData, 
        StepResult result, WorkflowContext context, ILogger logger)
    {
        try
        {
            string body;

            if (sourceData != null && sourceData.Rows.Count > 0)
            {
                // Convert data table to JSON array
                var rows = new List<Dictionary<string, object?>>();
                foreach (DataRow row in sourceData.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in sourceData.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    rows.Add(dict);
                }

                if (!string.IsNullOrEmpty(config.BodyTemplate))
                {
                    // Replace placeholder with JSON array
                    body = config.BodyTemplate.Replace("{data}", JsonSerializer.Serialize(rows));
                }
                else
                {
                    body = JsonSerializer.Serialize(rows);
                }
            }
            else
            {
                body = config.BodyTemplate ?? "{}";
            }

            // Make HTTP call
            var response = await MakeHttpCallAsync(config, body, context.CancellationToken);

            result.RecordsProcessed = sourceData?.Rows.Count ?? 1;
            result.LogDetails = $"Batch call successful. Status: {response.StatusCode}\nResponse: {response.Body}";

            logger.LogInformation($"Batch call completed successfully with status {response.StatusCode}");
        }
        catch (Exception)
        {
            result.RecordsFailed = sourceData?.Rows.Count ?? 1;
            throw;
        }
    }

    private async Task<HttpResponse> MakeHttpCallAsync(WebServiceStepConfig config, string body, 
        CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        // Add custom headers
        foreach (var header in config.Headers)
        {
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }

        HttpResponseMessage response;

        switch (config.Method.ToUpper())
        {
            case "GET":
                response = await httpClient.GetAsync(config.Url, cancellationToken);
                break;
            case "POST":
                response = await httpClient.PostAsync(config.Url, 
                    new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
                break;
            case "PUT":
                response = await httpClient.PutAsync(config.Url, 
                    new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
                break;
            case "DELETE":
                response = await httpClient.DeleteAsync(config.Url, cancellationToken);
                break;
            default:
                throw new Exception($"Unsupported HTTP method: {config.Method}");
        }

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponse
        {
            StatusCode = response.StatusCode,
            Body = responseBody
        };
    }

    private string ReplacePlaceholders(string template, DataRow row)
    {
        // Replace {ColumnName} with actual values
        var result = template;
        var regex = new Regex(@"\{(\w+)\}");
        var matches = regex.Matches(template);

        foreach (Match match in matches)
        {
            var columnName = match.Groups[1].Value;
            if (row.Table.Columns.Contains(columnName))
            {
                var value = row[columnName];
                var valueStr = value == DBNull.Value ? string.Empty : value.ToString();
                result = result.Replace(match.Value, valueStr);
            }
        }

        return result;
    }

    private class HttpResponse
    {
        public System.Net.HttpStatusCode StatusCode { get; set; }
        public string? Body { get; set; }
    }
}
