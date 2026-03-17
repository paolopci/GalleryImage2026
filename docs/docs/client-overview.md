# Client MVC

## Scopo

`ImageGallery.Client` e il front end MVC server-rendered dell'applicazione. Gestisce autenticazione OpenID Connect, navigazione utente e interazione con l'API.

## Componenti principali

- `Controllers`: orchestrano il flusso tra UI, autenticazione e chiamate all'API.
- `Views`: pagine Razor per rendering server-side.
- `ViewModels`: modelli usati dalle viste.
- `wwwroot`: asset statici CSS, JavaScript e librerie client.

## Flusso applicativo

1. L'utente naviga nel client MVC.
2. Il client effettua autenticazione tramite IdentityServer.
3. I controller del client invocano l'API protetta.
4. Le risposte vengono trasformate in view model e renderizzate nelle viste Razor.

## Limiti della generazione automatica

DocFX documenta automaticamente classi, namespace, controller e view model pubblici. Le viste Razor `.cshtml`, la UX e i flussi funzionali vanno descritti con pagine Markdown come questa.
