# SQL Excel App

Applicazione Blazor WebAssembly con architettura ibrida per importare file Excel/CSV, eseguire query SQL e esportare risultati.

## ⚡ Architettura Ibrida WASM + Server Locale

L'applicazione implementa un'architettura ibrida innovativa che combina:
- **Blazor WebAssembly** per query semplici e veloci nel browser
- **Server ASP.NET Core locale** per query complesse e grandi dataset
- **Smart routing automatico** basato sulla complessità della query

**Benefici:**
- Gestione dataset fino a 100k+ righe (vs 50k precedenti)
- Performance JOIN: da 30s a <1s
- Isolamento multi-utente con sessioni
- Storage persistente con SQLite file-based
- 100% locale, zero costi cloud

📖 **[Documentazione completa architettura ibrida](HYBRID_ARCHITECTURE.md)**

## Requisiti

- Windows 10/11
- .NET 8.0 SDK
- Visual Studio 2022 (opzionale)

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

### 📊 Data Analysis (NEW!)
- Analisi completa delle colonne con statistiche dettagliate
- Rilevamento automatico dei tipi di dato
- Pattern detection (email, URL, phone, ecc.)
- Quality scoring (0-100) per ogni colonna
- Identificazione automatica di problemi di qualità
- Visualizzazioni interattive con grafici e progress bar
- Statistiche specifiche per numeri, stringhe e date
- Distribuzione dei valori top N
- Report di qualità completi

Per maggiori dettagli sulla funzionalità Data Analysis, consulta [DATA_ANALYSIS.md](DATA_ANALYSIS.md).

## Compilazione

```bash
# Dalla root del progetto
dotnet restore
dotnet build
```

## Esecuzione

### Server (necessario per modalità ibrida)
```bash
cd SqlExcelBlazor.Server
dotnet run
# Il server si avvia su http://localhost:5001
```

### Client (in un nuovo terminale)
```bash
cd SqlExcelBlazor.Server
dotnet watch
# Il client WASM sarà disponibile su http://localhost:5001
```

## Struttura Progetto

```
SqlExcelBlazor.Server/ (Server ASP.NET Core)
├── Controllers/
│   ├── SessionController.cs       # Gestione sessioni
│   ├── QueryController.cs         # Esecuzione query server-side
│   ├── FileController.cs          # Upload/download file
│   ├── SqliteController.cs        # API SQLite legacy
│   └── DataAnalysisController.cs  # Analisi dati
├── Services/
│   ├── WorkspaceManager.cs        # Gestione workspace e session isolation
│   ├── SessionCleanupService.cs   # Background service cleanup
│   ├── SqliteService.cs           # Servizio SQLite in-memory
│   └── ServerExcelService.cs      # Servizio Excel server-side
└── Program.cs                      # Configurazione server

SqlExcelBlazor/ (Client Blazor WASM)
├── Components/                     # Componenti UI
├── Pages/                          # Pagine Blazor
├── Services/
│   ├── HybridQueryRouter.cs       # Smart routing WASM/Server
│   ├── ServerApiClient.cs         # Client API server
│   ├── QueryService.cs            # Parser SQL locale
│   ├── AppState.cs                # Stato applicazione
│   └── SqliteApiClient.cs         # Client API SQLite
├── Models/                         # Modelli dati
└── wwwroot/
    └── appsettings.json           # Configurazione client
```

## Tecnologie

- **.NET 9** - Framework
- **Blazor WebAssembly** - Client-side UI framework
- **ASP.NET Core** - Server framework
- **CommunityToolkit.Mvvm** - Pattern MVVM
- **ClosedXML** - Lettura/scrittura Excel (MIT License)
- **Microsoft.Data.Sqlite** - Database in-memory e file-based per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server
- **BlazorMonaco** - Editor SQL con syntax highlighting

## Licenza

MIT License
