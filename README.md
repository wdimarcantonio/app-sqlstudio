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
- Isolamento completo delle sessioni utente
- Ogni utente ha il proprio workspace SQLite in-memory
- Gestione automatica del ciclo di vita delle sessioni
- Pulizia automatica delle sessioni inattive (ogni 5 minuti)
- API per gestione manuale delle sessioni (/api/sessions)
- Supporto per applicazioni multi-utente scalabili

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

Il sistema fornisce l'infrastruttura per isolamento multi-utente delle sessioni:

- **WorkspaceManager (Singleton)**: Gestisce workspace utente isolati su richiesta
- **SqliteService**: Ogni istanza fornisce un database in-memory isolato
- **SessionCleanupService (Background)**: Rimuove automaticamente le sessioni inattive (> 30 minuti)
- **SessionsController (API)**: Endpoint REST per gestione manuale delle sessioni

L'infrastruttura è pronta per supportare sessioni utente isolate:
- WorkspaceManager può creare e gestire database SQLite in-memory separati per ogni sessione
- Ogni sessione riceve il proprio SqliteService con dati completamente isolati
- API disponibili per gestire manualmente le sessioni attive

Nota: L'integrazione completa con i controller esistenti è opzionale e può essere implementata quando necessario.

## Tecnologie

- **.NET 8** - Framework
- **WPF** - User Interface
- **CommunityToolkit.Mvvm** - Pattern MVVM
- **ClosedXML** - Lettura/scrittura Excel (MIT License)
- **Microsoft.Data.Sqlite** - Database in-memory per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server

## Licenza

MIT License
