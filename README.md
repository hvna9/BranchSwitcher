# BranchSwitcher 

<p align="center">
  <img src="docs/images/app-icon.png" width="150" alt="BranchSwitcher icon">
</p>

**BranchSwitcher** è una utility Windows con interfaccia grafica che permette di cambiare rapidamente branch su più repository Git usando configurazioni riutilizzabili.

È pensata per progetti composti da più servizi, librerie o repository correlati, quando è necessario passare spesso da una lavorazione all'altra senza eseguire manualmente `fetch`, `switch` e `pull` su ogni repository.

## Funzionalità principali

- interfaccia grafica Windows avviabile con doppio click;
- associazione libera tra alias repository e path locali;
- creazione delle configurazioni direttamente dalla GUI;
- compatibilità con file `.conf` creati o modificati manualmente;
- ricerca delle configurazioni;
- eliminazione delle configurazioni;
- aggiornamento manuale della lista;
- tooltip per leggere per intero nomi di configurazione lunghi;
- controllo preventivo delle modifiche locali;
- `git fetch --prune` prima dello switch;
- creazione automatica del branch locale se esiste solo su `origin`;
- `git pull --ff-only` per evitare merge automatici indesiderati;
- log dettagliato delle operazioni;
- pubblicazione Windows x64 self-contained;
- icona applicativa incorporata nell'EXE.

## Come funziona

Ogni utente configura una sola volta i repository presenti sul proprio computer, associando un alias a un path locale. Gli alias vengono poi usati nelle configurazioni di branch.

Esempio di mapping locale:

```text
service-api     -> C:\Workspace\ProjectSuite\Services\service-api
shared-utils    -> C:\Workspace\ProjectSuite\Libraries\shared-utils
data-parser     -> D:\Git\ProjectSuite\Libraries\data-parser
```

I path possono essere diversi da utente a utente: le configurazioni restano comunque condivisibili perché contengono solo gli alias.

## Avvio rapido

1. Scarica o ricevi il pacchetto di distribuzione.
2. Estrai lo ZIP in una cartella locale.
3. Avvia `BranchSwitcher.exe` con doppio click.
4. Apri **Impostazioni**.
5. Configura alias e path dei repository.
6. Seleziona una configurazione esistente oppure creane una nuova.
7. Premi **Avvia switch**.

Per la guida completa vedere [`user-guide.pdf`](user-guide.pdf).

## Esempi di configurazione

Una configurazione può contenere un solo repository:

```ini
service-api=feature/TASK-101
```

Oppure più repository e branch differenti:

```ini
service-api=feature/TASK-202
shared-utils=develop
data-parser=feature/TASK-202
```

BranchSwitcher modifica **solo i repository presenti nella configurazione selezionata**. Tutti gli altri restano invariati.

## Flusso Git

Per ogni repository BranchSwitcher esegue concettualmente:

```text
controllo working tree
        |
        v
git fetch --prune
        |
        v
git switch <branch>
        |
        v
git pull --ff-only
```

Se il branch non esiste localmente ma esiste su `origin`, viene creato automaticamente come branch locale tracciato.

BranchSwitcher **non esegue automaticamente** `stash`, `reset --hard`, `clean`, `merge`, `rebase` o `push`.

## Sicurezza

Se un repository contiene modifiche locali, file staged o file non tracciati, lo switch viene bloccato per quel repository.

Se il branch locale e quello remoto sono divergenti, `git pull --ff-only` fallisce e la situazione deve essere risolta manualmente.

## Requisiti per l'utilizzatore

- Windows 10/11 x64;
- Git installato e raggiungibile tramite `git`, oppure percorso dell'eseguibile Git configurato nelle Impostazioni.

La build distribuita è **self-contained**, quindi non è necessario installare .NET sul PC dell'utilizzatore.

## Build per sviluppatori

Per compilare servono:

- .NET 8 SDK;
- Windows x64;
- Git.

Verifica SDK:

```powershell
dotnet --list-sdks
```

Build/publish:

```powershell
.\publish.bat
```

Output:

```text
publish/
├── BranchSwitcher.exe
├── configs/
├── settings.example.json
└── user-guide.pdf
```

Per generare lo ZIP distribuibile:

```powershell
.\create-zip.bat
```

La documentazione tecnica completa è in [`README-DEV.md`](README-DEV.md).

## Struttura del repository

```text
BranchSwitcher/
├── .github/
│   └── workflows/
│       └── build-windows.yml
├── BranchSwitcher/
│   ├── Models/
│   ├── Resources/
│   │   └── BranchSwitcher.ico
│   ├── Services/
│   ├── ConfigForm.cs
│   ├── MainForm.cs
│   ├── SettingsForm.cs
│   ├── Program.cs
│   └── BranchSwitcher.csproj
├── configs/
│   ├── TASK-101.conf
│   └── TASK-202.conf
├── docs/
│   └── images/
│       └── app-icon.png
├── README.md
├── README-DEV.md
├── user-guide.pdf
├── publish.bat
├── create-zip.bat
└── init-github.bat
```

## GitHub Actions

La workflow `.github/workflows/build-windows.yml` compila il progetto su `windows-latest`, pubblica la versione Windows x64 self-contained e genera un artifact ZIP scaricabile dal run GitHub Actions.

## Documentazione

- [`README.md`](README.md): panoramica del progetto e quick start.
- [`README-DEV.md`](README-DEV.md): sviluppo, architettura, build e manutenzione.
- [`user-guide.pdf`](user-guide.pdf): guida per gli utilizzatori finali.

## Versione

Versione progetto: **2.2.1**.
