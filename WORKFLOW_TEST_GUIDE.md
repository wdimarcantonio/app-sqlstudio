# Workflow System Test Script

This script demonstrates how to create and execute a workflow using the API.

## Prerequisites

Start the application:
```bash
cd SqlExcelBlazor.Server
dotnet run
```

The API will be available at: https://localhost:5001 (or the port shown in console)

## Test Scenario

We'll create a simple workflow that:
1. Executes a query to get data from SQLite
2. Simulates a web service call
3. Transfers data

## Step 1: Create a QueryView

```bash
curl -X POST https://localhost:5001/api/queryview \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TestQuery",
    "description": "Test query for demonstration",
    "sqlQuery": "SELECT 1 as Id, '\''Test'\'' as Name",
    "connectionString": "Data Source=:memory:",
    "parameters": []
  }'
```

## Step 2: Create a Workflow

```bash
curl -X POST https://localhost:5001/api/workflow \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Workflow",
    "description": "Simple test workflow",
    "isActive": true,
    "steps": [
      {
        "order": 1,
        "name": "Execute Test Query",
        "type": 0,
        "configuration": "{\"QueryViewId\":1,\"ResultKey\":\"TestData\"}",
        "onSuccess": "continue",
        "onError": "end",
        "maxRetries": 1,
        "timeoutSeconds": 60
      }
    ]
  }'
```

## Step 3: Execute the Workflow

```bash
curl -X POST https://localhost:5001/api/workflow/1/execute \
  -H "Content-Type: application/json" \
  -d '{}'
```

## Step 4: Check Execution Results

```bash
curl https://localhost:5001/api/workflow/1/executions
```

## Step 5: Get Workflow Statistics

```bash
curl https://localhost:5001/api/workflow/1/statistics
```

## PowerShell Script (Windows)

```powershell
# Set base URL
$baseUrl = "https://localhost:5001"

# Ignore SSL certificate errors for testing
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

# Create QueryView
$queryView = @{
    name = "TestQuery"
    description = "Test query"
    sqlQuery = "SELECT 1 as Id, 'Test' as Name"
    connectionString = "Data Source=:memory:"
    parameters = @()
} | ConvertTo-Json

$qvResponse = Invoke-RestMethod -Uri "$baseUrl/api/queryview" `
    -Method Post `
    -Body $queryView `
    -ContentType "application/json"

Write-Host "Created QueryView with ID: $($qvResponse.id)"

# Create Workflow
$workflow = @{
    name = "Test Workflow"
    description = "Simple test"
    isActive = $true
    steps = @(
        @{
            order = 1
            name = "Execute Query"
            type = 0
            configuration = "{`"QueryViewId`":$($qvResponse.id),`"ResultKey`":`"TestData`"}"
            onSuccess = "continue"
            maxRetries = 1
            timeoutSeconds = 60
        }
    )
} | ConvertTo-Json -Depth 10

$wfResponse = Invoke-RestMethod -Uri "$baseUrl/api/workflow" `
    -Method Post `
    -Body $workflow `
    -ContentType "application/json"

Write-Host "Created Workflow with ID: $($wfResponse.id)"

# Execute Workflow
$execResponse = Invoke-RestMethod -Uri "$baseUrl/api/workflow/$($wfResponse.id)/execute" `
    -Method Post `
    -Body "{}" `
    -ContentType "application/json"

Write-Host "Workflow Execution Result:"
Write-Host "Success: $($execResponse.success)"
Write-Host "Duration: $($execResponse.durationSeconds) seconds"
Write-Host "Completed Steps: $($execResponse.completedSteps)/$($execResponse.totalSteps)"
```

## Notes

- The application uses SQLite for metadata storage (workflow.db)
- Connection strings and credentials should be secured in production
- HTTPS certificate validation is bypassed in the examples for local testing
- Adjust the base URL and port according to your setup
