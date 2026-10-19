@echo off
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  BRANCH SWITCHER - INIZIALIZZAZIONE REPOSITORY GITHUB
echo ============================================================
echo.

where git >nul 2>nul
if errorlevel 1 (
  echo [ERRORE] Git non trovato nel PATH.
  pause
  exit /b 1
)

if not exist ".git" (
  git init
  if errorlevel 1 goto :error
) else (
  echo [INFO] Repository Git gia inizializzato.
)

git add .
if errorlevel 1 goto :error

git diff --cached --quiet
if errorlevel 1 (
  git commit -m "Initial BranchSwitcher release"
  if errorlevel 1 goto :error
) else (
  echo [INFO] Nessuna modifica da committare.
)

git branch -M main

set /p REMOTE_URL=Inserisci URL del repository GitHub remoto: 
if "%REMOTE_URL%"=="" (
  echo [ERRORE] URL remoto non specificato.
  pause
  exit /b 1
)

git remote get-url origin >nul 2>nul
if errorlevel 1 (
  git remote add origin "%REMOTE_URL%"
) else (
  echo [INFO] Remote origin gia presente. Aggiornamento URL...
  git remote set-url origin "%REMOTE_URL%"
)

if errorlevel 1 goto :error

echo.
echo Push su origin/main...
git push -u origin main
if errorlevel 1 goto :error

echo.
echo [OK] Repository pubblicato correttamente.
echo.
pause
exit /b 0

:error
echo.
echo [ERRORE] Operazione Git fallita. Controlla il messaggio precedente.
echo.
pause
exit /b 1
