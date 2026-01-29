# SQL Excel App

Applicazione Blazor WebAssembly .NET 9 per importare file Excel/CSV, eseguire query SQL, esportare risultati e gestire workflow di elaborazione dati.

## Requisiti

- Windows 10/11 / Linux / macOS
- .NET 9.0 SDK
- Visual Studio 2022 o Visual Studio Code (opzionale)

## Funzionalità

### 📁 Importazione
- Importa file Excel (.xlsx, .xls)
- Importa file CSV
- Anteprima delle prime 10 righe
- Supporto per multiple origini dati

### 🔧 Costruzione Query
- Seleziona colonne da includere
- Assegna alias alle colonne
- Applica trasformazioni: UPPER, LOWER, TRIM, LEFT, RIGHT
- Generazione automatica della query SQL

### ▶️ Esecuzione Query
- Editor SQL con sintassi SQLite
- Esecuzione query con tempi di risposta
- Visualizzazione risultati in griglia
- Supporto JOIN tra tabelle (multiple origini dati)

### 📤 Export
- Esporta risultati in Excel (.xlsx)
- Importa in database SQL Server

### 🔄 Sistema Workflow (NUOVO!)
Il sistema workflow permette di creare e gestire flussi di lavoro automatizzati per l'elaborazione dati:

- **Query Views**: Salva e riutilizza query SQL con parametri configurabili
- **Workflow Multi-Step**: Crea workflow con step sequenziali
- **Step Executors**:
  - **ExecuteQuery**: Esegui query salvate
  - **DataTransfer**: Trasferisci dati tra database (Insert/Upsert/Truncate)
  - **WebServiceCall**: Chiama API esterne (modalità PerRecord o Batch)
- **Error Handling**: Gestione errori con retry automatico
- **Monitoring**: Traccia esecuzioni con log dettagliati
- **API REST**: Gestisci workflow via API

📖 **Documentazione completa**: [WORKFLOW_DOCUMENTATION.md](WORKFLOW_DOCUMENTATION.md)  
🧪 **Guida test**: [WORKFLOW_TEST_GUIDE.md](WORKFLOW_TEST_GUIDE.md)

## Compilazione

```bash
# Dalla cartella del progetto
cd SqlExcelBlazor.Server
dotnet restore
dotnet build
```

## Esecuzione

```bash
cd SqlExcelBlazor.Server
dotnet run
```

L'applicazione sarà disponibile su:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## Struttura Progetto

```
SqlExcelBlazor/
├── Models/                      # Modelli dati condivisi
├── Components/                  # Componenti Blazor
├── Pages/                       # Pagine Blazor
└── Services/                    # Servizi client

SqlExcelBlazor.Server/
├── Controllers/
│   ├── SqliteController.cs     # API per query SQL
│   ├── QueryViewController.cs  # API per QueryViews
│   └── WorkflowController.cs   # API per Workflow
├── Services/
│   ├── SqliteService.cs        # Gestione SQLite
│   ├── ExecuteQueryStepExecutor.cs    # Esecutore query
│   ├── DataTransferStepExecutor.cs    # Esecutore trasferimenti
│   ├── WebServiceStepExecutor.cs      # Esecutore chiamate API
│   └── WorkflowEngine.cs              # Motore workflow
├── Models/
│   ├── QueryView.cs            # Modello query salvate
│   ├── Workflow.cs             # Modello workflow
│   ├── WorkflowStep.cs         # Modello step workflow
│   └── WorkflowContext.cs      # Contesto esecuzione
├── Data/
│   └── ApplicationDbContext.cs # Context Entity Framework
└── Migrations/                 # Migrazioni database
```

## Tecnologie

- **.NET 9** - Framework
- **Blazor WebAssembly** - User Interface
- **Entity Framework Core** - ORM per workflow metadata
- **SQLite** - Database in-memory per query SQL e metadata
- **ClosedXML** - Lettura/scrittura Excel (MIT License)
- **Microsoft.Data.Sqlite** - Database in-memory per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server

## API Endpoints

### Query Views
```
GET    /api/queryview              # Lista query views
POST   /api/queryview              # Crea query view
GET    /api/queryview/{id}         # Dettagli query view
PUT    /api/queryview/{id}         # Aggiorna query view
DELETE /api/queryview/{id}         # Elimina query view
POST   /api/queryview/{id}/execute # Esegui query view
```

### Workflows
```
GET    /api/workflow                 # Lista workflows
POST   /api/workflow                 # Crea workflow
GET    /api/workflow/{id}            # Dettagli workflow
PUT    /api/workflow/{id}            # Aggiorna workflow
DELETE /api/workflow/{id}            # Elimina workflow
POST   /api/workflow/{id}/execute    # Esegui workflow
GET    /api/workflow/{id}/executions # Storico esecuzioni
GET    /api/workflow/{id}/statistics # Statistiche workflow
```

## Esempio Workflow Completo

```json
{
  "name": "Customer Data Sync",
  "description": "Sincronizza dati clienti con sistema esterno",
  "isActive": true,
  "steps": [
    {
      "order": 1,
      "name": "Fetch Customers",
      "type": 0,
      "configuration": "{\"QueryViewId\":1,\"ResultKey\":\"Customers\"}"
    },
    {
      "order": 2,
      "name": "Enrich via API",
      "type": 2,
      "configuration": "{\"Method\":\"POST\",\"Url\":\"https://api.example.com/enrich\",\"Mode\":\"PerRecord\",\"DataSource\":\"Customers\"}"
    },
    {
      "order": 3,
      "name": "Transfer to Warehouse",
      "type": 1,
      "configuration": "{\"SourceQueryViewId\":2,\"DestinationTableName\":\"DimCustomers\",\"Mode\":\"Upsert\"}"
    }
  ]
}
```

## Licenza

MIT License
