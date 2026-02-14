#!/bin/bash

# Script per risolvere problemi 404 e import asincrono
# Usage: ./fix-and-run.sh

set -e

echo "======================================"
echo "Fix e Avvio App SQL Studio"
echo "======================================"
echo ""

# Verifica che siamo nella root del progetto
if [ ! -f "SqlExcelApp.sln" ]; then
    echo "❌ Errore: Esegui questo script dalla root del progetto!"
    exit 1
fi

echo "✅ Progetto trovato"
echo ""

# Step 1: Clean
echo "🧹 Step 1/5: Clean progetto..."
dotnet clean > /dev/null 2>&1
echo "✅ Clean completato"
echo ""

# Step 2: Remove bin/obj
echo "🗑️  Step 2/5: Rimozione bin/obj folders..."
rm -rf SqlExcelBlazor.Server/bin 2>/dev/null || true
rm -rf SqlExcelBlazor.Server/obj 2>/dev/null || true
rm -rf SqlExcelBlazor/bin 2>/dev/null || true
rm -rf SqlExcelBlazor/obj 2>/dev/null || true
echo "✅ Folders rimossi"
echo ""

# Step 3: Restore
echo "📦 Step 3/5: Restore dependencies..."
dotnet restore > /dev/null 2>&1
echo "✅ Restore completato"
echo ""

# Step 4: Build
echo "🔨 Step 4/5: Build progetto..."
dotnet build > build.log 2>&1
if [ $? -eq 0 ]; then
    echo "✅ Build completato con successo"
    # Mostra solo warnings/errors summary
    grep -E "(Warning\(s\)|Error\(s\))" build.log | tail -2
else
    echo "❌ Build fallito! Controlla build.log per dettagli"
    cat build.log
    exit 1
fi
echo ""

# Step 5: Info di esecuzione
echo "🚀 Step 5/5: Pronto per l'esecuzione"
echo ""
echo "======================================"
echo "✅ Setup completato!"
echo "======================================"
echo ""
echo "Per avviare l'applicazione:"
echo ""
echo "  cd SqlExcelBlazor.Server"
echo "  dotnet run"
echo ""
echo "Poi apri il browser su:"
echo "  http://localhost:5264"
echo ""
echo "======================================"
echo "📝 Note Importanti:"
echo "======================================"
echo ""
echo "1. ❌ NON eseguire 'dotnet run' in SqlExcelBlazor"
echo "2. ✅ Esegui SOLO in SqlExcelBlazor.Server"
echo "3. 🔄 Hard refresh browser: Ctrl+Shift+R"
echo "4. 🔍 Apri DevTools (F12) per debug"
echo ""
echo "Per test import asincrono:"
echo "  - Import Excel → Dialog chiude subito"
echo "  - Vedi icona ⏳ che ruota"
echo "  - Progress bar animata"
echo "  - Messaggi: 'Caricamento dati... (60%)'"
echo ""
echo "======================================"

# Chiedi se vuole avviare subito
echo ""
read -p "Vuoi avviare il server ora? (y/n) " -n 1 -r
echo ""
if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo ""
    echo "🚀 Avvio server..."
    echo ""
    cd SqlExcelBlazor.Server
    dotnet run
fi
