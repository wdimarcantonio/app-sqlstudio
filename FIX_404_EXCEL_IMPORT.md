# Fix Errore 404 durante Import Excel

## Problema

Errore durante l'importazione di file Excel:
```
Errore lettura file: Response status code does not indicate success: 404 (Not Found)
```

## Causa

Il progetto utilizza un'architettura **Hosted Blazor WebAssembly** dove:
- Il **server** (SqlExcelBlazor.Server) hosta sia le API che il client Blazor
- Il **client** (SqlExcelBlazor) è un'applicazione WebAssembly servita dal server

Se si esegue il progetto client **standalone** (SqlExcelBlazor), questo cerca di chiamare le API sulla propria porta ma non trova nulla, causando errori 404.

## Soluzione: Eseguire il Progetto Server

### ✅ Metodo Corretto

**Eseguire il progetto SqlExcelBlazor.Server:**

```bash
cd SqlExcelBlazor.Server
dotnet run
```

Oppure da Visual Studio:
1. Imposta **SqlExcelBlazor.Server** come progetto di avvio
2. Premi F5 o click su "Esegui"

Il server:
- Si avvia sulla porta **5264** (http) o **7146** (https)
- Serve automaticamente il client Blazor
- Gestisce tutte le chiamate API

### ❌ Metodo Errato (Causa 404)

**NON eseguire il progetto SqlExcelBlazor standalone:**

```bash
cd SqlExcelBlazor
dotnet run  # ❌ QUESTO CAUSA IL 404!
```

## Architettura del Progetto

```
┌─────────────────────────────────────────────┐
│  Browser                                     │
│  http://localhost:5264                       │
└──────────────┬───────────────────────────────┘
               │
               │ HTTP Requests
               │
┌──────────────▼───────────────────────────────┐
│  SqlExcelBlazor.Server                       │
│  Port: 5264 (http) / 7146 (https)           │
│                                              │
│  ┌────────────────────────────────────────┐ │
│  │ API Controllers                         │ │
│  │ - /api/sqlite/*                        │ │
│  │ - /api/sqlserver/*                     │ │
│  │ - /api/dataanalysis/*                  │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  ┌────────────────────────────────────────┐ │
│  │ Blazor WebAssembly Files               │ │
│  │ (SqlExcelBlazor compiled files)        │ │
│  │ - index.html                           │ │
│  │ - _framework/blazor.webassembly.js     │ │
│  │ - _framework/*.dll                     │ │
│  └────────────────────────────────────────┘ │
└───────────────────────────────────────────────┘
```

## Verifica Configurazione

### 1. Verifica che il server sia in esecuzione

Apri il browser e vai a: `http://localhost:5264`

Dovresti vedere l'applicazione Blazor caricata correttamente.

### 2. Verifica che le API rispondano

Prova a chiamare un endpoint API:
```
http://localhost:5264/api/sqlite/tables
```

Se ricevi una risposta JSON (anche vuota `{"tables":[]}`), il server funziona.

### 3. Verifica nel DevTools

Apri DevTools del browser (F12) → scheda Network

Durante l'import Excel, dovresti vedere chiamate a:
- `POST http://localhost:5264/api/sqlite/excel/upload-temp`
- `GET http://localhost:5264/api/sqlite/excel/sheets/{id}`
- `POST http://localhost:5264/api/sqlite/excel/preview`

Se vedi chiamate a porta diversa (es. 5000 o 7233), stai eseguendo il client standalone.

## Configurazione Visual Studio

Per evitare di eseguire il progetto sbagliato:

1. Click destro su **SqlExcelBlazor.Server** nel Solution Explorer
2. Seleziona **"Imposta come progetto di avvio"**
3. Il progetto in grassetto indica quello che verrà eseguito

## Configurazione VS Code

Nel file `.vscode/launch.json`, assicurati di usare:

```json
{
  "name": "Launch Server",
  "type": "coreclr",
  "request": "launch",
  "preLaunchTask": "build",
  "program": "${workspaceFolder}/SqlExcelBlazor.Server/bin/Debug/net9.0/SqlExcelBlazor.Server.dll",
  "cwd": "${workspaceFolder}/SqlExcelBlazor.Server",
  "env": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

## File Modificati

Per risolvere questo problema e rendere la configurazione più chiara:

1. **SqlExcelBlazor/Program.cs** - Aggiunto commento esplicativo sulla configurazione HttpClient
2. **SqlExcelBlazor/Properties/launchSettings.json** - Aggiunto nota per non eseguire standalone
3. **SqlExcelBlazor/wwwroot/appsettings.json** - Creato per configurazione futura
4. **FIX_404_EXCEL_IMPORT.md** - Questa documentazione

## Risoluzione Problemi

### Errore persiste anche eseguendo il server

Se l'errore 404 persiste anche eseguendo SqlExcelBlazor.Server:

1. **Pulisci e ricompila:**
   ```bash
   dotnet clean
   dotnet build
   ```

2. **Verifica CORS** - Controlla che il server abbia CORS configurato (già presente in Program.cs)

3. **Verifica porte** - Assicurati che nessun altro processo usi la porta 5264/7146

4. **Controlla Console** - Guarda l'output della console del server per errori

### Altri errori comuni

**"Connection refused"**: Server non in esecuzione
- Soluzione: Avvia SqlExcelBlazor.Server

**"CORS policy error"**: Problema CORS
- Soluzione: Già risolto nel codice con `app.UseCors("AllowAll")`

**"File temporaneo non trovato"**: File scaduto
- Soluzione: Ricarica il file Excel

## Prossimi Passi

L'applicazione ora dovrebbe funzionare correttamente se:
1. Si esegue **SqlExcelBlazor.Server**
2. Si naviga a `http://localhost:5264`
3. Si importa un file Excel

Il problema 404 è risolto eseguendo il progetto corretto.

---

**Data:** 2026-02-14  
**Versione:** 1.0  
**Fix:** Configurazione Hosted Blazor WebAssembly
