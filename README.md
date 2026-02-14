# SQL Excel App

**Applicazione Blazor WebAssembly .NET 9 per importare file Excel/CSV, eseguire query SQL e analizzare dati.**

## ⚠️ IMPORTANTE: Come Eseguire l'Applicazione

Questo è un progetto **Hosted Blazor WebAssembly**. Per eseguirlo correttamente:

### ✅ Metodo Corretto
```bash
cd SqlExcelBlazor.Server
dotnet run
```

Poi apri il browser su: `http://localhost:5264`

### ❌ NON eseguire il progetto client standalone
```bash
cd SqlExcelBlazor
dotnet run  # ❌ Questo causa errori 404!
```

**Perché?** Il server hosta sia le API che il client Blazor. Eseguendo il client standalone, non troverà le API.

Per dettagli sul problema 404, vedi: [FIX_404_EXCEL_IMPORT.md](FIX_404_EXCEL_IMPORT.md)

---

## Requisiti

- .NET 9.0 SDK
- Browser moderno (Chrome, Edge, Firefox)
- (Opzionale) Visual Studio 2022 o VS Code

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
# Dalla cartella del progetto server
cd SqlExcelBlazor.Server
dotnet restore
dotnet build
```

## Esecuzione

```bash
# IMPORTANTE: Eseguire sempre il progetto Server
cd SqlExcelBlazor.Server
dotnet run
```

Poi apri il browser su: `http://localhost:5264` o `https://localhost:7146`

### Visual Studio

1. Imposta **SqlExcelBlazor.Server** come progetto di avvio
2. Premi F5 o click su "Esegui"

### Risoluzione Problemi

Se ricevi errori 404 durante l'import Excel:
- Verifica di aver eseguito **SqlExcelBlazor.Server** e non SqlExcelBlazor
- Consulta: [FIX_404_EXCEL_IMPORT.md](FIX_404_EXCEL_IMPORT.md)

## Struttura Progetto

```
SqlExcelBlazor/                    # Client Blazor WebAssembly
├── Components/                    # Componenti UI Razor
├── Models/                        # Modelli dati
├── Services/                      # Client services
└── wwwroot/                       # File statici e CSS

SqlExcelBlazor.Server/             # Server ASP.NET Core
├── Controllers/                   # API Controllers
│   ├── SqliteController.cs       # API import/query
│   ├── SqlServerController.cs    # API SQL Server
│   └── DataAnalysisController.cs # API analisi dati
├── Services/                      # Server services
│   ├── SqliteService.cs          # SQLite in-memory
│   ├── ServerExcelService.cs     # Lettura Excel
│   └── Analysis/                 # Servizi analisi
└── Program.cs                    # Configurazione server
```

## Tecnologie

- **.NET 9** - Framework
- **Blazor WebAssembly** - UI Framework
- **ASP.NET Core** - Server API
- **ClosedXML** - Lettura/scrittura Excel
- **Microsoft.Data.Sqlite** - Database in-memory per query SQL
- **Microsoft.Data.SqlClient** - Connessione SQL Server
- **ExcelDataReader** - Lettura Excel alternativa

## Licenza

MIT License
