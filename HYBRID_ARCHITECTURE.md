# Hybrid WASM + Local Server Architecture

## Overview

This application implements a hybrid architecture combining Blazor WebAssembly (WASM) client-side execution with a local ASP.NET Core server for enhanced performance and scalability.

## Architecture Components

### Server (SqlExcelBlazor.Server)

**Runs on:** `http://localhost:5001`

**Key Components:**

1. **WorkspaceManager** - Session isolation service
   - Dual SQLite strategy: in-memory for performance + file-based for persistence
   - Automatic session cleanup (>2 hours inactive)
   - Stores session databases in `%LOCALAPPDATA%/SqlStudio/data/sessions/`

2. **Controllers:**
   - `SessionController` - Session management (create/get/delete)
   - `QueryController` - Server-side query execution
   - `FileController` - File upload/download operations

3. **Background Services:**
   - `SessionCleanupService` - Periodic cleanup every 1 hour

### Client (SqlExcelBlazor - WASM)

**Key Components:**

1. **HybridQueryRouter** - Smart query routing
   - Analyzes query complexity
   - Routes based on criteria:
     - **WASM:** <5k rows, no JOINs, simple queries
     - **Server:** >5k rows, JOINs, complex queries
   - Automatic fallback to WASM if server unavailable

2. **ServerApiClient** - API communication
   - Session management
   - Query execution
   - File operations

## Configuration

### Server (appsettings.json)

```json
{
  "DataPath": "",  // Empty = uses %LOCALAPPDATA%/SqlStudio/data/
  "Urls": "http://localhost:5001"
}
```

### Client (wwwroot/appsettings.json)

```json
{
  "ServerApiUrl": "http://localhost:5001",
  "EnableHybridMode": true
}
```

## File Storage Structure

```
%LOCALAPPDATA%/SqlStudio/data/
├── sessions/
│   ├── session_{guid}.db  (SQLite database per session)
│   └── session_{guid}.db
├── workflows.db (future)
└── uploads/
    └── (temporary uploaded files)
```

## API Endpoints

### Session Management

- **POST** `/api/session/create` - Create new session
  ```json
  Request: { "userId": "optional-user-id" }
  Response: { "success": true, "sessionId": "guid", "createdAt": "timestamp" }
  ```

- **GET** `/api/session/{sessionId}` - Get session info
  ```json
  Response: {
    "success": true,
    "sessionId": "guid",
    "userId": "string",
    "createdAt": "timestamp",
    "lastAccessedAt": "timestamp",
    "loadedTables": []
  }
  ```

- **GET** `/api/session` - List all active sessions
  ```json
  Response: { "success": true, "sessions": [], "count": 0 }
  ```

- **DELETE** `/api/session/{sessionId}` - Close session

### Query Execution

- **POST** `/api/query/execute` - Execute query
  ```json
  Request: { "sessionId": "guid", "sql": "SELECT * FROM table" }
  Response: {
    "isSuccess": true,
    "columns": [],
    "rows": [],
    "rowCount": 0,
    "executionTimeMs": 0.0
  }
  ```

- **POST** `/api/query/analyze` - Analyze query complexity
  ```json
  Request: { "sql": "SELECT * FROM table" }
  Response: {
    "success": true,
    "analysis": {
      "hasJoin": false,
      "hasGroupBy": false,
      "complexityScore": 10,
      "recommendedExecution": "Client"
    }
  }
  ```

### File Operations

- **POST** `/api/file/upload-excel` - Upload Excel file
  - Form data: `file`, `sessionId`, `tableName`, `sheetName`

- **POST** `/api/file/upload-csv` - Upload CSV file
  - Form data: `file`, `sessionId`, `tableName`, `separator`

- **POST** `/api/file/download-excel` - Download query results as Excel
  ```json
  Request: {
    "sessionId": "guid",
    "sql": "SELECT * FROM table",
    "fileName": "results.xlsx",
    "sheetName": "Results"
  }
  ```

## Benefits

| Aspect | Before | After |
|--------|--------|-------|
| Max rows | 50k | 100k+ |
| JOIN performance | 30s | <1s |
| Multi-user | Conflicts | Isolated |
| Persistence | Memory only | File-based |
| Cost | - | €0 |
| Privacy | - | 100% local |

## Query Routing Logic

```
Query Complexity Analysis:
├── Has JOIN? → Server
├── >5k estimated rows? → Server
├── Complexity score >40? → Server
└── Otherwise → WASM

Complexity Score Calculation:
- JOIN: +30
- GROUP BY: +20
- HAVING: +15
- Subquery: +25
- Aggregations: +5 each (max +20)
- ORDER BY: +10
- >5k rows: +20
- >1k rows: +10
```

## Usage Example

1. **Start Server:**
   ```bash
   cd SqlExcelBlazor.Server
   dotnet run
   ```

2. **Create Session:**
   ```bash
   curl -X POST http://localhost:5001/api/session/create \
     -H "Content-Type: application/json" \
     -d '{"userId":"user1"}'
   ```

3. **Execute Query:**
   ```bash
   curl -X POST http://localhost:5001/api/query/execute \
     -H "Content-Type: application/json" \
     -d '{"sessionId":"...","sql":"SELECT * FROM table"}'
   ```

## Session Cleanup

- Sessions are automatically cleaned up after 2 hours of inactivity
- Cleanup runs every 1 hour via `SessionCleanupService`
- Session files are deleted when closed or cleaned up
- Can be manually closed via DELETE `/api/session/{sessionId}`

## Development Notes

- Server must be running for hybrid mode to work
- If server is unavailable, queries automatically fallback to WASM execution
- Session IDs are GUIDs and must be provided by the client
- All session data is isolated and stored in separate SQLite files
- CORS is configured to allow all origins in development mode
