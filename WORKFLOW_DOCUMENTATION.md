# Workflow System Documentation

## Overview

The Workflow System for app-sqlstudio provides a comprehensive solution for creating, managing, and executing multi-step data workflows. It supports query execution, data transfers, and web service integrations.

## Key Features

### 1. Query Views
Save and reuse SQL queries with configurable parameters:
- Store queries with connection strings
- Define parameters with default values
- Execute queries via API
- Track execution history

### 2. Workflows
Create complex multi-step workflows with:
- Sequential step execution
- Error handling and retry logic
- Success/error flow control
- Timeout management
- Context-based data sharing between steps

### 3. Step Types

#### ExecuteQuery
Execute a saved QueryView and store results in workflow context.

**Configuration:**
```json
{
  "QueryViewId": 1,
  "ParameterValues": {
    "@CustomerId": "123"
  },
  "ResultKey": "CustomerData"
}
```

#### DataTransfer
Transfer data from a QueryView to a destination database.

**Configuration:**
```json
{
  "SourceQueryViewId": 1,
  "DestinationConnectionString": "Server=localhost;Database=Target;...",
  "DestinationTableName": "Customers",
  "Mode": "Upsert",
  "PrimaryKeyColumns": ["CustomerId"],
  "BatchSize": 1000
}
```

**Modes:**
- `Insert`: Simple bulk insert
- `Upsert`: Update if exists, insert otherwise
- `Truncate`: Clear table before insert

#### WebServiceCall
Call external web services with data from workflow context.

**Configuration:**
```json
{
  "Method": "POST",
  "Url": "https://api.example.com/customers",
  "Mode": "PerRecord",
  "DataSource": "CustomerData",
  "BodyTemplate": "{\"id\": \"{CustomerId}\", \"name\": \"{CustomerName}\"}",
  "Headers": {
    "Authorization": "Bearer token123",
    "Content-Type": "application/json"
  },
  "TimeoutSeconds": 30,
  "MaxRetries": 3,
  "SaveResponses": true,
  "ResponseTableName": "ApiResponses"
}
```

**Modes:**
- `PerRecord`: One HTTP call per record (with retry logic per record)
- `Batch`: Single HTTP call with all records

## API Endpoints

### QueryView Endpoints

```
GET    /api/queryview              - List all query views
GET    /api/queryview/{id}         - Get specific query view
POST   /api/queryview              - Create query view
PUT    /api/queryview/{id}         - Update query view
DELETE /api/queryview/{id}         - Delete query view
POST   /api/queryview/{id}/execute - Execute query view
```

### Workflow Endpoints

```
GET    /api/workflow                    - List all workflows
GET    /api/workflow/{id}               - Get specific workflow
POST   /api/workflow                    - Create workflow
PUT    /api/workflow/{id}               - Update workflow
DELETE /api/workflow/{id}               - Delete workflow
POST   /api/workflow/{id}/execute       - Execute workflow
GET    /api/workflow/{id}/executions    - Get execution history
GET    /api/workflow/executions/{id}    - Get execution details
GET    /api/workflow/{id}/statistics    - Get workflow statistics
```

## Example Workflow

Here's a complete example workflow that:
1. Fetches customer data from a database
2. Calls an external API for each customer
3. Saves API responses
4. Transfers enriched data to another database

### Step 1: Create QueryView for Source Data

```http
POST /api/queryview
{
  "Name": "ActiveCustomers",
  "Description": "Fetch active customers for sync",
  "SqlQuery": "SELECT CustomerId, CustomerName, Email FROM Customers WHERE IsActive = 1",
  "ConnectionString": "Data Source=local.db",
  "Parameters": []
}
```

### Step 2: Create QueryView for Enriched Data

```http
POST /api/queryview
{
  "Name": "EnrichedCustomers",
  "Description": "Combined customer data with API responses",
  "SqlQuery": "SELECT c.CustomerId, c.CustomerName, c.Email, a.ResponseBody FROM Customers c LEFT JOIN ApiResponses a ON c.CustomerId = a.RecordIndex",
  "ConnectionString": "Data Source=local.db",
  "Parameters": []
}
```

### Step 3: Create Workflow

```http
POST /api/workflow
{
  "Name": "Customer Data Sync",
  "Description": "Sync customer data with external system and transfer to data warehouse",
  "IsActive": true,
  "Steps": [
    {
      "Order": 1,
      "Name": "Fetch Active Customers",
      "Type": 0,
      "Configuration": "{\"QueryViewId\":1,\"ResultKey\":\"CustomerData\"}",
      "OnSuccess": "continue",
      "OnError": "end",
      "MaxRetries": 2,
      "TimeoutSeconds": 300
    },
    {
      "Order": 2,
      "Name": "Call External API",
      "Type": 2,
      "Configuration": "{\"Method\":\"POST\",\"Url\":\"https://api.example.com/enrich\",\"Mode\":\"PerRecord\",\"DataSource\":\"CustomerData\",\"BodyTemplate\":\"{\\\"customerId\\\":\\\"{CustomerId}\\\",\\\"name\\\":\\\"{CustomerName}\\\"}\",\"Headers\":{\"Authorization\":\"Bearer token123\"},\"TimeoutSeconds\":30,\"MaxRetries\":3,\"SaveResponses\":true,\"ResponseTableName\":\"ApiResponses\"}",
      "OnSuccess": "continue",
      "OnError": "continue",
      "MaxRetries": 1,
      "TimeoutSeconds": 600
    },
    {
      "Order": 3,
      "Name": "Prepare Enriched Data",
      "Type": 0,
      "Configuration": "{\"QueryViewId\":2,\"ResultKey\":\"EnrichedData\"}",
      "OnSuccess": "continue",
      "OnError": "end",
      "MaxRetries": 2,
      "TimeoutSeconds": 300
    },
    {
      "Order": 4,
      "Name": "Transfer to Data Warehouse",
      "Type": 1,
      "Configuration": "{\"SourceQueryViewId\":2,\"DestinationConnectionString\":\"Server=warehouse;Database=DW;...\",\"DestinationTableName\":\"DimCustomers\",\"Mode\":\"Upsert\",\"PrimaryKeyColumns\":[\"CustomerId\"],\"BatchSize\":1000}",
      "OnSuccess": "continue",
      "OnError": "end",
      "MaxRetries": 2,
      "TimeoutSeconds": 900
    }
  ]
}
```

### Step 4: Execute Workflow

```http
POST /api/workflow/1/execute
{
  "environment": "production",
  "date": "2024-01-29"
}
```

### Step 5: Monitor Execution

```http
GET /api/workflow/1/executions
```

Response:
```json
[
  {
    "id": 1,
    "workflowId": 1,
    "startTime": "2024-01-29T10:00:00Z",
    "endTime": "2024-01-29T10:05:30Z",
    "success": true,
    "totalSteps": 4,
    "completedSteps": 4,
    "durationSeconds": 330,
    "stepResults": [
      {
        "stepOrder": 1,
        "stepName": "Fetch Active Customers",
        "success": true,
        "recordsProcessed": 150,
        "durationSeconds": 5
      },
      {
        "stepOrder": 2,
        "stepName": "Call External API",
        "success": true,
        "recordsProcessed": 150,
        "recordsFailed": 2,
        "durationSeconds": 300
      },
      {
        "stepOrder": 3,
        "stepName": "Prepare Enriched Data",
        "success": true,
        "recordsProcessed": 148,
        "durationSeconds": 3
      },
      {
        "stepOrder": 4,
        "stepName": "Transfer to Data Warehouse",
        "success": true,
        "recordsProcessed": 148,
        "durationSeconds": 22
      }
    ]
  }
]
```

## Error Handling

### OnSuccess Actions
- `continue`: Proceed to next step (default)
- `end`: Stop workflow successfully
- `skip_to:N`: Jump to step N

### OnError Actions
- `end`: Stop workflow with error (default)
- `continue`: Continue to next step despite error
- `skip_to:N`: Jump to step N

### Retry Logic
Configure `MaxRetries` for each step. The system will:
- Retry failed steps up to the specified count
- Use exponential backoff for retries
- Log each retry attempt

## Database Schema

The system uses SQLite for metadata storage:

- `QueryViews`: Saved query definitions
- `QueryParameters`: Query parameters
- `Workflows`: Workflow definitions
- `WorkflowSteps`: Workflow steps
- `WorkflowExecutionResults`: Execution history
- `StepResults`: Step execution details

## Best Practices

1. **QueryView Design**
   - Use meaningful names
   - Add descriptions for documentation
   - Test queries before using in workflows
   - Use parameters for flexibility

2. **Workflow Design**
   - Keep steps focused and single-purpose
   - Use appropriate timeouts
   - Configure retry logic for transient failures
   - Use OnError actions to handle failures gracefully

3. **Web Service Calls**
   - Use `PerRecord` mode for APIs that require individual calls
   - Use `Batch` mode for APIs that accept bulk data
   - Configure appropriate timeouts
   - Enable SaveResponses for audit trails
   - Use retry logic for transient network issues

4. **Data Transfers**
   - Use `Upsert` mode for incremental updates
   - Configure appropriate batch sizes (1000-5000)
   - Ensure primary keys are set for Upsert mode
   - Use `Truncate` mode carefully (data loss risk)

5. **Monitoring**
   - Check execution history regularly
   - Monitor failed steps
   - Review step logs for errors
   - Track execution duration trends

## Security Considerations

1. **Connection Strings**: Store securely, consider using Azure Key Vault or similar
2. **API Keys**: Never hardcode, use configuration or secure storage
3. **SQL Injection**: Use parameters in QueryViews
4. **Access Control**: Implement authorization on API endpoints
5. **Audit Trail**: All executions are logged with details

## Performance Tips

1. **Bulk Operations**: Use batch sizes appropriately
2. **Indexing**: Ensure source and destination tables are indexed
3. **Parallel Execution**: Future enhancement (currently sequential)
4. **Resource Limits**: Configure timeouts to prevent runaway processes
5. **Data Volume**: Test with production-sized datasets

## Troubleshooting

### Common Issues

1. **Timeout Errors**
   - Increase `TimeoutSeconds` for the step
   - Optimize source queries
   - Reduce batch sizes

2. **Connection Failures**
   - Verify connection strings
   - Check network connectivity
   - Ensure credentials are valid

3. **Data Transfer Errors**
   - Verify table schemas match
   - Check primary key configuration for Upsert
   - Ensure target database permissions

4. **Web Service Errors**
   - Verify API endpoint is accessible
   - Check authentication headers
   - Review API rate limits
   - Enable SaveResponses to see error details

## Future Enhancements

- Parallel step execution
- Conditional branching
- Scheduled execution (cron)
- Email notifications
- Workflow templates
- Visual workflow designer UI
- Real-time execution monitoring
- Workflow versioning
- Data validation steps
- Transformation steps
