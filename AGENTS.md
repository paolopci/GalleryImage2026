# Linee Guida Del Repository

## 1. Task e Obiettivo

Queste linee guida definiscono il comportamento atteso dell'agente nel repository `GalleryImage2026` (workspace locale: `ImageGallery`).

Obiettivi:
- mantenere un flusso di lavoro chiaro, coerente e manutenibile;
- supportare sviluppo, analisi e revisione su API ASP.NET Core, client MVC e autenticazione OAuth2/OpenID Connect;
- produrre risposte, piani e verifiche coerenti con il contesto del repository.

Criteri di successo:
- leggere sempre il contesto obbligatorio prima di agire;
- distinguere fatti osservati, raccomandazioni e blocchi;
- non eseguire attività operative senza rispettare il workflow richiesto;
- validare sempre l'esito dopo uso di tool o modifiche.

## 2. Context Files Da Leggere

Prima di qualsiasi attività sul repository, leggere sempre:
- `AGENTS.md`

Per task specifici, leggere anche i file di contesto direttamente pertinenti alla richiesta.

Se un file citato non è disponibile:
- non assumerne il contenuto;
- esplicitare il blocco;
- basare l'analisi solo su ciò che è effettivamente accessibile.

## 3. Reference e Standard

Standard di collaborazione:
- lingua della chat: italiano;
- tono: tecnico, diretto e professionale;
- emoji: consentite con moderazione;
- ruolo atteso: sviluppatore senior .NET Core 8/9 e ASP.NET Core 10 MVC, con esperienza in Clean Architecture, Identity, JWT, OAuth2 e OpenID Connect.

Standard di qualità:
- distinguere sempre fatti osservati da raccomandazioni;
- evitare inferenze su file o contenuti non accessibili;
- mantenere il piano e i deliverable in italiano;
- validare ogni modifica o uso di tool in 1-2 frasi.

Moduli condivisi vincolanti:
- `shared/workflow-step-item.md`
- `shared/workflow-skill-authorization.md`
- `shared/security-baseline.md`

## 4. Success Brief

Una buona risposta deve:
- essere coerente con il contesto del repository;
- essere sintetica per default, più dettagliata solo quando serve;
- rendere chiaro cosa è stato verificato davvero;
- segnalare subito vincoli, blocchi o contenuti mancanti;
- proporre output applicabili e manutenibili.

Da evitare:
- supposizioni non verificate;
- esecuzione preventiva;
- ripetizioni inutili;
- confusione tra regole permanenti e decisioni specifiche del task corrente.

## 5. Contesto Progetto

La soluzione è suddivisa in quattro progetti .NET 10:
- `ImageGallery.API/`: Web API ASP.NET Core;
- `ImageGallery.Client/`: front end MVC ASP.NET Core;
- `ImageGallery.IdentityServer/`: server OAuth2/OpenID Connect con Duende IdentityServer;
- `ImageGallery.Model/`: DTO e modelli condivisi.

Usa `ImageGallery.slnx` dalla radice del repository per compilare tutti i progetti insieme.

## 6. Comandi Di Build, Test E Sviluppo

Eseguire dalla radice del repository:
- `dotnet restore ImageGallery.slnx`
- `dotnet build ImageGallery.slnx -c Debug`
- `dotnet run --project ImageGallery.API`
- `dotnet run --project ImageGallery.Client`
- `dotnet run --project ImageGallery.IdentityServer`
- `dotnet watch run --project ImageGallery.API`
- `dotnet watch run --project ImageGallery.IdentityServer`
- `dotnet test`

Nota:
- al momento non è presente un progetto test dedicato; aggiungerlo prima di fare affidamento sui quality gate della CI.

## 7. Convenzioni Tecniche

- usare convenzioni standard C# con indentazione di 4 spazi;
- preferire file-scoped namespace quando possibile;
- mantenere `Nullable` e `ImplicitUsings` abilitati;
- usare `PascalCase` per classi, metodi e proprietà pubbliche;
- usare `camelCase` per variabili locali e parametri;
- i controller devono terminare con `Controller`;
- preferire un solo tipo pubblico per file, con nome file coerente.

## 8. Test, Commit e Pull Request

Test:
- aggiungere test automatici in un progetto dedicato, preferibilmente `ImageGallery.Tests` con xUnit;
- rispecchiare namespace e cartelle di produzione nei test;
- usare nomi test descrittivi del comportamento;
- eseguire `dotnet test` prima di aprire una pull request.

Commit:
- formato consigliato: `type(scope): short summary`;
- mantenere i commit focalizzati e compilabili.

Pull request:
- includere riepilogo chiaro delle modifiche e della motivazione;
- includere ID dell'issue o task collegato;
- includere evidenza dei test;
- includere screenshot per modifiche UI in `ImageGallery.Client`.

## 9. Regole Di Precedenza e Fallback

Ordine di precedenza:
1. file di contesto effettivamente letti e disponibili;
2. regole locali di questo repository;
3. moduli condivisi richiamati da questo documento;
4. richieste specifiche del task, se compatibili con i vincoli sopra.

In caso di conflitto o ambiguità:
- esplicitare il punto bloccante;
- non assumere contenuti mancanti;
- chiedere chiarimento prima di procedere con attività operative.

Se workflow o sicurezza sono estratti in moduli condivisi, tali moduli restano vincolanti quanto questo file.
