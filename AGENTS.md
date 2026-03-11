# Linee Guida Del Repository

## 1. Scopo e Ambito

Queste linee guida definiscono regole operative, stile collaborativo e criteri di qualità per il repository `GalleryImage2026`.
Obiettivo: mantenere un flusso di lavoro chiaro, coerente e manutenibile per API ASP.NET Core + GraphQL.

## Struttura Del Progetto E Organizzazione Dei Moduli

La soluzione e suddivisa in tre progetti .NET 10:

- `ImageGallery.API/`: Web API ASP.NET Core (controller, avvio API, configurazione OpenAPI).
- `ImageGallery.Client/`: front end MVC ASP.NET Core (`Controllers/`, `Views/`, asset statici in `wwwroot/`).
- `ImageGallery.Model/`: classi DTO/modello condivise usate dal client e dall'API.

Usa `ImageGallery.slnx` dalla radice del repository per compilare tutti i progetti insieme.

## Comandi Di Build, Test E Sviluppo

Esegui i comandi dalla radice del repository:

- `dotnet restore ImageGallery.slnx`: ripristina i pacchetti NuGet per tutti i progetti.
- `dotnet build ImageGallery.slnx -c Debug`: compila l'intera soluzione.
- `dotnet run --project ImageGallery.API`: avvia localmente l'API.
- `dotnet run --project ImageGallery.Client`: avvia localmente il client MVC.
- `dotnet watch run --project ImageGallery.API`: avvia l'API con hot reload durante lo sviluppo.
- `dotnet test`: esegue i test (attualmente non e presente un progetto di test dedicato; aggiungine uno prima di fare affidamento sui quality gate della CI).

## Stile Del Codice E Convenzioni Di Naming

- Segui le convenzioni standard di C# con indentazione di 4 spazi e file-scoped namespace quando possibile.
- Mantieni `Nullable` e `ImplicitUsings` abilitati (gia configurati in tutti i file `.csproj`).
- Convenzioni di naming:
- `PascalCase` per classi, metodi e proprieta pubbliche.
- `camelCase` per variabili locali e parametri.
- Le classi controller terminano con `Controller` (ad esempio, `GalleryController`).
- Preferisci un solo tipo pubblico per file, con nome file corrispondente al nome del tipo.

## Linee Guida Per I Test

- Aggiungi test automatici in un progetto dedicato (consigliato: `ImageGallery.Tests` con xUnit).
- Rispecchia namespace e cartelle di produzione nei test (ad esempio, `Controllers/GalleryControllerTests.cs`).
- I nomi dei test devono descrivere il comportamento, ad esempio `GetGallery_ReturnsOk_WhenImagesExist`.
- Esegui `dotnet test` prima di aprire una pull request.

## Linee Guida Per Commit E Pull Request

La cronologia Git non e disponibile in questa snapshot del workspace, quindi non e stato possibile ricavare un pattern di commit consolidato. Usa questa base:

- Formato commit: `type(scope): short summary` (esempio: `feat(api): add image upload endpoint`).
- Mantieni i commit focalizzati e compilabili.
- Le PR devono includere:
- riepilogo chiaro delle modifiche e della motivazione,
- ID dell'issue/task collegato,
- evidenza dei test (output di `dotnet build`, `dotnet test`),
- screenshot per modifiche UI in `ImageGallery.Client`.

## Suggerimenti Su Sicurezza E Configurazione

- Non committare segreti in `appsettings*.json` o `launchSettings.json`.
- Preferisci variabili d'ambiente o user secrets per le credenziali locali.
- Valida tutti gli input API e restituisci codici di stato HTTP coerenti per richieste non valide.

## Flusso Di Collaborazione

- Prima di qualsiasi modifica a codice o configurazione, presenta sempre una breve checklist dei passaggi pianificati.
- Questa checklist e obbligatoria in ogni turno, anche quando la modalita di collaborazione attiva non e `Plan`.
- Dopo aver presentato la checklist, chiedi esplicitamente se procedere un elemento alla volta oppure eseguire tutti gli elementi insieme.
- Non iniziare modifiche prima di aver presentato la checklist, a meno che l'utente non dica esplicitamente di saltarla per quel turno.
