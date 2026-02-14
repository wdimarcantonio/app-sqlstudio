# Script per risolvere problemi 404 e import asincrono
# Usage: .\fix-and-run.ps1

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Fix e Avvio App SQL Studio" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Verifica che siamo nella root del progetto
if (-not (Test-Path "SqlExcelApp.sln")) {
    Write-Host "❌ Errore: Esegui questo script dalla root del progetto!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Progetto trovato" -ForegroundColor Green
Write-Host ""

# Step 1: Clean
Write-Host "🧹 Step 1/5: Clean progetto..." -ForegroundColor Yellow
dotnet clean | Out-Null
Write-Host "✅ Clean completato" -ForegroundColor Green
Write-Host ""

# Step 2: Remove bin/obj
Write-Host "🗑️  Step 2/5: Rimozione bin/obj folders..." -ForegroundColor Yellow
Remove-Item -Path "SqlExcelBlazor.Server\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "SqlExcelBlazor.Server\obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "SqlExcelBlazor\bin" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "SqlExcelBlazor\obj" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "✅ Folders rimossi" -ForegroundColor Green
Write-Host ""

# Step 3: Restore
Write-Host "📦 Step 3/5: Restore dependencies..." -ForegroundColor Yellow
dotnet restore | Out-Null
Write-Host "✅ Restore completato" -ForegroundColor Green
Write-Host ""

# Step 4: Build
Write-Host "🔨 Step 4/5: Build progetto..." -ForegroundColor Yellow
$buildOutput = dotnet build 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build completato con successo" -ForegroundColor Green
    # Mostra solo warnings/errors summary
    $buildOutput | Select-String -Pattern "(Warning\(s\)|Error\(s\))" | Select-Object -Last 2
} else {
    Write-Host "❌ Build fallito!" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}
Write-Host ""

# Step 5: Info di esecuzione
Write-Host "🚀 Step 5/5: Pronto per l'esecuzione" -ForegroundColor Yellow
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "✅ Setup completato!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Per avviare l'applicazione:" -ForegroundColor White
Write-Host ""
Write-Host "  cd SqlExcelBlazor.Server" -ForegroundColor Yellow
Write-Host "  dotnet run" -ForegroundColor Yellow
Write-Host ""
Write-Host "Poi apri il browser su:" -ForegroundColor White
Write-Host "  http://localhost:5264" -ForegroundColor Cyan
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "📝 Note Importanti:" -ForegroundColor White
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. ❌ NON eseguire 'dotnet run' in SqlExcelBlazor" -ForegroundColor Red
Write-Host "2. ✅ Esegui SOLO in SqlExcelBlazor.Server" -ForegroundColor Green
Write-Host "3. 🔄 Hard refresh browser: Ctrl+Shift+R" -ForegroundColor Yellow
Write-Host "4. 🔍 Apri DevTools (F12) per debug" -ForegroundColor Yellow
Write-Host ""
Write-Host "Per test import asincrono:" -ForegroundColor White
Write-Host "  - Import Excel → Dialog chiude subito" -ForegroundColor Gray
Write-Host "  - Vedi icona ⏳ che ruota" -ForegroundColor Gray
Write-Host "  - Progress bar animata" -ForegroundColor Gray
Write-Host "  - Messaggi: 'Caricamento dati... (60%)'" -ForegroundColor Gray
Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan

# Chiedi se vuole avviare subito
Write-Host ""
$response = Read-Host "Vuoi avviare il server ora? (y/n)"
if ($response -eq "y" -or $response -eq "Y") {
    Write-Host ""
    Write-Host "🚀 Avvio server..." -ForegroundColor Green
    Write-Host ""
    Set-Location SqlExcelBlazor.Server
    dotnet run
}
