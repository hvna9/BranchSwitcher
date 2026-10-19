@echo off
setlocal
cd /d "%~dp0"

echo.
echo ============================================================
echo  BRANCH SWITCHER - PUBLISH WINDOWS x64
echo ============================================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [ERRORE] .NET SDK non trovato.
  echo Installa .NET 8 SDK e riprova.
  echo.
  pause
  exit /b 1
)

echo [OK] dotnet trovato:
for /f "delims=" %%v in ('dotnet --version') do echo     %%v
echo.

if not exist "user-guide.pdf" (
  echo [ERRORE] File user-guide.pdf non trovato nella root del progetto.
  echo Il PDF e parte obbligatoria del pacchetto distribuibile.
  echo.
  pause
  exit /b 1
)

if exist publish (
  echo Pulizia cartella publish precedente...
  rmdir /s /q publish
)

mkdir publish

echo.
echo Compilazione applicazione...
echo.

dotnet publish BranchSwitcher\BranchSwitcher.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:DebugType=None ^
  -o "%CD%\publish"

if errorlevel 1 (
  echo.
  echo [ERRORE] Build fallita.
  pause
  exit /b 1
)

if not exist "publish\BranchSwitcher.exe" (
  echo.
  echo [ERRORE] Il publish non ha generato publish\BranchSwitcher.exe
  echo.
  dir /b publish
  pause
  exit /b 1
)

echo.
echo Copia file di distribuzione...

if not exist "publish\configs" mkdir "publish\configs"
if exist "configs\*.conf" copy /Y "configs\*.conf" "publish\configs\" >nul
if exist "BranchSwitcher\settings.example.json" copy /Y "BranchSwitcher\settings.example.json" "publish\settings.example.json" >nul
copy /Y "user-guide.pdf" "publish\user-guide.pdf" >nul

if not exist "publish\user-guide.pdf" (
  echo [ERRORE] Impossibile copiare user-guide.pdf nella cartella publish.
  pause
  exit /b 1
)

echo.
echo ============================================================
echo  BUILD COMPLETATA
echo ============================================================
echo.
echo Cartella pronta per la distribuzione:
echo     %CD%\publish
echo.
echo Contenuto:
dir /b publish
echo.
pause
