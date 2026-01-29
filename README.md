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

## Session Management

### Isolamento Sessioni

Ogni utente/sessione ha il proprio database SQLite isolato in memoria:
- **Nessuna sovrapposizione tra utenti**: le tabelle di un utente non interferiscono con quelle di altri utenti
- **Tabelle con lo stesso nome in sessioni diverse sono indipendenti**: due utenti possono importare la stessa tabella "Clienti" senza conflitti
- **Cleanup automatico**: le sessioni inattive vengono automaticamente chiuse dopo 2 ore di inattività per liberare risorse

### Architettura

```
┌─────────────────────────────────────────────────────┐
│            WorkspaceManager (Singleton)              │
│  Mappa: SessionId → SessionWorkspace                 │
└─────────────────────────────────────────────────────┘
                        │
        ┌───────────────┴───────────────┐
        │                               │
        ▼                               ▼
┌──────────────────┐          ┌──────────────────┐
│  Session A       │          │  Session B       │
│  (User Alice)    │          │  (User Bob)      │
├──────────────────┤          ├──────────────────┤
│ SessionId: abc   │          │ SessionId: xyz   │
│ Connection:      │          │ Connection:      │
│ ┌──────────────┐ │          │ ┌──────────────┐ │
│ │ :memory:     │ │          │ │ :memory:     │ │
│ │ [Clienti]    │ │          │ │ [Clienti]    │ │
│ │ [Ordini]     │ │          │ │ [Prodotti]   │ │
│ └──────────────┘ │          │ └──────────────┘ │
└──────────────────┘          └──────────────────┘
```

**Componenti principali:**
- **WorkspaceManager**: gestisce il mapping SessionId → SQLite Connection
- **SqliteService**: usa connessioni session-scoped per l'isolamento
- **SessionCleanupService**: pulizia automatica delle sessioni inattive

### API Endpoints

- `GET /api/sessions/active` - Lista delle sessioni attive (utile per admin/debug)
- `GET /api/sessions/current` - Informazioni sulla sessione corrente

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
│   ├── SqliteService.cs       # Esecuzione query SQLite session-scoped
│   ├── WorkspaceManager.cs    # Gestione workspace per sessione
│   ├── SessionCleanupService.cs # Cleanup automatico sessioni inattive
│   └── SqlServerService.cs    # Export verso SQL Server
├── Controllers/
│   ├── SqliteController.cs    # API per query e gestione dati
│   └── SessionsController.cs  # API per monitoraggio sessioni
├── ViewModels/
│   └── MainViewModel.cs       # ViewModel principale (MVVM)
├── Views/
│   └── MainWindow.xaml        # Interfaccia principale
├── Styles/
│   └── ModernTheme.xaml       # Tema dark moderno
└── Converters/
    └── BoolConverters.cs      # Converters WPF
```

## Tecnologie

- **.NET 8** - Framework
- **WPF** - User Interface
- **CommunityToolkit.Mvvm** - Pattern MVVM
- **ClosedXML** - Lettura/scrittura Excel (MIT License)
- **Microsoft.Data.Sqlite** - Database in-memory per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server

## Licenza

MIT License
