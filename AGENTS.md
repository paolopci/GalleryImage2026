# Linee Guida Del Repository

## 1. Scopo e Ambito

Queste linee guida definiscono regole operative, stile collaborativo e criteri di qualità per il repository GitHub `GalleryImage2026` (workspace locale: `ImageGallery`).
Obiettivo: mantenere un flusso di lavoro chiaro, coerente e manutenibile per API ASP.NET Core + OAuth2 OpenIdConnect.

## 2. Regole di Collaborazione

Le regole di collaborazione condivise sono definite in [`_shared/regole-collaborazione.md`](_shared/regole-collaborazione.md).

Questo repository adotta integralmente quel modulo come riferimento per lingua, tono e ruolo atteso.

## 3. Workflow Operativo

Il workflow operativo condiviso è definito in [`_shared/workflow-operativo.md`](_shared/workflow-operativo.md).

Questo repository adotta integralmente quel modulo come riferimento vincolante per lettura iniziale, checklist, scelte `1/2`, validazioni, uso tool e autorizzazione delle skill.

Regola esplicita di avvio: alla prima interazione su questo progetto bisogna leggere sempre `AGENTS.md` come prima azione, prima di analisi, piano, uso tool o modifiche.

## 4. Struttura Del Progetto E Organizzazione Dei Moduli

La soluzione è suddivisa in quattro progetti .NET 10:

- `ImageGallery.API/`: Web API ASP.NET Core (controller, avvio API, configurazione OpenAPI).
- `ImageGallery.Client/`: front end MVC ASP.NET Core (`Controllers/`, `Views/`, asset statici in `wwwroot/`).
- `ImageGallery.IdentityServer/`: server di identità OAuth2/OpenID Connect (Duende IdentityServer) per autenticazione e rilascio token.
- `ImageGallery.Model/`: classi DTO/modello condivise usate dal client e dall'API.

Usa `ImageGallery.slnx` dalla radice del repository per compilare tutti i progetti insieme.

## 5. Comandi Di Build, Test E Sviluppo

Esegui i comandi dalla radice del repository:

- `dotnet restore ImageGallery.slnx`: ripristina i pacchetti NuGet per tutti i progetti.
- `dotnet build ImageGallery.slnx -c Debug`: compila l'intera soluzione.
- `dotnet run --project ImageGallery.API`: avvia localmente l'API.
- `dotnet run --project ImageGallery.Client`: avvia localmente il client MVC.
- `dotnet run --project ImageGallery.IdentityServer`: avvia localmente l'IdentityServer.
- `dotnet watch run --project ImageGallery.API`: avvia l'API con hot reload durante lo sviluppo.
- `dotnet watch run --project ImageGallery.IdentityServer`: avvia l'IdentityServer con hot reload durante lo sviluppo.
- `dotnet test`: esegue i test (attualmente non è presente un progetto di test dedicato; aggiungine uno prima di fare affidamento sui quality gate della CI).

## 6. Stile Del Codice E Convenzioni Di Naming

- Segui le convenzioni standard di C# con indentazione di 4 spazi e file-scoped namespace quando possibile.
- Mantieni `Nullable` e `ImplicitUsings` abilitati (già configurati in tutti i file `.csproj`).
- Convenzioni di naming:
  - `PascalCase` per classi, metodi e proprietà pubbliche.
  - `camelCase` per variabili locali e parametri.
  - Le classi controller terminano con `Controller` (ad esempio, `GalleryController`).
  - Preferisci un solo tipo pubblico per file, con nome file corrispondente al nome del tipo.

## 7. Linee Guida Per I Test

- Aggiungi test automatici in un progetto dedicato (consigliato: `ImageGallery.Tests` con xUnit).
- Rispecchia namespace e cartelle di produzione nei test (ad esempio, `Controllers/GalleryControllerTests.cs`).
- I nomi dei test devono descrivere il comportamento, ad esempio `GetGallery_ReturnsOk_WhenImagesExist`.
- Esegui `dotnet test` prima di aprire una pull request.

## 8. Linee Guida Per Commit E Pull Request

La cronologia Git non è disponibile in questa snapshot del workspace, quindi non è stato possibile ricavare un pattern di commit consolidato. Usa questa base:

- Formato commit: `type(scope): short summary` (esempio: `feat(api): add image upload endpoint`).
- Mantieni i commit focalizzati e compilabili.
- Le PR devono includere:
  - riepilogo chiaro delle modifiche e della motivazione,
  - ID dell'issue/task collegato,
  - evidenza dei test (output di `dotnet build`, `dotnet test`),
  - screenshot per modifiche UI in `ImageGallery.Client`.

## 9. Suggerimenti Su Sicurezza E Configurazione

Le regole condivise di sicurezza e configurazione sono definite in [`_shared/sicurezza-configurazione.md`](_shared/sicurezza-configurazione.md).

Questo repository le adotta come baseline minima per gestione segreti, configurazione sensibile, validazione input ed error handling.

## 10. Flusso Di Collaborazione

Il criterio generale di collaborazione condiviso è definito in [`_shared/flusso-collaborazione.md`](_shared/flusso-collaborazione.md).

Per tutte le modifiche relative a OAuth2, OpenID Connect, token, claim, scope, client, API protette e configurazioni IdentityServer, mantenere aggiornato progressivamente il file `READ_ME.md` in root, documentando i passaggi già eseguiti, l'impatto tecnico e i prossimi allineamenti da fare.

In caso di sovrapposizione o dubbio interpretativo tra questo file e i moduli condivisi, prevalgono il workflow operativo e i riferimenti esplicitamente richiamati in `AGENTS.md`.
