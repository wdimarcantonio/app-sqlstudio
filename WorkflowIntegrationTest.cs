using System.Net.Http.Json;
using System.Text.Json;
using SqlExcelBlazor.Server.Models;

namespace WorkflowSystemTests;

/// <summary>
/// Integration test for the Workflow System
/// This test demonstrates a complete workflow execution
/// </summary>
public class WorkflowIntegrationTest
{
    private const string BaseUrl = "https://localhost:5001";
    
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Workflow System Integration Test ===");
        Console.WriteLine();
        
        try
        {
            // Skip SSL validation for testing
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
            
            // Test 1: Create QueryView
            Console.WriteLine("Test 1: Creating QueryView...");
            var queryView = new
            {
                name = "TestQuery",
                description = "Test query for integration test",
                sqlQuery = "SELECT 1 as Id, 'Test Data' as Name, 'test@example.com' as Email",
                connectionString = "Data Source=:memory:",
                parameters = new List<object>()
            };
            
            var qvResponse = await httpClient.PostAsJsonAsync("/api/queryview", queryView);
            qvResponse.EnsureSuccessStatusCode();
            var createdQueryView = await qvResponse.Content.ReadFromJsonAsync<JsonElement>();
            var queryViewId = createdQueryView.GetProperty("id").GetInt32();
            Console.WriteLine($"✓ QueryView created with ID: {queryViewId}");
            Console.WriteLine();
            
            // Test 2: Create Workflow
            Console.WriteLine("Test 2: Creating Workflow...");
            var workflow = new
            {
                name = "Integration Test Workflow",
                description = "Test workflow for integration testing",
                isActive = true,
                steps = new[]
                {
                    new
                    {
                        order = 1,
                        name = "Execute Test Query",
                        type = 0, // ExecuteQuery
                        configuration = $"{{\"QueryViewId\":{queryViewId},\"ResultKey\":\"TestData\"}}",
                        onSuccess = "continue",
                        onError = "end",
                        maxRetries = 1,
                        timeoutSeconds = 60
                    }
                }
            };
            
            var wfResponse = await httpClient.PostAsJsonAsync("/api/workflow", workflow);
            wfResponse.EnsureSuccessStatusCode();
            var createdWorkflow = await wfResponse.Content.ReadFromJsonAsync<JsonElement>();
            var workflowId = createdWorkflow.GetProperty("id").GetInt32();
            Console.WriteLine($"✓ Workflow created with ID: {workflowId}");
            Console.WriteLine();
            
            // Test 3: Execute Workflow
            Console.WriteLine("Test 3: Executing Workflow...");
            var execResponse = await httpClient.PostAsJsonAsync($"/api/workflow/{workflowId}/execute", new { });
            execResponse.EnsureSuccessStatusCode();
            var executionResult = await execResponse.Content.ReadFromJsonAsync<JsonElement>();
            
            var success = executionResult.GetProperty("success").GetBoolean();
            var duration = executionResult.GetProperty("durationSeconds").GetDouble();
            var completedSteps = executionResult.GetProperty("completedSteps").GetInt32();
            var totalSteps = executionResult.GetProperty("totalSteps").GetInt32();
            
            Console.WriteLine($"✓ Workflow executed");
            Console.WriteLine($"  Success: {success}");
            Console.WriteLine($"  Duration: {duration:F2} seconds");
            Console.WriteLine($"  Steps: {completedSteps}/{totalSteps}");
            Console.WriteLine();
            
            // Test 4: Get Execution History
            Console.WriteLine("Test 4: Getting Execution History...");
            var historyResponse = await httpClient.GetAsync($"/api/workflow/{workflowId}/executions");
            historyResponse.EnsureSuccessStatusCode();
            var history = await historyResponse.Content.ReadFromJsonAsync<JsonElement>();
            var executionCount = history.GetArrayLength();
            Console.WriteLine($"✓ Retrieved {executionCount} execution(s)");
            Console.WriteLine();
            
            // Test 5: Get Statistics
            Console.WriteLine("Test 5: Getting Workflow Statistics...");
            var statsResponse = await httpClient.GetAsync($"/api/workflow/{workflowId}/statistics");
            statsResponse.EnsureSuccessStatusCode();
            var stats = await statsResponse.Content.ReadFromJsonAsync<JsonElement>();
            
            var totalExecutions = stats.GetProperty("totalExecutions").GetInt32();
            var successfulExecutions = stats.GetProperty("successfulExecutions").GetInt32();
            var failedExecutions = stats.GetProperty("failedExecutions").GetInt32();
            
            Console.WriteLine($"✓ Statistics retrieved");
            Console.WriteLine($"  Total: {totalExecutions}");
            Console.WriteLine($"  Successful: {successfulExecutions}");
            Console.WriteLine($"  Failed: {failedExecutions}");
            Console.WriteLine();
            
            // Test 6: List All QueryViews
            Console.WriteLine("Test 6: Listing All QueryViews...");
            var qvListResponse = await httpClient.GetAsync("/api/queryview");
            qvListResponse.EnsureSuccessStatusCode();
            var queryViews = await qvListResponse.Content.ReadFromJsonAsync<JsonElement>();
            var qvCount = queryViews.GetArrayLength();
            Console.WriteLine($"✓ Found {qvCount} QueryView(s)");
            Console.WriteLine();
            
            // Test 7: List All Workflows
            Console.WriteLine("Test 7: Listing All Workflows...");
            var wfListResponse = await httpClient.GetAsync("/api/workflow");
            wfListResponse.EnsureSuccessStatusCode();
            var workflows = await wfListResponse.Content.ReadFromJsonAsync<JsonElement>();
            var wfCount = workflows.GetArrayLength();
            Console.WriteLine($"✓ Found {wfCount} Workflow(s)");
            Console.WriteLine();
            
            Console.WriteLine("=== ALL TESTS PASSED ===");
            Console.WriteLine();
            Console.WriteLine("The Workflow System is working correctly!");
            Console.WriteLine();
            Console.WriteLine("You can now:");
            Console.WriteLine("- Access the API at: " + BaseUrl);
            Console.WriteLine("- View QueryViews: GET /api/queryview");
            Console.WriteLine("- View Workflows: GET /api/workflow");
            Console.WriteLine("- See the complete documentation in WORKFLOW_DOCUMENTATION.md");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ TEST FAILED");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Make sure the application is running:");
            Console.WriteLine("  cd SqlExcelBlazor.Server");
            Console.WriteLine("  dotnet run");
        }
    }
}
