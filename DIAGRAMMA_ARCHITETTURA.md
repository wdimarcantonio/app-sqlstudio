# Diagramma Architettura - SQL Excel Studio

## Flusso Dati: Importazione Excel/CSV

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                          1. UPLOAD FILE (Utente)                              ║
╚═══════════════════════════════════════════════════════════════════════════════╝

    👤 Utente
     │
     │ Seleziona file Excel/CSV
     ▼
┌─────────────────────────┐
│   Browser (Client)      │
│                         │
│  📁 File in memoria     │
│     - Non salvato       │
│     - Solo buffer RAM   │
└────────────┬────────────┘
             │
             │ HTTP POST
             │ multipart/form-data
             │ File stream
             ▼

╔═══════════════════════════════════════════════════════════════════════════════╗
║                     2. RICEZIONE SERVER (Temporaneo)                          ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                         Server Backend                                       │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SqliteController.cs                                                  │   │
│  │   POST /api/sqlite/excel/upload-temp                                 │   │
│  └────────────────────────────┬─────────────────────────────────────────┘   │
│                                │                                              │
│                                ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ServerExcelService.cs                                                │   │
│  │                                                                       │   │
│  │  _tempFiles = ConcurrentDictionary<Guid, (byte[], string)>          │   │
│  │  {                                                                    │   │
│  │    "a1b2c3..." → (byte[5MB], "vendite.xlsx")  ← File in RAM         │   │
│  │  }                                                                    │   │
│  │                                                                       │   │
│  │  ⏱️  Temporaneo: eliminato dopo importazione                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘

             │
             │ Restituisce fileId
             ▼

╔═══════════════════════════════════════════════════════════════════════════════╗
║                      3. PREVIEW E CONFERMA (Utente)                           ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────┐
│   Browser (Client)      │
│                         │
│  ExcelImportDialog      │
│   - Selezione foglio    │
│   - Preview 5 righe     │
│   - Conferma            │
└────────────┬────────────┘
             │
             │ POST /api/sqlite/excel/import
             │ { fileId, sheetName, tableName }
             ▼

╔═══════════════════════════════════════════════════════════════════════════════╗
║                4. IMPORTAZIONE E MEMORIZZAZIONE DEFINITIVA                    ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                         Server Backend                                       │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SqliteController.cs                                                  │   │
│  │   1. Recupera file temporaneo                                        │   │
│  │   2. Legge tutte le righe Excel                                      │   │
│  │   3. Crea DataTable                                                  │   │
│  └────────────────────────────┬─────────────────────────────────────────┘   │
│                                │                                              │
│                                ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SqliteService.cs                                                     │   │
│  │                                                                       │   │
│  │  _connection = new SqliteConnection("Data Source=:memory:");        │   │
│  │                                                                       │   │
│  │  ┌──────────────────────────────────────────────────────────────┐  │   │
│  │  │ Database SQLite In-Memory                                     │  │   │
│  │  │                                                                │  │   │
│  │  │  Tabella: Vendite2025                                         │  │   │
│  │  │  ┌──────┬────────┬────────┬─────────┐                        │  │   │
│  │  │  │ ID   │ Data   │ Importo│ Cliente │                        │  │   │
│  │  │  ├──────┼────────┼────────┼─────────┤                        │  │   │
│  │  │  │ 1    │2025-01 │ 1500   │ Rossi   │                        │  │   │
│  │  │  │ 2    │2025-01 │ 2300   │ Bianchi │                        │  │   │
│  │  │  │ ...  │ ...    │ ...    │ ...     │                        │  │   │
│  │  │  │10000 │2025-12 │ 3200   │ Verdi   │                        │  │   │
│  │  │  └──────┴────────┴────────┴─────────┘                        │  │   │
│  │  │                                                                │  │   │
│  │  │  ✅ Tutti i dati (10.000 righe)                              │  │   │
│  │  │  ✅ Isolato per sessione                                      │  │   │
│  │  │  ⚡ Velocità massima (RAM)                                    │  │   │
│  │  │  ⏱️  Persistenza: fino a chiusura sessione                    │  │   │
│  │  └──────────────────────────────────────────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ServerExcelService.cs                                                │   │
│  │   RemoveTempFile(fileId) → ❌ File temporaneo ELIMINATO            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘

             │
             │ Restituisce: { success: true, rows: 10000, columns: 4 }
             ▼

╔═══════════════════════════════════════════════════════════════════════════════╗
║                  5. AGGIORNAMENTO STATO BROWSER                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                                      │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ AppState.cs                                                          │   │
│  │                                                                       │   │
│  │  DataSources = [                                                     │   │
│  │    {                                                                 │   │
│  │      TableName: "Vendite2025",                                       │   │
│  │      TableAlias: "Vendite2025",                                      │   │
│  │      RowCount: 10000,                    ← Solo metadata            │   │
│  │      ColumnCount: 4,                                                 │   │
│  │      Columns: ["ID","Data","Importo","Cliente"],                    │   │
│  │      Data: [                             ← Solo prime 50 righe      │   │
│  │        { ID: 1, Data: "2025-01", ... },                             │   │
│  │        { ID: 2, Data: "2025-01", ... },                             │   │
│  │        ...                                                           │   │
│  │        { ID: 50, Data: "2025-02", ... }                             │   │
│  │      ]                                                               │   │
│  │    }                                                                 │   │
│  │  ]                                                                   │   │
│  │                                                                       │   │
│  │  ✅ Leggero: ~100 KB                                                │   │
│  │  ❌ NO dati completi (solo 50/10000 righe)                          │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ DataSourcesTab.razor                                                 │   │
│  │                                                                       │   │
│  │  📊 Origini Dati                                                     │   │
│  │  ┌──────────────────────────────────────────────────────────────┐  │   │
│  │  │ 📗 Vendite2025                                                │  │   │
│  │  │    Righe: 10.000 | Colonne: 4                                │  │   │
│  │  │    [Visualizza] [Rinomina] [🗑️]                              │  │   │
│  │  └──────────────────────────────────────────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Flusso Query SQL

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                         ESECUZIONE QUERY SQL                                  ║
╚═══════════════════════════════════════════════════════════════════════════════╝

    👤 Utente digita: SELECT * FROM Vendite2025 WHERE Importo > 2000
     │
     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                                      │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ExecutionTab.razor                                                   │   │
│  │                                                                       │   │
│  │  [Editor SQL]                                                        │   │
│  │  SELECT * FROM Vendite2025 WHERE Importo > 2000                     │   │
│  │                                                                       │   │
│  │  [▶️ Esegui Query]                                                   │   │
│  └────────────────────────────┬─────────────────────────────────────────┘   │
└────────────────────────────────┼──────────────────────────────────────────────┘
                                 │
                                 │ POST /api/sqlite/query
                                 │ { sql: "SELECT * FROM ..." }
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Server Backend                                       │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SqliteController.cs                                                  │   │
│  │   POST /api/sqlite/query                                             │   │
│  └────────────────────────────┬─────────────────────────────────────────┘   │
│                                │                                              │
│                                ▼                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ SqliteService.cs                                                     │   │
│  │                                                                       │   │
│  │  ExecuteQueryAsync(sql)                                              │   │
│  │    │                                                                 │   │
│  │    ▼                                                                 │   │
│  │  ┌──────────────────────────────────────────────────────────────┐  │   │
│  │  │ Database SQLite In-Memory                                     │  │   │
│  │  │                                                                │  │   │
│  │  │  using var cmd = new SqliteCommand(sql, _connection);        │  │   │
│  │  │  using var reader = cmd.ExecuteReader();                     │  │   │
│  │  │                                                                │  │   │
│  │  │  ⚡ Query eseguita in RAM                                      │  │   │
│  │  │  ⏱️  Tempo: ~5ms per 10.000 righe                             │  │   │
│  │  │                                                                │  │   │
│  │  │  Risultato: 3.500 righe                                        │  │   │
│  │  └──────────────────────────────────────────────────────────────┘  │   │
│  │    │                                                                 │   │
│  │    ▼                                                                 │   │
│  │  Converte in JSON:                                                  │   │
│  │  {                                                                  │   │
│  │    columns: ["ID", "Data", "Importo", "Cliente"],                  │   │
│  │    rows: [                                                          │   │
│  │      { ID: 2, Data: "2025-01", Importo: 2300, Cliente: "Bianchi" },│   │
│  │      { ID: 50, Data: "2025-02", Importo: 3200, Cliente: "Verdi" }, │   │
│  │      ...                                                            │   │
│  │    ],                                                               │   │
│  │    rowCount: 3500,                                                  │   │
│  │    executionTimeMs: 5.2                                             │   │
│  │  }                                                                  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────┬───────────────────────────────────────┘
                                       │
                                       │ HTTP Response (JSON)
                                       │ ~700 KB per 3.500 righe
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                                      │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ ExecutionTab.razor                                                   │   │
│  │                                                                       │   │
│  │  ✅ Query eseguita in 5.2ms                                          │   │
│  │  📊 3.500 righe                                                      │   │
│  │                                                                       │   │
│  │  [Griglia Risultati]                                                 │   │
│  │  ┌──────┬────────┬────────┬─────────┐                              │   │
│  │  │ ID   │ Data   │ Importo│ Cliente │                              │   │
│  │  ├──────┼────────┼────────┼─────────┤                              │   │
│  │  │ 2    │2025-01 │ 2300   │ Bianchi │                              │   │
│  │  │ 50   │2025-02 │ 3200   │ Verdi   │                              │   │
│  │  │ ...  │ ...    │ ...    │ ...     │                              │   │
│  │  └──────┴────────┴────────┴─────────┘                              │   │
│  │                                                                       │   │
│  │  [📥 Esporta Excel] [📤 Esporta SQL Server]                         │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  AppState.LastResult = risultato query (in RAM browser)                    │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

## Isolamento Sessioni Multi-Utente

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                      ISOLAMENTO SESSIONI UTENTI                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                           👤 Utente A                                        │
│                        (Chrome, Tab 1)                                       │
│                                                                              │
│  Browser A                                                                   │
│  ├─ AppState A                                                              │
│  └─ DataSources: [Vendite2025, Clienti]                                    │
│                                                                              │
│                  │ HTTP Session A                                            │
│                  ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Server: Scoped Services A                                            │   │
│  │                                                                       │   │
│  │  SqliteService A                                                     │   │
│  │  └─ Connection A: :memory:                                           │   │
│  │     ├─ Tabella: Vendite2025 (10.000 righe)                          │   │
│  │     └─ Tabella: Clienti (500 righe)                                 │   │
│  │                                                                       │   │
│  │  ServerExcelService A                                                │   │
│  │  └─ _tempFiles A: { }                                                │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘

                            ❌ ISOLATO ❌
                      NON possono comunicare

┌─────────────────────────────────────────────────────────────────────────────┐
│                           👤 Utente B                                        │
│                        (Firefox, Tab 1)                                      │
│                                                                              │
│  Browser B                                                                   │
│  ├─ AppState B                                                              │
│  └─ DataSources: [Ordini]                                                   │
│                                                                              │
│                  │ HTTP Session B                                            │
│                  ▼                                                           │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │ Server: Scoped Services B                                            │   │
│  │                                                                       │   │
│  │  SqliteService B                                                     │   │
│  │  └─ Connection B: :memory:                                           │   │
│  │     └─ Tabella: Ordini (2.000 righe)                                │   │
│  │                                                                       │   │
│  │  ServerExcelService B                                                │   │
│  │  └─ _tempFiles B: { "xyz..." → (byte[], "ordini.xlsx") }            │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘

╔═══════════════════════════════════════════════════════════════════════════════╗
║  Registrazione Servizi Scoped (Program.cs)                                   ║
╚═══════════════════════════════════════════════════════════════════════════════╝

// SqlExcelBlazor.Server/Program.cs
builder.Services.AddScoped<SqliteService>();           // ← Istanza per sessione
builder.Services.AddScoped<ServerExcelService>();      // ← Istanza per sessione
builder.Services.AddScoped<DataAnalyzerService>();     // ← Istanza per sessione

// SqlExcelBlazor/Program.cs (Browser)
builder.Services.AddScoped<AppState>();                // ← Istanza per tab
builder.Services.AddScoped<NotificationService>();     // ← Istanza per tab

✅ Ogni utente → Propria istanza servizi
✅ Nessuna condivisione dati
✅ Isolamento garantito dal framework ASP.NET Core
```

---

## Ciclo di Vita Memoria

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                         CICLO DI VITA DATI                                    ║
╚═══════════════════════════════════════════════════════════════════════════════╝

t=0s    Utente apre applicazione
        │
        ├─ Browser: AppState creato
        ├─ Server: Nessun dato
        └─ RAM: ~10 MB (app vuota)

t=30s   Utente importa vendite.xlsx (5 MB, 10.000 righe)
        │
        ├─ Browser: Metadata + 50 righe preview (~100 KB)
        ├─ Server: SQLite in-memory (~10 MB dati)
        └─ RAM: ~20 MB totale

t=60s   Utente importa clienti.csv (1 MB, 500 righe)
        │
        ├─ Browser: Metadata + 50 righe preview (~120 KB)
        ├─ Server: SQLite in-memory (~11 MB dati)
        └─ RAM: ~21 MB totale

t=90s   Utente esegue query JOIN
        │
        ├─ Query eseguita su server in 8ms
        ├─ Risultato 2.000 righe inviato al browser
        └─ RAM: ~21.5 MB totale

t=20m   Utente inattivo per 20 minuti
        │
        └─ Timeout sessione

t=20m01s ⚡ PULIZIA AUTOMATICA
        │
        ├─ SqliteService.Dispose() chiamato
        ├─ _connection.Close() → DB eliminato
        ├─ ServerExcelService.Dispose() → temp files eliminati
        ├─ Garbage Collector libera memoria
        │
        └─ RAM: ~10 MB (app vuota)

t=infinite  Utente non torna
            │
            └─ Memoria completamente liberata ✅
```

---

## Memorizzazione Dati: Riepilogo Visuale

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                    DOVE SONO I DATI? - VISTA COMPLETA                         ║
╚═══════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  💻 BROWSER (Client-Side JavaScript)                                        │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ AppState (RAM)                                                      │    │
│  │                                                                      │    │
│  │  ✅ DataSources[] - Lista origini dati                             │    │
│  │     ├─ Nomi tabelle                                                │    │
│  │     ├─ Conteggi righe/colonne                                      │    │
│  │     ├─ Nomi colonne                                                │    │
│  │     └─ Preview dati (max 50 righe)                                │    │
│  │                                                                      │    │
│  │  ✅ LastResult - Ultimo risultato query                            │    │
│  │     ├─ Colonne                                                      │    │
│  │     └─ Righe risultato                                             │    │
│  │                                                                      │    │
│  │  ❌ NO dati completi tabelle                                       │    │
│  │  ❌ NO database locale                                             │    │
│  │                                                                      │    │
│  │  Dimensione tipica: 100-500 KB                                     │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

                                     ↕️  HTTP/JSON

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  🖥️  SERVER (Backend C#)                                                    │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ SqliteService (RAM)                                                 │    │
│  │                                                                      │    │
│  │  ✅ SqliteConnection (Data Source=:memory:)                        │    │
│  │     │                                                                │    │
│  │     ├─ Tabella: Vendite2025      (10.000 righe)                   │    │
│  │     ├─ Tabella: Clienti          (500 righe)                      │    │
│  │     ├─ Tabella: Prodotti         (1.200 righe)                    │    │
│  │     └─ Indici, Cache query                                         │    │
│  │                                                                      │    │
│  │  ✅ TUTTI i dati importati                                         │    │
│  │  ✅ Isolato per sessione utente                                    │    │
│  │  ✅ Performance massime (RAM)                                      │    │
│  │                                                                      │    │
│  │  Dimensione tipica: 10-1000 MB                                     │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │ ServerExcelService (RAM)                                            │    │
│  │                                                                      │    │
│  │  _tempFiles = Dictionary<Guid, byte[]>                             │    │
│  │     └─ File temporanei durante upload                              │    │
│  │     └─ Eliminati dopo importazione                                 │    │
│  │                                                                      │    │
│  │  Dimensione tipica: 0-50 MB (temporaneo)                           │    │
│  └────────────────────────────────────────────────────────────────────┘    │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘

                                     ↕️  MAI

┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│  💾 DISCO (Persistenza)                                                     │
│                                                                              │
│  ❌ Nessun file salvato                                                     │
│  ❌ Nessun database persistente                                             │
│  ❌ Nessun log dati utente                                                  │
│                                                                              │
│  Solo file applicazione (binari .dll)                                       │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

---

**Legenda:**
- ✅ = Memorizzato qui
- ❌ = NON memorizzato qui
- ⏱️  = Temporaneo
- ⚡ = Azione automatica
- 💻 = Client-side (Browser)
- 🖥️  = Server-side (Backend)
- 💾 = Storage persistente
