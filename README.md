# SQL Excel App

Applicazione WPF .NET 8 per importare file Excel/CSV, eseguire query SQL e esportare risultati.

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

### 🔒 Session Management (NEW!)
- Isolamento completo delle sessioni utente tramite cookie
- Ogni browser/tab riceve automaticamente il proprio workspace SQLite in-memory
- Gestione automatica del ciclo di vita delle sessioni (timeout 30 minuti)
- Pulizia automatica delle sessioni inattive (ogni 5 minuti)
- API per gestione manuale delle sessioni (/api/sessions)
- Nessuna interferenza tra utenti diversi - ogni sessione è completamente isolata

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
# Dalla cartella del progetto
cd SqlExcelApp
dotnet restore
dotnet build
```

## Esecuzione

```bash
dotnet run
```

## Struttura Progetto

```
SqlExcelApp/
├── Models/
│   ├── ColumnDefinition.cs    # Definizione colonne con trasformazioni
│   ├── DataSource.cs          # Gestione multiple origini dati
│   ├── QueryResult.cs         # Risultato query
│   └── SqlServerConfig.cs     # Configurazione SQL Server
├── Services/
│   ├── ExcelService.cs        # Import/export Excel (ClosedXML)
│   ├── CsvService.cs          # Import/export CSV
│   ├── QueryService.cs        # Esecuzione query SQLite in-memory
│   ├── SqlServerService.cs    # Export verso SQL Server
│   ├── WorkspaceManager.cs    # Gestione sessioni utente (NEW!)
│   ├── IWorkspaceManager.cs   # Interface per gestione sessioni
│   └── SessionCleanupService.cs # Pulizia automatica sessioni
├── ViewModels/
│   └── MainViewModel.cs       # ViewModel principale (MVVM)
├── Views/
│   └── MainWindow.xaml        # Interfaccia principale
├── Styles/
│   └── ModernTheme.xaml       # Tema dark moderno
└── Converters/
    └── BoolConverters.cs      # Converters WPF
```

## Architettura Session Management

Il sistema utilizza un'architettura multi-utente con isolamento completo delle sessioni:

- **Session Middleware (ASP.NET Core)**: Gestisce sessioni automatiche tramite cookie (timeout 30 minuti)
- **WorkspaceManager (Singleton)**: Crea e gestisce workspace SQLite isolati per ogni session ID
- **SqliteService (Per Sessione)**: Ogni sessione riceve il proprio database in-memory completamente isolato
- **SessionCleanupService (Background)**: Rimuove automaticamente le sessioni inattive (> 30 minuti, ogni 5 minuti)
- **SessionsController (API)**: Endpoint REST per monitoraggio e gestione manuale delle sessioni

### Come Funziona

1. **L'utente apre l'app** → ASP.NET Core crea automaticamente una sessione con ID univoco (salvato in cookie)
2. **L'utente importa una tabella** → WorkspaceManager crea un SqliteService isolato per quella sessione
3. **L'utente esegue query** → Usa sempre lo stesso SqliteService con i propri dati
4. **Browser/tab diverso** → Nuova sessione → Nuovo SqliteService completamente separato

### Isolamento Garantito

- Ogni sessione ha il proprio database SQLite in-memory dedicato
- Le tabelle e i dati sono completamente separati tra sessioni
- Nessuna possibilità di interferenza o accesso ai dati di altre sessioni
- Session ID gestito automaticamente tramite cookie HTTP

## Tecnologie

- **.NET 8** - Framework
- **WPF** - User Interface
- **CommunityToolkit.Mvvm** - Pattern MVVM
- **ClosedXML** - Lettura/scrittura Excel (MIT License)
- **Microsoft.Data.Sqlite** - Database in-memory per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server

## Licenza

MIT License
