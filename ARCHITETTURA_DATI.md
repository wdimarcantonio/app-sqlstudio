# Architettura e Gestione Dati - SQL Excel Studio

## Domanda: Dove vengono memorizzati i dati quando inserisco un'origine dati?

**Risposta breve:** I dati vengono **copiati nel backend (server) nella memoria RAM** e **NON nel browser dell'utente**. Il browser contiene solo metadati e anteprime limitate.

---

## 📊 Architettura Completa

### Tipo di Architettura
**Server-Centrica**: Il server elabora e memorizza tutti i dati. Il browser funge solo da interfaccia utente.

```
┌─────────────────────────────────────────────────────────────────┐
│                         BROWSER (Client)                         │
│                                                                  │
│  ✅ Interfaccia utente (Blazor WebAssembly)                     │
│  ✅ Metadati origine dati (nomi colonne, conteggi righe)        │
│  ✅ Preview campioni (max 50 righe per visualizzazione)         │
│  ❌ NO dati completi                                            │
│  ❌ NO file salvati su disco                                    │
│                                                                  │
└───────────────────────────┬─────────────────────────────────────┘
                            │
                            │ HTTP/JSON API
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                         SERVER (Backend)                         │
│                                                                  │
│  ✅ Database SQLite in-memory (Data Source=:memory:)            │
│  ✅ File temporanei durante importazione (byte[] in RAM)        │
│  ✅ Elaborazione query SQL                                      │
│  ✅ Tutti i dati delle tabelle importate                        │
│  ⚠️  Isolamento per sessione (Scoped services)                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Flusso Completo: Importazione di un File Excel

### Fase 1: Upload del File

```
1. Utente seleziona file Excel nel browser
   └─ Componente: DataSourcesTab.razor (pulsante "📗 Importa Excel")
   └─ File rimane in memoria browser (NO salvataggio su disco)

2. File viene inviato al server via HTTP POST
   └─ Endpoint: POST /api/sqlite/excel/upload-temp
   └─ Metodo: Multipart form data
   └─ Dimensione max: Configurabile (default illimitato)
```

**Codice Client (SqliteApiClient.cs):**
```csharp
public async Task<TempFileInfo> UploadTempExcelAsync(IBrowserFile file)
{
    using var content = new MultipartFormDataContent();
    var stream = file.OpenReadStream(maxAllowedSize: long.MaxValue);
    var fileContent = new StreamContent(stream);
    content.Add(fileContent, "file", file.Name);
    
    // Invia al server
    var response = await _httpClient.PostAsync("api/sqlite/excel/upload-temp", content);
}
```

### Fase 2: Memorizzazione Temporanea nel Server

**Server riceve il file (SqliteController.cs):**
```csharp
[HttpPost("excel/upload-temp")]
public async Task<IActionResult> UploadTempExcel(IFormFile file)
{
    // Legge file in memoria
    using var stream = new MemoryStream();
    await file.CopyToAsync(stream);
    var data = stream.ToArray(); // ← Byte array in RAM
    
    // Salva temporaneamente
    var id = _excelService.AddTempFile(data, file.FileName);
    return Ok(new { fileId = id });
}
```

**Memorizzazione temporanea (ServerExcelService.cs):**
```csharp
private readonly ConcurrentDictionary<Guid, (byte[] Data, string FileName)> _tempFiles = new();

public Guid AddTempFile(byte[] data, string fileName)
{
    var id = Guid.NewGuid();
    _tempFiles[id] = (data, fileName); // ← Salvato in RAM server
    return id;
}
```

**Importante:**
- ✅ File salvato **solo in RAM server** (non su disco)
- ✅ Identificato da GUID unico
- ✅ **Isolato per sessione** (ogni utente ha il suo `ConcurrentDictionary`)
- ⏱️ Temporaneo: eliminato dopo l'importazione o alla fine della sessione

### Fase 3: Lettura e Anteprima Fogli

```
3. Server legge i fogli disponibili
   └─ Endpoint: GET /api/sqlite/excel/sheets/{fileId}
   └─ Usa ExcelDataReader per leggere struttura
   └─ Restituisce: Lista nomi fogli

4. Utente visualizza anteprima (prime 5 righe)
   └─ Endpoint: GET /api/sqlite/excel/preview/{fileId}/{sheetName}
   └─ Restituisce: JSON con preview limitata
   └─ Mostrato nel browser per conferma
```

### Fase 4: Importazione Definitiva

```
5. Utente conferma importazione foglio
   └─ Endpoint: POST /api/sqlite/excel/import
   └─ Server legge TUTTE le righe dal file temporaneo
   └─ Crea DataTable in memoria
   └─ Carica nel database SQLite in-memory
```

**Codice Server (SqliteController.cs):**
```csharp
[HttpPost("excel/import")]
public async Task<IActionResult> ImportExcelSheet([FromBody] ImportExcelRequest request)
{
    // 1. Recupera file temporaneo
    var tempFile = _excelService.GetTempFile(request.FileId);
    
    // 2. Legge tutti i dati dal foglio
    using var stream = new MemoryStream(tempFile.Value.Data);
    var dt = _excelService.GetAllData(stream, tempFile.Value.FileName, request.SheetName);
    
    // 3. Carica nel database SQLite in-memory
    await _sqliteService.LoadTableAsync(dt, request.TableName);
    
    // 4. Elimina file temporaneo
    _excelService.RemoveTempFile(request.FileId);
    
    return Ok();
}
```

**Database SQLite (SqliteService.cs):**
```csharp
private SqliteConnection? _connection;

private void EnsureConnection()
{
    if (_connection == null)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }
}

public async Task LoadTableAsync(DataTable data, string tableName)
{
    lock (_lock)
    {
        EnsureConnection();
        
        // 1. Crea tabella SQLite in-memory
        var createSql = GenerateCreateTableSql(data, tableName);
        using var createCmd = new SqliteCommand(createSql, _connection);
        createCmd.ExecuteNonQuery();
        
        // 2. Inserisce tutti i dati
        InsertData(data, tableName);
        
        _loadedTables.Add(tableName);
    }
}
```

### Fase 5: Sincronizzazione Metadati nel Browser

```
6. Server restituisce informazioni tabella
   └─ Nome tabella, numero righe, numero colonne
   
7. Browser aggiorna AppState
   └─ Aggiunge DataSource alla lista
   └─ Carica preview primi 50 record
   └─ Mostra nella UI
```

**Codice Client (DataSourcesTab.razor):**
```csharp
private async Task OnImportConfirmed(ImportConfirmation confirmation)
{
    // Importa nel server
    await SqliteApi.ImportExcelSheetAsync(confirmation.FileId, 
                                          confirmation.SheetName, 
                                          confirmation.TableName);
    
    // Carica metadati e preview
    var result = await SqliteApi.ExecuteQueryAsync(
        $"SELECT * FROM [{confirmation.TableName}] LIMIT 50");
    
    // Aggiunge a AppState (browser)
    var ds = new DataSource
    {
        TableName = confirmation.TableName,
        RowCount = totalRows,
        Columns = columns,
        Data = result.Rows.Take(50).ToList() // Solo prime 50 righe
    };
    
    AppState.AddDataSource(ds);
}
```

---

## 💾 Dove Risiedono i Dati: Tabella Dettagliata

| Componente | Posizione | Tipo Dati | Persistenza | Dimensione |
|------------|-----------|-----------|-------------|------------|
| **File Upload** | RAM Server | `byte[]` | Temporaneo (minuti) | File completo |
| **Database SQLite** | RAM Server | Tabelle in-memory | Sessione utente | Tutti i dati importati |
| **Metadati Tabelle** | RAM Browser | `List<DataSource>` | Sessione browser | Solo metadata (KB) |
| **Preview Dati** | RAM Browser | `List<Dictionary>` | Sessione browser | Max 50 righe per tabella |
| **Risultati Query** | RAM Browser | `QueryResult` | Fino a nuova query | Variabile |

### Esempio Pratico

**File Excel: `vendite_2025.xlsx` (5 MB, 10.000 righe)**

1. **Upload (t=0s)**
   - Browser: `vendite_2025.xlsx` in memoria (5 MB)
   - Server: `vendite_2025.xlsx` salvato come `byte[]` (5 MB in RAM)

2. **Importazione (t=2s)**
   - Server: Legge Excel → Crea tabella SQLite in-memory
   - Database: Tabella `Vendite2025` con 10.000 righe (memoria server)
   - Server: Elimina `byte[]` temporaneo ✅

3. **Dopo Importazione (t=5s)**
   - **Server**: 
     - Database SQLite: Tabella `Vendite2025` (10.000 righe) ✅
     - File temporaneo: ELIMINATO ❌
   
   - **Browser**:
     - Metadata: Nome tabella, 10.000 righe, nomi colonne (1 KB) ✅
     - Preview: Prime 50 righe (20 KB) ✅
     - Dati completi: NO ❌

4. **Query "SELECT * FROM Vendite2025 WHERE importo > 1000" (t=10s)**
   - Eseguita **sul server** nel database SQLite
   - Risultato inviato al browser come JSON
   - Browser mostra in griglia

---

## 🔒 Isolamento Sessioni

**Domanda:** Se due utenti importano lo stesso file, si vedono i dati a vicenda?

**Risposta:** **NO**, grazie all'architettura Scoped implementata.

### Isolamento Server-Side

```csharp
// Program.cs (SqlExcelBlazor.Server)
builder.Services.AddScoped<SqliteService>();           // ← Una istanza per sessione
builder.Services.AddScoped<ServerExcelService>();      // ← File temp per sessione
builder.Services.AddScoped<DataAnalyzerService>();     // ← Analisi per sessione
```

**Cosa significa:**
- Ogni **richiesta HTTP** ottiene una **nuova istanza** dei servizi
- Ogni utente ha il proprio **database SQLite in-memory isolato**
- Ogni utente ha il proprio **storage file temporanei isolato**

### Isolamento Client-Side

```csharp
// Program.cs (SqlExcelBlazor - Browser)
builder.Services.AddScoped<AppState>();                // ← Una istanza per tab browser
builder.Services.AddScoped<NotificationService>();
```

**Cosa significa:**
- Ogni **tab/finestra browser** ha il proprio **AppState isolato**
- Apertura di due tab = due sessioni separate

### Scenario: Due Utenti Contemporanei

```
┌──────────────────────────────────────────────────────────────┐
│ Utente A (Browser Chrome, Tab 1)                             │
│   └─ AppState A                                              │
│   └─ Connessione HTTP → Server                               │
│        └─ SqliteService A (DB in-memory A)                   │
│        └─ ServerExcelService A (_tempFiles A)                │
│                                                               │
│ Utente B (Browser Firefox, Tab 1)                            │
│   └─ AppState B                                              │
│   └─ Connessione HTTP → Server                               │
│        └─ SqliteService B (DB in-memory B)                   │
│        └─ ServerExcelService B (_tempFiles B)                │
│                                                               │
│ ✅ Utente A e B hanno dati completamente isolati             │
│ ❌ NON possono vedere o accedere ai dati dell'altro          │
└──────────────────────────────────────────────────────────────┘
```

---

## 🔍 Esecuzione Query: Dove Avviene?

**Domanda:** Quando eseguo `SELECT * FROM MiaTabella`, dove viene eseguita la query?

**Risposta:** **Sul server**, nel database SQLite in-memory.

### Flusso Query

```
1. Utente digita SQL: "SELECT * FROM Vendite WHERE anno = 2025"
   └─ Componente: ExecutionTab.razor

2. Click su "▶️ Esegui Query"
   └─ SqliteApiClient.ExecuteQueryAsync(sql)
   └─ POST /api/sqlite/query
   
3. Server riceve richiesta
   └─ SqliteController.ExecuteQuery(sql)
   └─ SqliteService.ExecuteQueryAsync(sql)
   
4. Esecuzione query SQLite in-memory
   └─ using var cmd = new SqliteCommand(sql, _connection);
   └─ using var reader = cmd.ExecuteReader();
   └─ Legge risultati in memoria
   
5. Risultati convertiti in JSON
   └─ { columns: [...], rows: [...], rowCount: 123 }
   
6. JSON inviato al browser
   └─ Browser visualizza in griglia
   └─ AppState.LastResult = risultato
```

**Importante:**
- ✅ Query eseguita **sempre sul server**
- ✅ Browser riceve **solo i risultati** (JSON)
- ✅ Nessun motore SQL nel browser
- ✅ Performance: SQLite è velocissimo per query in-memory

---

## 📈 Gestione Memoria e Performance

### Dimensioni Tipiche

| Scenario | Righe | RAM Server | RAM Browser |
|----------|-------|------------|-------------|
| Piccolo | 1.000 | ~1 MB | ~50 KB |
| Medio | 10.000 | ~10 MB | ~100 KB |
| Grande | 100.000 | ~100 MB | ~500 KB |
| Molto Grande | 1.000.000 | ~1 GB | ~500 KB |

### Limiti e Considerazioni

**Server:**
- ✅ Può gestire milioni di righe (limitato solo da RAM server)
- ✅ SQLite in-memory è estremamente veloce
- ⚠️ Memoria allocata per sessione utente
- ⚠️ Dati persi quando sessione termina

**Browser:**
- ✅ Leggero: contiene solo metadata e preview
- ✅ Nessun limite di dimensione file importato
- ⚠️ Limite 50 righe per preview (configurabile)
- ⚠️ Query grandi possono richiedere tempo per trasferimento JSON

### Quando la Memoria Server si Libera?

```
1. Utente chiude browser/tab
   └─ Sessione Blazor termina
   └─ SqliteService.Dispose() chiamato
   └─ _connection.Close() → DB eliminato
   └─ Garbage Collector libera memoria

2. Utente clicca "🗑️ Rimuovi" su origine dati
   └─ SqliteService.DropTableAsync(tableName)
   └─ DROP TABLE eliminata, memoria liberata parzialmente

3. Timeout sessione (configurabile)
   └─ Default: 20 minuti di inattività
   └─ Sessione scade automaticamente
```

---

## 🆚 Confronto con Alternative

### Opzione A: Tutto nel Browser (NON implementata)

```
❌ File Excel → Parsing JavaScript → IndexedDB browser

Vantaggi:
- Nessun carico server
- Lavora offline

Svantaggi:
- Limitazioni dimensione file (2-4 GB max browser)
- Performance ridotte per file grandi
- Parsing Excel complesso in JavaScript
- Query SQL richiederebbero libreria SQL.js (lenta)
```

### Opzione B: Storage Persistente Server (NON implementata)

```
❌ File Excel → Database SQL Server/PostgreSQL permanente

Vantaggi:
- Dati persistenti tra sessioni
- Condivisione tra utenti

Svantaggi:
- Richiede infrastruttura database
- Gestione backup e manutenzione
- Non adatto per analisi "usa e getta"
- Complessità gestione utenti multi-tenant
```

### Opzione C: In-Memory Server (✅ IMPLEMENTATA)

```
✅ File Excel → SQLite in-memory server

Vantaggi:
- Velocità massima (tutto in RAM)
- Nessuna configurazione database
- Isolamento automatico sessioni
- Ideale per analisi temporanee
- Supporta file enormi

Svantaggi:
- Dati persi alla chiusura sessione
- Usa RAM server per ogni utente
- Non condivisibile tra utenti
```

---

## 🎯 Riepilogo Finale

### ✅ Cosa Fa il Programma Quando Importi un'Origine Dati

1. **Upload File**
   - File inviato da browser a server via HTTP
   - Server salva `byte[]` in RAM (temporaneo)

2. **Lettura e Preview**
   - Server legge Excel/CSV con librerie dedicate
   - Invia preview limitata al browser

3. **Importazione Dati**
   - Server crea tabella SQLite in-memory
   - Inserisce tutti i dati nella tabella
   - Elimina file temporaneo

4. **Memorizzazione Finale**
   - **Server**: Database SQLite in-memory con tutti i dati
   - **Browser**: Solo metadata + preview (50 righe)

5. **Esecuzione Query**
   - Tutte le query eseguite sul server
   - Risultati inviati al browser come JSON

### ✅ Dove Sono i Dati?

| Cosa | Dove | Persistenza |
|------|------|-------------|
| **File caricato** | Server RAM (temp) | Minuti |
| **Dati completi** | Server RAM (SQLite) | Sessione |
| **Metadata** | Browser RAM | Sessione |
| **Preview** | Browser RAM | Sessione |
| **Su disco** | ❌ MAI | - |

### ✅ Domande Frequenti

**D: I dati sono salvati su disco?**  
R: **NO**, tutto è in memoria RAM (server e browser).

**D: Se chiudo il browser, i dati rimangono?**  
R: **NO**, tutti i dati vengono persi.

**D: Altri utenti possono vedere i miei dati?**  
R: **NO**, ogni sessione è completamente isolata.

**D: C'è un limite di dimensione file?**  
R: **NO limite client-side**, ma limitato dalla RAM disponibile sul server.

**D: Posso lavorare offline?**  
R: **NO**, è richiesta connessione al server per tutte le operazioni.

**D: Dove viene eseguita la query SQL?**  
R: **Sul server**, nel database SQLite in-memory.

---

## 📚 File Rilevanti nel Codice Sorgente

```
SqlExcelBlazor/                          (CLIENT - Browser)
├── Components/
│   ├── DataSourcesTab.razor            → Upload UI
│   └── ExcelImportDialog.razor         → Dialog importazione
├── Services/
│   ├── AppState.cs                     → Stato sessione browser
│   └── SqliteApiClient.cs              → Client HTTP API
└── Program.cs                          → Registrazione servizi Scoped

SqlExcelBlazor.Server/                   (SERVER - Backend)
├── Controllers/
│   └── SqliteController.cs             → API endpoints
├── Services/
│   ├── SqliteService.cs                → Database in-memory
│   └── ServerExcelService.cs           → Lettura Excel/CSV
└── Program.cs                          → Registrazione servizi Scoped
```

---

**Documento creato il:** 2026-02-14  
**Versione:** 1.0  
**Autore:** Sistema SQL Excel Studio
