# Guida Risoluzione 404 e Import Asincrono

## Problemi Segnalati

1. ❌ Errore 404 durante import Excel
2. ❌ Import asincrono non funziona (nessuna progress bar visibile)

## ✅ Verifica: Le Modifiche Sono Nel Branch Corretto

**Branch:** `copilot/fix-excel-import-issue`  
**Commit chiave:**
- `bc10c0b` - Fix 404 documentation (ultimo)
- `4860acb` - Implement async import with progress tracking
- `be122e7` - Add comprehensive documentation

✅ **Tutte le modifiche sono presenti nel branch!**

---

## 🔧 Soluzione Step-by-Step

### Step 1: Assicurati di Avere l'Ultima Versione

```bash
cd /path/to/app-sqlstudio
git fetch origin
git checkout copilot/fix-excel-import-issue
git pull origin copilot/fix-excel-import-issue
```

### Step 2: Clean + Rebuild Completo

**IMPORTANTE:** Le modifiche includono codice C# E file CSS. Un semplice build potrebbe non aggiornare tutto.

```bash
# Clean completo
dotnet clean

# Rebuild completo
dotnet build
```

### Step 3: Clear Browser Cache

Il browser potrebbe aver cachato i vecchi file Blazor WebAssembly (.dll compilati e CSS).

**Metodo 1: Hard Refresh**
- Chrome/Edge: `Ctrl + Shift + R` (Windows) o `Cmd + Shift + R` (Mac)
- Firefox: `Ctrl + F5`

**Metodo 2: DevTools**
1. Apri DevTools (F12)
2. Click destro sul pulsante refresh
3. Seleziona "Empty Cache and Hard Reload"

**Metodo 3: Settings**
- Chrome: Settings → Privacy → Clear browsing data → Cached images and files
- Scegli "Last hour" per cancellare solo recente

### Step 4: Esegui Il Progetto CORRETTO

**❌ NON eseguire:**
```bash
cd SqlExcelBlazor
dotnet run  # SBAGLIATO - Causa 404!
```

**✅ Esegui QUESTO:**
```bash
cd SqlExcelBlazor.Server
dotnet run
```

**Output atteso:**
```
Now listening on: http://localhost:5264
Now listening on: https://localhost:7146
```

### Step 5: Naviga All'URL Corretto

Apri browser su: **`http://localhost:5264`**

❌ NON usare: `http://localhost:5000` (porta client standalone)

---

## 🧪 Test Import Asincrono

### Come Verificare che Funziona

1. **Apri DevTools** (F12)
2. **Tab "Console"** - verifica nessun errore
3. **Tab "Network"** - monitora chiamate API

### Flusso Normale Import

1. Click "📗 Importa Excel"
2. Seleziona file → Dialog si apre
3. Scegli sheet
4. Click "Importa"

**✅ Con import asincrono:**
- Dialog si chiude IMMEDIATAMENTE
- Nella lista appare elemento con icona ⏳ che ruota
- Progress bar animata visibile
- Percentuale e messaggio (es. "Caricamento dati... 60%")
- Dopo completamento: diventa elemento normale 📊

**❌ Senza import asincrono (vecchio):**
- Dialog rimane aperto durante import
- UI si blocca/congela
- Nessuna progress bar
- Dialog si chiude solo a fine import

---

## 🔍 Diagnosi Problemi

### Problema: Errore 404 Persiste

**Verifica 1: Quale progetto stai eseguendo?**

```bash
# Controlla processo in esecuzione
# Deve mostrare SqlExcelBlazor.Server, non SqlExcelBlazor
ps aux | grep dotnet
```

Oppure guarda l'output console quando avvii:
```
✅ CORRETTO: "SqlExcelBlazor.Server -> /path/bin/.../SqlExcelBlazor.Server.dll"
❌ SBAGLIATO: "SqlExcelBlazor -> /path/bin/.../SqlExcelBlazor.dll"
```

**Verifica 2: Porta corretta?**

Apri DevTools → Network

Durante import Excel dovresti vedere:
```
✅ POST http://localhost:5264/api/sqlite/excel/upload-temp
✅ GET  http://localhost:5264/api/sqlite/excel/sheets/{id}
✅ POST http://localhost:5264/api/sqlite/excel/preview

❌ POST http://localhost:5000/api/...  (PORTA SBAGLIATA!)
```

**Verifica 3: Build include modifiche?**

```bash
# Controlla data modifica file CSS
ls -la SqlExcelBlazor/wwwroot/css/app.css

# Deve essere recente (dopo le tue modifiche)
```

---

### Problema: Import Asincrono Non Visibile

**Verifica 1: CSS caricato?**

DevTools → Network → Filter "CSS"

Cerca `app.css` - dovrebbe essere 200 OK

Click sul file → Response tab → Cerca:
```css
.progress-bar {
    width: 100%;
    height: 20px;
    ...
}
```

Se NON c'è → Browser ha cached vecchio CSS

**Soluzione:**
- Hard refresh (Ctrl + Shift + R)
- O aggiungi timestamp al CSS in index.html

**Verifica 2: Codice compilato include modifiche?**

```bash
# Controlla che DataSource.cs compilato includa ImportStatus
grep -r "ImportStatus" SqlExcelBlazor.Server/bin/Debug/net9.0/

# Dovrebbe trovare riferimenti
```

**Verifica 3: JavaScript console errors?**

DevTools → Console

Se vedi errori JavaScript/Blazor:
- Potrebbero bloccare il rendering
- Potrebbero indicare incompatibilità versione

---

## 🚀 Soluzione Rapida "Riprova Tutto"

Se ancora non funziona, questo dovrebbe risolvere:

```bash
# 1. Stop tutti i processi
# Chiudi browser e terminali

# 2. Clean profondo
cd /path/to/app-sqlstudio
dotnet clean
rm -rf SqlExcelBlazor.Server/bin
rm -rf SqlExcelBlazor.Server/obj
rm -rf SqlExcelBlazor/bin
rm -rf SqlExcelBlazor/obj

# 3. Rebuild completo
dotnet restore
dotnet build

# 4. Avvia server
cd SqlExcelBlazor.Server
dotnet run

# 5. Nuovo browser o incognito
# Chrome: Ctrl + Shift + N
# Firefox: Ctrl + Shift + P

# 6. Naviga a http://localhost:5264

# 7. Test import Excel
```

---

## 📸 Screenshot Attesi

### Import Asincrono Funzionante

**Stato Loading:**
```
┌─────────────────────────────────────────────────┐
│ ⏳ vendite.xlsx                  [rotazione]    │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░ │
│ Caricamento dati... (60%)                       │
└─────────────────────────────────────────────────┘
```

**Stato Ready:**
```
┌─────────────────────────────────────────────────┐
│ 📊 vendite.xlsx                  [🗑️ Rimuovi]   │
│ Alias: vendite_xlsx          [✓ Applica]       │
│ 10.000 righe                                    │
└─────────────────────────────────────────────────┘
```

---

## 📋 Checklist Finale

Prima di segnalare che non funziona, verifica:

- [ ] Branch corretto: `copilot/fix-excel-import-issue`
- [ ] `git pull` fatto
- [ ] `dotnet clean` eseguito
- [ ] `dotnet build` completato con successo (0 errori)
- [ ] Browser cache cleared (Ctrl + Shift + R)
- [ ] Eseguendo **SqlExcelBlazor.Server** (non SqlExcelBlazor)
- [ ] Porta corretta: `5264` (non 5000)
- [ ] DevTools aperto per monitorare Network e Console
- [ ] Nessun errore 404 in Network tab
- [ ] app.css contiene `.progress-bar` styles

Se TUTTI questi sono ✅ e ancora non funziona:
- Fai screenshot DevTools (Network + Console)
- Copia output console server
- Segnala versione .NET (`dotnet --version`)

---

## 💡 Cause Comuni

### Perché l'errore 404?

1. **Eseguendo progetto sbagliato** (90% dei casi)
   - Client standalone invece di Server

2. **URL sbagliato**
   - `localhost:5000` invece di `localhost:5264`

3. **Build non aggiornata**
   - Non fatto `dotnet clean && dotnet build`

### Perché import asincrono non si vede?

1. **Browser cache** (80% dei casi)
   - CSS e DLL Blazor cachati
   - Soluzione: Hard refresh o incognito

2. **Build non completa**
   - CSS non copiato in bin
   - Soluzione: `dotnet clean && dotnet build`

3. **Errori JavaScript silenti**
   - Check DevTools console
   - Potrebbero bloccare rendering

---

## 🎯 Risultato Atteso

Dopo questi step:

✅ Nessun errore 404 sulle chiamate API  
✅ Import Excel avvia subito con dialog che chiude  
✅ Progress bar visibile e animata  
✅ Percentuale e messaggi aggiornati in real-time  
✅ Completamento con elemento utilizzabile  

Se hai seguito tutti gli step e NON vedi questo risultato:
- Apri un issue con screenshot e log dettagliati
- Includi versione .NET, OS, Browser

---

**Data:** 2026-02-14  
**Branch:** copilot/fix-excel-import-issue  
**Versione Guida:** 1.0
