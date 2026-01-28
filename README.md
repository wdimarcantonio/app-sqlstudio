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
│   └── SqlServerService.cs    # Export verso SQL Server
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
