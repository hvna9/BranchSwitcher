@echo off
setlocal
cd /d "%~dp0"

call publish.bat
if errorlevel 1 exit /b 1

if exist "BranchSwitcher-Windows-x64.zip" del /q "BranchSwitcher-Windows-x64.zip"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Compress-Archive -Path 'publish\*' -DestinationPath 'BranchSwitcher-Windows-x64.zip' -Force"

if errorlevel 1 (
  echo.
  echo [ERRORE] Creazione ZIP fallita.
  pause
  exit /b 1
)

if not exist "BranchSwitcher-Windows-x64.zip" (
  echo.
  echo [ERRORE] ZIP non generato.
  pause
  exit /b 1
)

echo.
echo ============================================================
echo  PACCHETTO DISTRIBUIBILE CREATO
echo ============================================================
echo.
echo     %CD%\BranchSwitcher-Windows-x64.zip
echo.
echo Il file contiene l'output di publish, incluso user-guide.pdf.
echo.
pause
