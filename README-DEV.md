# BranchSwitcher — Developer Guide

## 1. Scopo del documento

Questo documento è destinato a chi sviluppa, mantiene o distribuisce **BranchSwitcher**.

BranchSwitcher è una applicazione desktop Windows in .NET/WinForms che permette di applicare rapidamente una configurazione di branch Git a uno o più repository locali.

Le configurazioni sono salvate come file `.conf` e possono essere:

- create direttamente dalla GUI;
- eliminate dalla GUI;
- ricercate dalla GUI;
- aggiunte o modificate manualmente;
- condivise e versionate tramite Git.

Il programma esegue operazioni Git conservative:

1. verifica che il working tree sia pulito;
2. esegue `git fetch --prune`;
3. cambia branch;
4. esegue `git pull --ff-only`.

Non vengono eseguiti automaticamente `stash`, `reset`, `clean`, `merge`, `rebase` o `push`.

---

# 2. Requisiti di sviluppo

Per compilare il progetto sono necessari:

- Windows 10/11 x64;
- Git;
- .NET 8 SDK x64;
- opzionalmente Visual Studio 2022 con workload **.NET desktop development**.

Verifica dell'SDK:

```powershell
dotnet --list-sdks
```

Deve essere presente almeno una versione `8.0.x`.

Esempio:

```text
8.0.4xx [C:\Program Files\dotnet\sdk]
```

> Avere installato solamente il .NET Runtime non è sufficiente per compilare il progetto.

---

# 3. Struttura del repository

La struttura indicativa è:

```text
BranchSwitcher/
├── .github/
│   └── workflows/
│       └── build-windows.yml
│
├── BranchSwitcher/
│   ├── Models/
│   ├── Services/
│   ├── Resources/
│   │   └── BranchSwitcher.ico
│   ├── Program.cs
│   ├── MainForm.cs
│   ├── SettingsForm.cs
│   ├── ConfigForm.cs
│   ├── settings.example.json
│   └── BranchSwitcher.csproj
│
├── configs/
│   ├── TASK-101.conf
│   └── TASK-202.conf
│
├── docs/
│   └── images/
│
├── README.md
├── README-DEV.md
├── user-guide.pdf
├── publish.bat
├── create-zip.bat
├── init-github.bat
└── .gitignore
```

---

# 4. Componenti principali

## 4.1 `Program.cs`

È l'entry point dell'applicazione WinForms.

Responsabilità principali:

- inizializzazione dell'applicazione;
- configurazione WinForms;
- apertura della `MainForm`.

---

## 4.2 `MainForm.cs`

È la finestra principale.

Gestisce:

- caricamento delle configurazioni;
- selezione della configurazione;
- ricerca delle configurazioni;
- refresh manuale della lista;
- anteprima repository/branch;
- tooltip del nome completo della configurazione;
- apertura della finestra impostazioni;
- apertura della finestra per creare configurazioni;
- eliminazione delle configurazioni;
- avvio dello switch Git;
- visualizzazione dei log operativi.

Il pulsante principale è:

```text
Avvia switch
```

---

## 4.3 `SettingsForm.cs`

Gestisce l'associazione tra un alias logico e il path locale di un repository.

Esempio:

```text
service-api
→ C:\Workspace\ProjectSuite\Services\service-api
```

Gli alias sono quelli utilizzati nei file `.conf`.

Il vantaggio è che una stessa configurazione può essere condivisa tra sviluppatori che hanno workspace locali differenti.

---

## 4.4 `ConfigForm.cs`

Permette di creare una nuova configurazione direttamente dalla GUI.

L'utente specifica:

- nome della configurazione;
- repository da includere;
- branch richiesto per ogni repository.

Esempio:

```text
TASK-101
```

può generare:

```ini
service-api=feature/TASK-101
shared-utils=develop
```

Dopo il salvataggio:

1. viene creato il file `.conf`;
2. la lista della `MainForm` viene aggiornata;
3. la nuova configurazione viene selezionata automaticamente.

---

# 5. Formato delle configurazioni

Ogni configurazione è un file:

```text
configs/<nome>.conf
```

Esempio:

```text
configs/TASK-101.conf
```

Contenuto:

```ini
service-api=feature/TASK-101
shared-utils=develop
```

Formato:

```text
repositoryAlias=branch
```

Sono ammessi:

- righe vuote;
- commenti che iniziano con `#`.

Esempio:

```ini
# Backend
service-api=feature/TASK-101

# Librerie
shared-utils=develop
```

I repository non presenti nel file non vengono modificati.

---

# 6. Impostazioni locali

Le impostazioni locali permettono di mappare gli alias ai path fisici del computer.

Esempio concettuale:

```json
{
  "Repositories": {
    "service-api": "C:\\Workspace\\ProjectSuite\\Services\\service-api",
    "shared-utils": "C:\\Workspace\\ProjectSuite\\Libraries\\shared-utils"
  }
}
```

Il file reale delle impostazioni personali non dovrebbe essere committato nel repository se contiene path specifici della macchina.

Nel repository viene invece mantenuto:

```text
settings.example.json
```

con valori generici.

---

# 7. Flusso Git

Per ciascun repository configurato viene eseguito il seguente flusso.

## 7.1 Controllo working tree

Lo stato viene verificato prima di qualsiasi cambio branch.

Se sono presenti:

- file modificati;
- file staged;
- file eliminati;
- file untracked;

il repository viene saltato.

Questo evita che il programma sposti involontariamente modifiche locali da un branch a un altro.

---

## 7.2 Fetch

Viene eseguito:

```bash
git fetch --prune
```

Questo:

- aggiorna i riferimenti remoti;
- recupera i branch remoti disponibili;
- elimina i riferimenti locali a branch remoti non più esistenti.

---

## 7.3 Switch

Se il branch esiste localmente:

```bash
git switch <branch>
```

Se esiste solamente su `origin`, viene creato il tracking locale.

Esempio concettuale:

```bash
git switch --track origin/feature/TASK-101
```

---

## 7.4 Pull

Al termine viene eseguito:

```bash
git pull --ff-only
```

L'opzione `--ff-only` impedisce la creazione automatica di merge commit.

Se il branch locale e quello remoto sono divergenti, l'operazione fallisce e viene richiesto un intervento manuale.

---

# 8. Build locale

Dalla root del repository:

```powershell
dotnet restore .\BranchSwitcher\BranchSwitcher.csproj
```

Poi:

```powershell
dotnet build .\BranchSwitcher\BranchSwitcher.csproj -c Release
```

---

# 9. Publish Windows

Il progetto viene distribuito come applicazione Windows x64 self-contained.

Comando equivalente:

```powershell
dotnet publish .\BranchSwitcher\BranchSwitcher.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -o .\publish
```

L'output principale deve essere:

```text
publish/
└── BranchSwitcher.exe
```

Essendo self-contained, sul PC dell'utilizzatore non è necessario installare il .NET Runtime.

Git deve invece essere disponibile perché BranchSwitcher richiama l'eseguibile `git`.

---

# 10. `publish.bat`

Il file:

```text
publish.bat
```

automatizza il publish locale.

Il risultato atteso è simile a:

```text
publish/
├── BranchSwitcher.exe
├── configs/
├── settings.example.json
└── user-guide.pdf
```

La guida utente viene copiata automaticamente nel pacchetto di distribuzione.

`README-DEV.md` rimane invece nel repository sorgente e non è necessario distribuirlo agli utenti finali.

---

# 11. `create-zip.bat`

`create-zip.bat`:

1. esegue il publish;
2. verifica l'output;
3. comprime il contenuto della cartella `publish`;
4. genera uno ZIP distribuibile.

Esempio:

```text
BranchSwitcher-Windows-x64.zip
```

---

# 12. GitHub Actions

BranchSwitcher può essere compilato automaticamente da GitHub Actions.

Il workflow consigliato deve utilizzare un runner Windows, perché il progetto è WinForms e targetta:

```text
net8.0-windows
```

Esempio:

```yaml
runs-on: windows-latest
```

Non utilizzare `ubuntu-latest` per il workflow principale di publish dell'applicazione Windows.

---

# 13. Artifact e Release: differenza

È importante distinguere due concetti.

## 13.1 Artifact

Un artifact viene prodotto da un singolo run GitHub Actions.

Percorso tipico:

```text
GitHub
→ Actions
→ Workflow
→ Run
→ Artifacts
```

È utile per:

- test;
- build intermedie;
- download da parte degli sviluppatori;
- verificare che la pipeline funzioni.

Esempio:

```text
BranchSwitcher-Windows-x64
```

che contiene:

```text
BranchSwitcher-Windows-x64.zip
```

L'artifact è legato al workflow run e non rappresenta formalmente una versione pubblicata del software.

## 13.2 GitHub Release

Una Release è invece una versione pubblicata del prodotto.

Percorso:

```text
GitHub
→ Releases
```

Esempio:

```text
v2.2.1
```

con asset:

```text
BranchSwitcher-Windows-x64.zip
```

Per distribuire BranchSwitcher agli utenti, le Release sono preferibili agli artifact dei workflow.

---

# 14. Release automatica tramite tag Git

La modalità consigliata è:

```text
normale push
→ build automatica

push di un tag v*
→ build + publish + GitHub Release
```

Esempio di tag:

```text
v2.2.1
v2.2.2
v2.3.0
v3.0.0
```

---

# 15. Workflow Build + Release consigliato

File:

```text
.github/workflows/build-windows.yml
```

Contenuto:

```yaml
name: Build and Release BranchSwitcher

on:
  push:
    branches:
      - master
    tags:
      - 'v*'

  pull_request:
    branches:
      - master

  workflow_dispatch:

permissions:
  contents: write

jobs:

  build-windows:
    runs-on: windows-latest

    steps:

      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore .\BranchSwitcher\BranchSwitcher.csproj

      - name: Build
        run: >
          dotnet build
          .\BranchSwitcher\BranchSwitcher.csproj
          -c Release
          --no-restore

      - name: Publish Windows x64
        run: >
          dotnet publish
          .\BranchSwitcher\BranchSwitcher.csproj
          -c Release
          -r win-x64
          --self-contained true
          -p:PublishSingleFile=true
          -p:IncludeNativeLibrariesForSelfExtract=true
          -p:DebugType=None
          -o .\publish

      - name: Copy configs
        shell: pwsh
        run: |
          New-Item -ItemType Directory -Force -Path ".\publish\configs"

          if (Test-Path ".\configs") {
              Copy-Item ".\configs\*.conf" ".\publish\configs\" -Force
          }

      - name: Copy user guide
        shell: pwsh
        run: |
          if (Test-Path ".\user-guide.pdf") {
              Copy-Item ".\user-guide.pdf" ".\publish\user-guide.pdf" -Force
          }

      - name: Copy example settings
        shell: pwsh
        run: |
          if (Test-Path ".\BranchSwitcher\settings.example.json") {
              Copy-Item `
                ".\BranchSwitcher\settings.example.json" `
                ".\publish\settings.example.json" `
                -Force
          }

      - name: Verify executable
        shell: pwsh
        run: |
          if (!(Test-Path ".\publish\BranchSwitcher.exe")) {
              throw "BranchSwitcher.exe non è stato generato."
          }

      - name: Create distribution ZIP
        shell: pwsh
        run: |
          Compress-Archive `
            -Path ".\publish\*" `
            -DestinationPath ".\BranchSwitcher-Windows-x64.zip" `
            -Force

      - name: Upload workflow artifact
        uses: actions/upload-artifact@v4
        with:
          name: BranchSwitcher-Windows-x64
          path: BranchSwitcher-Windows-x64.zip
          if-no-files-found: error

      - name: Create GitHub Release
        if: startsWith(github.ref, 'refs/tags/v')
        shell: pwsh
        env:
          GH_TOKEN: ${{ github.token }}
        run: |
          gh release create "${{ github.ref_name }}" `
            ".\BranchSwitcher-Windows-x64.zip" `
            --title "BranchSwitcher ${{ github.ref_name }}" `
            --generate-notes
```

---

# 16. Come funziona il workflow

## Push normale

Esempio:

```bash
git push
```

Il workflow:

```text
checkout
   ↓
setup .NET
   ↓
restore
   ↓
build
   ↓
publish
   ↓
creazione ZIP
   ↓
upload artifact
```

Non viene creata nessuna GitHub Release.

---

## Push di un tag

Esempio:

```bash
git tag v2.2.1
git push origin v2.2.1
```

Il trigger:

```yaml
tags:
  - 'v*'
```

riconosce il tag.

Il workflow esegue quindi:

```text
checkout
   ↓
restore
   ↓
build
   ↓
publish
   ↓
BranchSwitcher.exe
   ↓
ZIP distribuzione
   ↓
artifact GitHub Actions
   ↓
GitHub Release v2.2.1
   ↓
asset BranchSwitcher-Windows-x64.zip
```

---

# 17. Perché viene usato `permissions: contents: write`

Per creare una GitHub Release, il token automatico del workflow deve poter scrivere nei contenuti del repository.

Per questo il workflow dichiara:

```yaml
permissions:
  contents: write
```

Senza questa autorizzazione, lo step di creazione della Release può fallire con errori relativi ai permessi del `GITHUB_TOKEN`.

Se l'organizzazione o il repository applicano policy più restrittive, controllare anche:

```text
Repository
→ Settings
→ Actions
→ General
→ Workflow permissions
```

---

# 18. Uso di `GITHUB_TOKEN`

Nel workflow viene utilizzato:

```yaml
env:
  GH_TOKEN: ${{ github.token }}
```

`github.token` è il token automatico messo a disposizione da GitHub Actions per il run corrente.

Non è quindi necessario salvare manualmente username/password GitHub all'interno del repository.

Il token eredita i permessi dichiarati nel workflow.

---

# 19. Creazione della Release tramite GitHub CLI

Lo step:

```powershell
gh release create "${{ github.ref_name }}" `
  ".\BranchSwitcher-Windows-x64.zip" `
  --title "BranchSwitcher ${{ github.ref_name }}" `
  --generate-notes
```

fa tre cose.

## 19.1 Nome Release

Se il tag è:

```text
v2.2.1
```

la Release viene chiamata:

```text
BranchSwitcher v2.2.1
```

## 19.2 Asset

Viene allegato:

```text
BranchSwitcher-Windows-x64.zip
```

## 19.3 Release notes

L'opzione:

```text
--generate-notes
```

chiede a GitHub di generare automaticamente le note della Release.

---

# 20. Versionamento consigliato

Usare semantic versioning:

```text
MAJOR.MINOR.PATCH
```

Esempio:

```text
2.2.1
```

## PATCH

Correzioni senza nuove funzionalità:

```text
2.2.1
→ 2.2.2
```

## MINOR

Nuove funzionalità compatibili:

```text
2.2.2
→ 2.3.0
```

## MAJOR

Modifiche importanti o incompatibili:

```text
2.3.0
→ 3.0.0
```

I tag Git dovrebbero avere prefisso `v`:

```text
v2.2.1
v2.3.0
v3.0.0
```

---

# 21. Procedura completa per pubblicare una nuova versione

Esempio: pubblicazione `v2.3.0`.

## 21.1 Modifica codice

Effettuare le modifiche necessarie.

## 21.2 Aggiornamento versione applicativa

Aggiornare la versione nel progetto se prevista nel `.csproj`.

Esempio:

```xml
<Version>2.3.0</Version>
<AssemblyVersion>2.3.0.0</AssemblyVersion>
<FileVersion>2.3.0.0</FileVersion>
```

## 21.3 Commit

```bash
git add .
git commit -m "Release 2.3.0"
```

## 21.4 Push del codice

```bash
git push
```

Questo avvia una build normale.

È consigliabile verificare che la build sia verde prima di creare il tag.

## 21.5 Creazione tag

```bash
git tag v2.3.0
```

## 21.6 Push del tag

```bash
git push origin v2.3.0
```

A questo punto GitHub Actions crea automaticamente la Release.

## 21.7 Verifica

Controllare:

```text
GitHub
→ Actions
```

e successivamente:

```text
GitHub
→ Releases
→ v2.3.0
```

Dovrebbe essere disponibile:

```text
BranchSwitcher-Windows-x64.zip
```

---

# 22. Tag annotati

È possibile anche creare un tag annotato:

```bash
git tag -a v2.3.0 -m "BranchSwitcher 2.3.0"
git push origin v2.3.0
```

Per BranchSwitcher entrambe le modalità sono valide.

---

# 23. Cancellare un tag creato per errore

## Locale

```bash
git tag -d v2.3.0
```

## Remoto

```bash
git push origin --delete v2.3.0
```

Se è già stata creata una GitHub Release, eliminare anche la Release dalla relativa pagina GitHub prima di ricrearla.

---

# 24. Eseguire manualmente il workflow

Il workflow contiene:

```yaml
workflow_dispatch:
```

Questo consente l'avvio manuale da:

```text
GitHub
→ Actions
→ Build and Release BranchSwitcher
→ Run workflow
```

Un avvio manuale normale genera build e artifact.

Non genera una Release, perché non è associato a un ref del tipo:

```text
refs/tags/v...
```

---

# 25. Dove scaricare una build normale

Per una build generata da push normale:

```text
GitHub
→ Actions
→ ultimo workflow riuscito
→ Artifacts
→ BranchSwitcher-Windows-x64
```

Scaricare lo ZIP dell'artifact.

---

# 26. Dove scaricare una Release

Per una versione ufficialmente pubblicata:

```text
GitHub
→ Releases
→ versione desiderata
→ Assets
```

Scaricare:

```text
BranchSwitcher-Windows-x64.zip
```

Questa è la modalità consigliata per la distribuzione agli utenti.

---

# 27. Troubleshooting GitHub Actions

## 27.1 Build su Ubuntu

Sintomo:

```text
WinForms / net8.0-windows build failure
```

Verificare che il workflow utilizzi:

```yaml
runs-on: windows-latest
```

---

## 27.2 `.csproj` non trovato

Usare il path completo:

```text
.\BranchSwitcher\BranchSwitcher.csproj
```

---

## 27.3 Release non creata

Verificare che il workflow sia partito da un tag:

```text
v2.2.1
```

e non da un semplice push.

Verificare inoltre:

```yaml
permissions:
  contents: write
```

---

## 27.4 Errore permessi

Controllare:

```text
Repository
→ Settings
→ Actions
→ General
→ Workflow permissions
```

Le policy dell'organizzazione possono limitare i permessi disponibili anche se il workflow dichiara `contents: write`.

---

## 27.5 Artifact presente ma Release assente

Significa normalmente che il workflow è stato eseguito da:

- push su branch;
- pull request;
- avvio manuale.

La Release viene creata solamente quando:

```text
github.ref
```

inizia con:

```text
refs/tags/v
```

---

## 27.6 Tag già esistente

Controllare:

```bash
git tag
```

e:

```bash
git ls-remote --tags origin
```

Se necessario eliminare il tag errato e ricrearlo.

---

# 28. Icona applicazione

L'icona si trova in:

```text
BranchSwitcher/Resources/BranchSwitcher.ico
```

Nel `.csproj` è configurata tramite:

```xml
<ApplicationIcon>Resources\BranchSwitcher.ico</ApplicationIcon>
```

In questo modo viene incorporata nell'EXE pubblicato.

---

# 29. Documentazione distribuita

Nel repository vengono mantenuti:

```text
README.md
README-DEV.md
user-guide.pdf
```

## `README.md`

Documentazione generale del repository GitHub.

## `README-DEV.md`

Documentazione tecnica per sviluppatori.

## `user-guide.pdf`

Guida destinata agli utenti finali.

Durante il publish, `user-guide.pdf` viene incluso nel pacchetto distribuibile.

---

# 30. Checklist prima di una Release

Prima di creare il tag:

- [ ] build locale riuscita;
- [ ] working tree Git pulito;
- [ ] versione applicativa aggiornata;
- [ ] README aggiornati se necessario;
- [ ] `user-guide.pdf` aggiornato se necessario;
- [ ] config di esempio generiche;
- [ ] nessun path personale presente;
- [ ] nessun dato aziendale o sensibile presente;
- [ ] push del codice completato;
- [ ] build GitHub Actions normale riuscita;
- [ ] tag coerente con la versione applicativa.

Poi:

```bash
git tag vX.Y.Z
git push origin vX.Y.Z
```

---

# 31. Flusso di rilascio riassuntivo

```text
MODIFICA CODICE
      │
      ▼
COMMIT
      │
      ▼
PUSH
      │
      ▼
GITHUB ACTIONS BUILD
      │
      ▼
BUILD OK?
      │
      ├── NO → correggere
      │
      ▼
     SI
      │
      ▼
git tag vX.Y.Z
      │
      ▼
git push origin vX.Y.Z
      │
      ▼
GITHUB ACTIONS
      │
      ├── build
      ├── publish
      ├── crea ZIP
      ├── artifact
      └── GitHub Release
              │
              ▼
      BranchSwitcher-Windows-x64.zip
```

---

# 32. Regola consigliata

Utilizzare:

```text
Artifacts
```

per le build di sviluppo e verifica.

Utilizzare:

```text
GitHub Releases
```

per le versioni ufficialmente distribuibili.

In questo modo il repository mantiene una separazione chiara tra build temporanee e versioni rilasciate.
