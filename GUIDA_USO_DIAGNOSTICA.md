# 🔍 Guida Uso Pagina Diagnostica

## Problema Persistente

Se dopo aver eseguito lo script PowerShell (`fix-and-run.ps1`) hai ancora problemi 404 o l'import asincrono non funziona, usa la **Pagina Diagnostica** per capire esattamente cosa sta succedendo.

---

## Come Accedere alla Diagnostica

### Step 1: Avvia il Server

```powershell
cd SqlExcelBlazor.Server
dotnet run
```

### Step 2: Apri l'Applicazione

Naviga a: `http://localhost:5264`

### Step 3: Vai alla Tab Diagnostica

Nell'applicazione, click sul tab **🔍 Diagnostica** (ultimo tab, rosso)

---

## Cosa Ti Mostra la Diagnostica

### 1. Configurazione HttpClient

Verifica che:
- ✅ **BaseAddress** non sia vuoto o null
- ✅ **Current URL** contenga porta **5264** o **7146**
- ✅ **Porta Attesa** sia corretta

**Se vedi ❌ qui:**
- **BaseAddress vuoto**: Problema grave configurazione Blazor
- **Porta sbagliata (5000/7233)**: Stai eseguendo progetto client standalone!

### 2. Test Connettività API

Click su "Test Connessione" per:
- Verificare che il server risponda
- Testare endpoint `/api/sqlite/tables`
- Vedere esattamente quale URL viene chiamato

**Risultati Possibili:**

✅ **Success (200 OK)**:
```
Test 1: GET /api/sqlite/tables
Status: 200 OK
Response: {"tables":[...]}

✅ Connessione API funzionante!
```
→ Il server funziona! Se hai ancora 404, il problema è altrove.

❌ **Error (404 Not Found)**:
```
Test 1: GET /api/sqlite/tables
Status: 404 NotFound

❌ API non raggiungibile!

Possibili cause:
1. Server non in esecuzione
2. Porta sbagliata
3. CORS bloccato
```
→ Stai usando porta sbagliata o server non attivo!

❌ **Exception**:
```
❌ ERRORE: Failed to fetch
Type: HttpRequestException
```
→ Server non raggiungibile o problema rete.

### 3. Test Singoli Endpoint

Testa endpoint specifici:
- `GET /api/sqlite/tables`
- `POST /api/sqlserver/test-connection`

Vedi:
- URL completo chiamato
- Status code ricevuto
- Response body

---

## Scenari Comuni e Soluzioni

### Scenario 1: Porta Sbagliata (5000 invece di 5264)

**Diagnostica mostra:**
```
❌ Porta Attesa: 5264 (http) o 7146 (https)
Attualmente sei su: 5000
```

**Problema:** Stai eseguendo `SqlExcelBlazor` (client standalone) invece di `SqlExcelBlazor.Server`

**Soluzione:**
1. Stop tutto (Ctrl+C nei terminali)
2. Chiudi browser
3. Esegui:
   ```powershell
   cd SqlExcelBlazor.Server  # NON SqlExcelBlazor!
   dotnet run
   ```
4. Apri `http://localhost:5264` (NON 5000!)

---

### Scenario 2: BaseAddress è Null/Vuoto

**Diagnostica mostra:**
```
❌ BaseAddress: (null)
```

**Problema:** HttpClient non configurato correttamente

**Soluzione:**
1. Verifica che `SqlExcelBlazor/Program.cs` contenga:
   ```csharp
   builder.Services.AddScoped(sp => 
   {
       var httpClient = new HttpClient { 
           BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) 
       };
       return httpClient;
   });
   ```
2. Rebuild completo:
   ```powershell
   dotnet clean
   dotnet build
   ```

---

### Scenario 3: Test Connessione OK ma Import Excel Fallisce

**Diagnostica mostra:**
```
✅ Connessione API funzionante!
```

Ma import Excel dà 404.

**Problema:** Endpoint specifico Excel potrebbe avere problemi

**Soluzione:**
1. Nella diagnostica, test endpoint: `api/sqlite/excel/upload-temp`
2. Verifica che restituisca 200 o 404
3. Se 404, controller SQLite potrebbe avere problemi

**Debug:**
```powershell
# Verifica che controller esista
ls SqlExcelBlazor.Server/Controllers/SqliteController.cs

# Cerca endpoint upload-temp
grep -n "upload-temp" SqlExcelBlazor.Server/Controllers/SqliteController.cs
```

---

### Scenario 4: CORS Bloccato

**Diagnostica mostra:**
```
Type: HttpRequestException
Message: ... CORS policy ...
```

**Problema:** Browser blocca richieste cross-origin

**Soluzione:**
1. Verifica che `SqlExcelBlazor.Server/Program.cs` contenga:
   ```csharp
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAll",
           builder => builder
           .AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader());
   });
   
   // ...
   
   app.UseCors("AllowAll");
   ```

2. Se già presente, il problema è che stai usando due porte diverse
   - **Soluzione**: Esegui solo SqlExcelBlazor.Server

---

### Scenario 5: Import Asincrono Non Visibile

**Diagnostica mostra tutto OK** ma non vedi progress bar.

**Problema:** Browser cache con vecchi file CSS/JS

**Soluzione 1: Hard Refresh**
1. Apri DevTools (F12)
2. Click destro su refresh → "Empty Cache and Hard Reload"

**Soluzione 2: Incognito**
1. Apri finestra incognito (Ctrl+Shift+N)
2. Vai a `http://localhost:5264`
3. Test import

**Soluzione 3: Verifica CSS**
1. DevTools → Network → Filter "CSS"
2. Cerca `app.css`
3. Click → Response → Cerca `.progress-bar`
4. Se mancano stili → Cache problema

**Fix Definitivo:**
```powershell
# Clear tutto
dotnet clean
Remove-Item -Recurse -Force SqlExcelBlazor/bin, SqlExcelBlazor/obj
Remove-Item -Recurse -Force SqlExcelBlazor.Server/bin, SqlExcelBlazor.Server/obj

# Rebuild
dotnet build

# Avvia server
cd SqlExcelBlazor.Server
dotnet run

# Browser: Ctrl+Shift+R (hard refresh)
```

---

## Endpoint Diagnostica Server

Il server ora ha anche endpoint diagnostica:

### GET /api/diagnostica/ping
```powershell
curl http://localhost:5264/api/diagnostica/ping
```

Risposta:
```json
{
  "message": "pong",
  "timestamp": "2026-02-14T15:00:00Z",
  "server": "SqlExcelBlazor.Server",
  "environment": "Development"
}
```

### GET /api/diagnostica/info
```powershell
curl http://localhost:5264/api/diagnostica/info
```

Mostra:
- Environment del server
- Request headers ricevuti
- Connection info (IP, porta)

### GET /api/diagnostica/test-cors
```powershell
curl http://localhost:5264/api/diagnostica/test-cors
```

Verifica CORS funzionante.

---

## Checklist Debug Completa

Usa la diagnostica per verificare ogni punto:

- [ ] Tab Diagnostica aperta
- [ ] BaseAddress non è null/vuoto
- [ ] Current URL contiene porta 5264 o 7146
- [ ] "Porta Attesa" mostra ✅
- [ ] Test Connessione → ✅ Success
- [ ] Endpoint `/api/sqlite/tables` → 200 OK
- [ ] Endpoint `/api/diagnostica/ping` → 200 OK
- [ ] Browser DevTools → Network → Nessun 404
- [ ] Browser DevTools → Console → Nessun errore rosso

Se TUTTI sono ✅:
- Il setup è corretto
- Se import Excel fallisce ancora, è problema specifico controller
- Se import asincrono non appare, è cache browser

---

## Segnalazione Bug Efficace

Se dopo aver verificato tutto con la diagnostica il problema persiste:

### Screenshot da Inviare

1. **Tab Diagnostica - Configurazione**
   - Mostra BaseAddress, Current URL, Porta

2. **Tab Diagnostica - Test Connettività**
   - Risultato del test (Success o Error)

3. **DevTools Network Tab**
   - Durante tentativo import Excel
   - Mostra richieste con status code

4. **DevTools Console Tab**
   - Eventuali errori JavaScript/Blazor

### Info da Fornire

```
OS: Windows/Linux/Mac
Browser: Chrome 120 / Firefox 115 / Edge 120
.NET Version: 9.0.x
Porta usata: 5264
Server output: [copia output console]
```

---

## Risoluzione Rapida per l'80% dei Casi

Se non hai voglia di investigare:

```powershell
# 1. Stop tutto
# Ctrl+C, chiudi browser

# 2. Clean profondo
dotnet clean
Remove-Item -Recurse -Force */bin, */obj

# 3. Rebuild
dotnet restore
dotnet build

# 4. Avvia SERVER (non client!)
cd SqlExcelBlazor.Server
dotnet run

# 5. Browser INCOGNITO
# Ctrl+Shift+N

# 6. Vai a http://localhost:5264

# 7. Tab Diagnostica
# Verifica tutto ✅

# 8. Test import Excel
```

Se questo non funziona, allora sì che c'è un problema più profondo.

---

**Data:** 2026-02-14  
**Versione:** 1.0  
**Aggiornamento:** Diagnostica integrata nell'app
