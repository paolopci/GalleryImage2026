# OAuth2 e OpenID Connect nel progetto ImageGallery

## Scopo del documento

Questo file serve a tracciare, in modo progressivo, tutti i passaggi fatti e da fare per integrare:

- autenticazione con OpenID Connect nel client MVC;
- autorizzazione OAuth2 con access token per la Web API;
- configurazione di Duende IdentityServer come Identity Provider locale.

Il documento va aggiornato ogni volta che vengono introdotte nuove modifiche rilevanti nel flusso di autenticazione o autorizzazione.

## Architettura coinvolta

La soluzione è composta da quattro progetti principali:

- `ImageGallery.IdentityServer`: Identity Provider locale basato su Duende IdentityServer.
- `ImageGallery.Client`: applicazione MVC che autentica l'utente tramite OpenID Connect.
- `ImageGallery.API`: Web API protetta tramite Bearer token JWT.
- `ImageGallery.Model`: modelli condivisi tra client e API.

## Obiettivo finale

L'obiettivo è ottenere questo flusso:

1. L'utente apre `ImageGallery.Client`.
2. Il client reindirizza l'utente a `ImageGallery.IdentityServer` per il login.
3. Dopo l'autenticazione, il client riceve i token tramite Authorization Code Flow.
4. Il client usa l'access token per chiamare `ImageGallery.API`.
5. L'API valida il Bearer token e consente l'accesso solo se audience, tipo token e scope sono corretti.

## Stato attuale del lavoro

Al momento il progetto contiene già una prima implementazione del flusso OAuth2/OpenID Connect ed è stato corretto un disallineamento che causava l'errore `invalid_client` in fase di login del client MVC.

Le modifiche principali già introdotte sono:

- definizione di `IdentityResources`, `ApiScopes`, `ApiResources` e `Clients` nell'IdentityServer;
- registrazione in memoria di queste risorse tramite `AddInMemory...`;
- configurazione OpenID Connect nel client MVC;
- configurazione Bearer JWT nella Web API;
- introduzione di commenti in italiano per chiarire la logica di sicurezza.

## Passaggi già eseguiti

### 1. Definizione delle IdentityResources nell'IdentityServer

File coinvolto:

- `ImageGallery.IdentityServer/Config.cs`

Sono state definite le risorse identitarie:

- `openid`
- `profile`
- `roles`

Scopo:

- `openid` è obbligatorio per OpenID Connect;
- `profile` serve a recuperare informazioni base del profilo utente;
- `roles` serve a esporre il claim `role` ai client autorizzati.

### 2. Introduzione degli ApiScope

File coinvolto:

- `ImageGallery.IdentityServer/Config.cs`

È stato aggiunto lo scope:

- `imagegalleryapi.fullaccess`

Scopo:

- rappresenta il permesso applicativo per accedere alle operazioni della API Image Gallery;
- viene usato per decidere se un client può ottenere un access token utilizzabile contro la API.

### 3. Introduzione della ApiResource

File coinvolto:

- `ImageGallery.IdentityServer/Config.cs`

È stata definita la resource:

- `imagegalleryapi`

con associazione allo scope:

- `imagegalleryapi.fullaccess`

Scopo:

- identifica la API protetta come audience del token;
- permette all'API di verificare che il token sia davvero destinato a lei.

### 4. Registrazione delle risorse in memoria nell'IdentityServer

File coinvolto:

- `ImageGallery.IdentityServer/HostingExtensions.cs`

Sono stati configurati:

- `AddInMemoryIdentityResources(Config.IdentityResources)`
- `AddInMemoryApiResources(Config.ApiResources)`
- `AddInMemoryApiScopes(Config.ApiScopes)`
- `AddInMemoryClients(Config.Clients)`

Scopo:

- rendere effettiva a runtime la configurazione definita nel file `Config.cs`.

### 5. Configurazione del client MVC con OpenID Connect

File coinvolto:

- `ImageGallery.Client/Program.cs`

Nel client è stata configurata l'autenticazione con:

- cookie come schema locale;
- OpenID Connect come challenge scheme;
- `Authority = https://localhost:5001`;
- `ResponseType = code`;
- `SaveTokens = true`;
- `GetClaimsFromUserInfoEndpoint = true`.

Sono stati inoltre aggiunti:

- richiesta dello scope `roles`;
- richiesta dello scope `imagegalleryapi.fullaccess`;
- mappatura del claim `role`;
- configurazione di `NameClaimType` e `RoleClaimType`.

Scopo:

- delegare il login all'IdentityServer;
- ottenere token e claim necessari;
- preparare il client a chiamare la API protetta.

### 6. Disattivazione della rimappatura automatica dei claim

File coinvolti:

- `ImageGallery.Client/Program.cs`
- `ImageGallery.API/Program.cs`

È stato aggiunto:

```csharp
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
```

Scopo:

- evitare che i claim standard JWT vengano convertiti automaticamente nei claim type Microsoft;
- mantenere nomi come `sub`, `role`, `scope`, `given_name` uguali a quelli presenti nel token originale.

### 7. Configurazione della Web API con autenticazione Bearer

File coinvolto:

- `ImageGallery.API/Program.cs`

È stata configurata l'autenticazione Bearer con:

- `Authority = https://localhost:5001`
- `ValidAudience = imagegalleryapi`

Scopo:

- accettare solo token emessi dall'IdentityServer locale;
- verificare che il token sia destinato alla API;
- accettare token destinati alla API secondo la configurazione corrente del progetto.

### 8. Configurazione dell'autorizzazione nella Web API

File coinvolti:

- `ImageGallery.API/Program.cs`
- `ImageGallery.API/Controllers/ImagesController.cs`

È stata aggiunta una policy:

- `ImageGalleryApiFullAccess`

La policy richiede:

- utente autenticato;
- claim `scope` con valore `imagegalleryapi.fullaccess`.

Il controller `ImagesController` è stato protetto con:

```csharp
[Authorize(Policy = "ImageGalleryApiFullAccess")]
```

Scopo:

- non limitarsi a validare il token;
- richiedere realmente un token con lo scope corretto per accedere agli endpoint immagini.

### 8.1 Correzione del runtime error sulla policy API

File coinvolto:

- `ImageGallery.API/Program.cs`

Problema rilevato:

- `ImagesController` usava `[Authorize(Policy = "ImageGalleryApiFullAccess")]`;
- la policy `ImageGalleryApiFullAccess` era presente come esempio commentato ma non registrata realmente nei servizi.

Effetto:

- durante la richiesta a `GET /api/images` il middleware di autorizzazione non trovava la policy;
- la chiamata dal client MVC terminava con errore HTTP 500 invece di una normale risposta autorizzata/non autorizzata.

Correzione applicata:

- registrata esplicitamente la policy `ImageGalleryApiFullAccess` dentro `builder.Services.AddAuthorization(...)`;
- mantenuta anche la policy `UserCanAddImage` della libreria `ImageGallery.Authorization`.

Impatto tecnico:

- gli endpoint di `ImagesController` tornano a usare correttamente il controllo sul claim `scope = imagegalleryapi.fullaccess`;
- il middleware di autorizzazione non va più in errore per policy mancante;
- la creazione immagini continua a richiedere anche la policy aggiuntiva `UserCanAddImage`.

### 9. Inserimento di `UseAuthentication()` nella pipeline API

File coinvolto:

- `ImageGallery.API/Program.cs`

È stato aggiunto:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

Scopo:

- eseguire prima l'autenticazione del token;
- poi applicare l'autorizzazione basata su policy, ruoli o claim.

### 10. Gestione di OpenAPI nella Web API

File coinvolti:

- `ImageGallery.API/Program.cs`
- `ImageGallery.API/ImageGallery.API.csproj`

OpenAPI è stato commentato/disattivato.

Effetto:

- la API continua a funzionare;
- non viene più esposto il documento OpenAPI;
- la dipendenza è stata trattata come non necessaria nel contesto attuale.

## Differenze concettuali importanti fissate durante il lavoro

### `Audience` e `ValidAudience`

Nel corso si può trovare:

```csharp
options.Audience = "imagegalleryapi";
```

Nel progetto è stata usata una forma più esplicita:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience = "imagegalleryapi"
};
```

Le due configurazioni hanno lo stesso obiettivo principale:

- verificare che il token sia destinato alla API `imagegalleryapi`.

La seconda forma è più estendibile perché consente di configurare nello stesso blocco anche:

- `ValidTypes`
- `NameClaimType`
- `RoleClaimType`

### Nota su `ValidTypes = new[] { "at+jwt" }`

Questa impostazione era stata introdotta come controllo più restrittivo sul tipo di token accettato.

Nel flusso reale del progetto è stata poi rimossa perché la API continuava a rispondere `401 Unauthorized` pur ricevendo il Bearer token dal client MVC.

Conclusione pratica:

- il controllo `ValidTypes` è utile solo se si è certi del tipo esatto emesso dall'IdentityServer;
- in questo progetto, per mantenere il flusso funzionante, la validazione è stata lasciata meno restrittiva.

### `NameClaimType` e `RoleClaimType`

Queste impostazioni servono a dire ad ASP.NET Core:

- quale claim usare come nome dell'utente autenticato;
- quale claim usare come lista ruoli.

Nel progetto attuale:

- `NameClaimType = "given_name"`
- `RoleClaimType = "role"`

## Correzione applicata dopo errore `invalid_client`

Durante l'avvio della solution il client MVC ha restituito:

- `OpenIdConnectProtocolException`
- errore `invalid_client`

La causa era un disallineamento in `ImageGallery.IdentityServer/Config.cs`:

- il client MVC usava `ClientId = imagegalleryclient`;
- l'IdentityServer era stato configurato con `ClientId = imagegalleryclient.fullaccess`.

È stata applicata la correzione seguente:

- `ClientId` dell'IdentityServer riallineato a `imagegalleryclient`;
- `AllowedScopes` del client nell'IdentityServer riallineati a `imagegalleryapi.fullaccess`, coerente con gli scope realmente definiti.

Impatto:

- il login OpenID Connect del client MVC non viene più rifiutato per client sconosciuto;
- si evita anche un successivo errore di tipo `invalid_scope`, perché ora lo scope autorizzato nel client coincide con quello richiesto dal client MVC e con quello definito nelle API.

## Correzione applicata dopo errore `401 Unauthorized` verso la API

Dopo la correzione del `ClientId`, il client MVC riusciva ad autenticare l'utente ma falliva nella chiamata verso `ImageGallery.API` con risposta:

- `401 Unauthorized`

La causa era nel controller MVC:

- il client aveva ottenuto e salvato l'access token;
- però le chiamate HTTP verso la API non impostavano l'header `Authorization: Bearer <token>`.

È stata applicata la correzione seguente in `ImageGallery.Client/Controllers/GalleryController.cs`:

- recupero dell'access token tramite `HttpContext.GetTokenAsync(OpenIdConnectParameterNames.AccessToken)`;
- aggiunta dell'header Bearer su tutte le chiamate verso `APIClient`.

Impatto:

- il token viene finalmente inviato alla API;
- la API può autenticare la richiesta e valutare la policy sullo scope `imagegalleryapi.fullaccess`;
- il `401` non dipende più dall'assenza dell'header Bearer.

## Correzione applicata per visibilità token solo in Development

Per confrontare l'output locale con il corso senza esporre token in produzione, il controller MVC è stato aggiornato nel metodo `LoginIdentityInformation()`.

Correzione applicata:

- recupero sia di `id_token` sia di `access_token` dal contesto di autenticazione;
- scrittura nei log dei token completi solo quando l'ambiente corrente è `Development`;
- fuori da `Development` logging limitato ai soli claim utente.

Impatto:

- in sviluppo il terminale mostra anche `Access token:` come nel corso;
- in produzione e negli altri ambienti non vengono scritti token sensibili nei log;
- il comportamento resta coerente con il flusso OAuth2/OpenID Connect già configurato nel client MVC.

## Correzione applicata dopo ulteriore `401 Unauthorized` in validazione Bearer

Dopo aver iniziato a inviare correttamente il Bearer token alla API, la richiesta continuava a essere rifiutata con `401 Unauthorized`.

La causa più probabile era la validazione troppo restrittiva del tipo di token nella configurazione Bearer della API:

- era stato impostato `ValidTypes = new[] { "at+jwt" }`.

Nel progetto attuale questa restrizione è stata rimossa.

Impatto:

- la API continua a validare `Authority` e `Audience`;
- viene evitato il rifiuto del token dovuto al tipo token atteso;
- il flusso resta più vicino alla configurazione essenziale del corso.

## Correzione applicata per usare `given_name` nella descrizione immagini restituite dalla API

File coinvolto:

- `ImageGallery.IdentityServer/Config.cs`
- `ImageGallery.API/Controllers/ImagesController.cs`

Correzione applicata:

- la `ApiResource` `imagegalleryapi` è stata configurata per includere `given_name` tra gli user claim emessi verso la API;
- oltre al claim `sub`, il controller legge anche il claim `given_name` dal Bearer token;
- la query verso il repository continua a usare `sub` come identificativo tecnico del proprietario;
- prima di restituire i DTO `Image`, il campo `Title` viene valorizzato con il testo `An image by <given_name>`;
- se il token corrente non contiene ancora `given_name`, il controller usa temporaneamente il fallback `An image by the user` invece di interrompere la richiesta.

Motivo tecnico:

- il claim `sub` deve restare l'identificativo stabile usato lato API per filtrare le immagini dell'utente;
- il claim `given_name` è invece il dato corretto da mostrare in output quando serve una descrizione leggibile per l'utente autenticato;
- l'access token già emesso prima della modifica può non contenere ancora il claim, quindi serve una gestione compatibile durante la transizione.

Impatto:

- il client MVC continua a ricevere la stessa struttura JSON del DTO `Image`;
- nella galleria non viene più visualizzato l'identificativo tecnico del soggetto autenticato;
- dopo l'emissione di un nuovo access token, il testo mostrato per ogni immagine usa il nome profilo presente nei claim OIDC;
- finché non viene ottenuto un nuovo token, la UI resta funzionante grazie al fallback senza eccezioni runtime.

## Correzione applicata per preservare il titolo inserito nel form di aggiunta immagine

File coinvolti:

- `ImageGallery.Client/ViewModels/AddImageViewModel.cs`
- `ImageGallery.Client/Controllers/GalleryController.cs`
- `ImageGallery.API/Controllers/ImagesController.cs`

Correzione applicata:

- il campo `Title` del form MVC non è più obbligatorio lato view model;
- durante il submit, il client usa il testo inserito nel campo `You image's title` se contiene un valore non vuoto;
- se il campo resta vuoto, il client genera il fallback `An image by <given_name>` prima di inviare il DTO alla API;
- la API, quando restituisce le immagini, mantiene il titolo salvato se presente e usa il fallback basato su `given_name` solo se il titolo risulta vuoto o mancante.

Motivo tecnico:

- in precedenza il titolo digitato dall'utente veniva inviato dal client ma poi sovrascritto sempre in lettura dalla API con `An image by <given_name>`;
- inoltre il form MVC impediva di lasciare il titolo vuoto, rendendo impossibile il comportamento desiderato con fallback automatico.

Impatto:

- se l'utente inserisce un titolo personalizzato, quel titolo viene mostrato in galleria;
- se l'utente lascia il campo vuoto, il comportamento resta coerente con il fallback precedente;
- il comportamento è ora uniforme tra salvataggio lato client e resa finale lato API.

## Correzione applicata per limitare create, update e delete alle sole immagini del proprietario

File coinvolto:

- `ImageGallery.API/Controllers/ImagesController.cs`

Correzione applicata:

- il controller recupera il claim `sub` tramite un metodo condiviso `GetOwnerId()`;
- in `CreateImage()` il valore di `sub` viene assegnato a `OwnerId` prima del salvataggio;
- in `UpdateImage()` e `DeleteImage()` viene verificato che l'immagine richiesta appartenga all'utente autenticato;
- se l'immagine esiste ma appartiene a un altro utente, la API restituisce `Forbid()`.

Motivo tecnico:

- filtrare `GetImages()` per proprietario non basta a proteggere anche le operazioni di modifica;
- senza controllo esplicito su create, update e delete, un utente autenticato potrebbe agire su immagini non sue conoscendone l'identificativo.

Impatto:

- ogni nuova immagine viene salvata con un proprietario coerente con il claim `sub` del token;
- modifica e cancellazione sono consentite solo al proprietario dell'immagine;
- il comportamento della API è ora coerente con la regola di ownership già applicata nella lettura della galleria.

## Correzione applicata per rendere più robusto il controllo ownership in edit immagine

File coinvolti:

- `ImageGallery.API/Authorization/MustOwnImageHandler.cs`
- `ImageGallery.Client/Views/Gallery/EditImage.cshtml`

Correzione applicata:

- l'handler `MustOwnImageHandler` recupera ora l'identificativo dell'immagine prima dal `AuthorizationFilterContext`, poi dal `HttpContext` e solo come fallback da `IHttpContextAccessor`;
- la view `EditImage` invia esplicitamente l'`Id` dell'immagine tramite campo hidden nel postback del form MVC.

Motivo tecnico:

- il controllo di ownership dipende dal corretto recupero dell'`id` della risorsa richiesta durante l'autorizzazione;
- affidarsi solo a `IHttpContextAccessor` rende il recupero del route value meno esplicito e più fragile rispetto al contesto reale della richiesta autorizzata;
- nel flusso MVC di modifica immagine, l'`Id` deve essere preservato in modo esplicito tra GET e POST per evitare ambiguità nel `PUT` verso la API.

Impatto:

- la policy `MustOwnImage` usa ora una sorgente più affidabile per determinare la risorsa da autorizzare;
- il form di modifica mantiene sempre l'identificativo corretto dell'immagine selezionata;
- il flusso di edit risulta più coerente e più semplice da verificare quando si prova a manipolare manualmente l'URL o il postback.

## Correzione applicata dopo `401 Unauthorized` dovuto a chiamata HTTP verso la API

Dopo i fix precedenti, dai log risultava che il client MVC chiamava:

- `http://localhost:5212/api/images/`

mentre la API è configurata con:

- `UseHttpsRedirection()`
- endpoint HTTPS su `https://localhost:7162`

Nel profilo di sviluppo del client era quindi presente un disallineamento:

- `ImageGallery.Client/appsettings.Development.json` puntava a `http://localhost:5212/`

È stata applicata la correzione seguente:

- `ImageGalleryAPIRoot` aggiornato a `https://localhost:7162/`

Impatto:

- il client MVC chiama direttamente la API in HTTPS;
- si evita il redirect HTTP -> HTTPS;
- si evita il rischio di perdere l'header `Authorization` durante il redirect;
- il `401 Unauthorized` non dipende più dal protocollo usato verso la API.

## Correzione applicata per risolvere `AddUserAccessTokenHandler()` non disponibile nel client MVC

File coinvolti:

- `ImageGallery.Client/ImageGallery.Client.csproj`
- `ImageGallery.Client/Program.cs`

Correzione applicata:

- aggiunto il package `Duende.AccessTokenManagement.OpenIdConnect` al progetto MVC;
- registrato `AddOpenIdConnectAccessTokenManagement()` dopo la configurazione di cookie + OpenID Connect;
- mantenuta la registrazione dell'`HttpClient` `APIClient` con `.AddUserAccessTokenHandler()` per allegare automaticamente l'access token dell'utente corrente alle chiamate verso la API.

Motivo tecnico:

- `AddUserAccessTokenHandler()` non è esposto dal solo package `Microsoft.AspNetCore.Authentication.OpenIdConnect`;
- il metodo appartiene all'integrazione di access token management per applicazioni web OIDC e richiede anche la registrazione dei servizi dedicati;
- senza questa libreria il progetto compila con simbolo non risolto e il client non può delegare in modo automatico l'invio e il refresh del token utente.

Impatto:

- il progetto client ha ora la dipendenza corretta per usare l'handler automatico sui client HTTP;
- le chiamate dell'`APIClient` possono usare il token dell'utente autenticato senza impostare manualmente l'header Bearer a ogni richiesta;
- il flusso OIDC del client resta coerente con `SaveTokens = true` e con la richiesta dello scope `offline_access`.

## Correzione applicata per rimuovere l'aggiunta manuale del Bearer token nel controller MVC

File coinvolti:

- `ImageGallery.Client/Controllers/GalleryController.cs`
- `ImageGallery.Client/Program.cs`

Correzione applicata:

- rimosso dal controller il metodo privato che leggeva l'access token da `HttpContext` e impostava manualmente `Authorization: Bearer ...`;
- eliminate le chiamate ridondanti a quel metodo prima delle richieste HTTP verso la API;
- mantenuto l'uso del named client `APIClient`, già configurato in `Program.cs` con `.AddUserAccessTokenHandler()`.

Motivo tecnico:

- dopo l'introduzione di `Duende.AccessTokenManagement.OpenIdConnect`, il client HTTP è già in grado di allegare automaticamente l'access token dell'utente autenticato;
- mantenere anche l'impostazione manuale dell'header nel controller crea una doppia responsabilità e rende più difficile capire quale componente stia propagando davvero il token;
- centralizzare questo comportamento nella configurazione dell'`HttpClient` riduce il codice ripetuto e rende il flusso più coerente.

Impatto:

- il controller MVC resta focalizzato sulla logica applicativa e non sulla gestione del Bearer token;
- tutte le chiamate fatte tramite `APIClient` usano la stessa strategia centralizzata di propagazione del token;
- resta invariata la lettura dei token in `LoginIdentityInformation()` per logging locale e diagnostica in ambiente `Development`.

## Correzione applicata dopo errore su claim `sub` mancante nella API

File coinvolti:

- `ImageGallery.API/Program.cs`
- `ImageGallery.API/Controllers/ImagesController.cs`

Correzione applicata:

- mantenuta la protezione globale di `ImagesController` con `[Authorize]`, così gli endpoint non entrano nel controller quando la richiesta è anonima;
- reso più esplicito il messaggio di errore in `GetOwnerId()` quando un utente autenticato non contiene il claim `sub`;
- riallineata la API a `AddOAuth2Introspection(...)` invece di `AddJwtBearer(...)`;
- configurata l'introspection con `ClientId = imagegalleryapi` e `ClientSecret = secret`, coerenti con `ApiSecrets` della resource API in `Config.cs`.

Motivo tecnico:

- il client MVC è configurato per ricevere `AccessTokenType = Reference`, quindi il bearer token inviato alla API non è un JWT validabile localmente;
- per i Reference Token la API deve interrogare l'IdentityServer tramite introspection, usando credenziali della resource protetta;
- senza questo allineamento la API risponde `401` perché tenta di interpretare come JWT un token opaco/reference.

Impatto:

- la API può validare correttamente i Reference Token emessi dall'IdentityServer locale;
- le richieste senza token valido continuano a essere bloccate dal middleware prima di entrare negli endpoint immagini;
- quando l'introspection ha esito positivo, la API può leggere `sub` dal principal autenticato e usarlo come identificativo del proprietario.

## Allineamento finale del refresh token con `offline_access`

File coinvolti:

- `ImageGallery.IdentityServer/Config.cs`
- `ImageGallery.Client/Program.cs`

Correzione applicata:

- mantenuta nel client MVC la richiesta dello scope `offline_access`;
- aggiunto in modo esplicito `IdentityServerConstants.StandardScopes.OfflineAccess` agli `AllowedScopes` del client `imagegalleryclient`.

Motivo tecnico:

- il client MVC usa `SaveTokens = true` e `AddOpenIdConnectAccessTokenManagement()`, quindi il refresh automatico dell'access token dipende dalla presenza coerente del refresh token;
- rendere esplicita l'autorizzazione allo scope `offline_access` lato IdentityServer elimina ambiguità nella configurazione del client e allinea in modo chiaro richiesta OIDC ed emissione del refresh token.

Impatto:

- il flusso `Reference Token` resta coerente anche quando l'access token scade;
- il client MVC è configurato in modo esplicito per ottenere e usare il refresh token nelle chiamate alla API.

## Chiarimento finale sull'autorizzazione di `ImagesController`

File coinvolto:

- `ImageGallery.API/Controllers/ImagesController.cs`

Allineamento applicato:

- confermata la protezione globale del controller con `[Authorize]`;
- confermato l'uso di policy più restrittive solo sulle operazioni sensibili, come scrittura e ownership della risorsa;
- corretto il commento introduttivo del controller per riflettere il comportamento reale del codice.

Motivo tecnico:

- nel modello corrente a `Reference Token`, il requisito minimo per tutte le azioni è avere un principal autenticato ottenuto via introspection;
- imporre una policy di scope unica a livello controller sarebbe una scelta autorizzativa più restrittiva, ma non è un requisito tecnico necessario per far funzionare correttamente i `Reference Token`.

Impatto:

- il `GET` immagini continua a richiedere autenticazione valida;
- le operazioni di modifica restano protette da policy dedicate come `UserCanAddImage`, `ClientApplicationCanWrite` e `MustOwnImage`;
- la documentazione ora riflette il comportamento reale del controller.

## Introduzione del persisted grant store reale nell'IdentityServer

File coinvolti:

- `ImageGallery.IdentityServer/ImageGallery.IdentityServer.csproj`
- `ImageGallery.IdentityServer/HostingExtensions.cs`
- `ImageGallery.IdentityServer/Migrations/PersistedGrantDb/*`

Correzione applicata:

- aggiunto `Duende.IdentityServer.EntityFramework` al progetto IdentityServer;
- configurato `AddOperationalStore(...)` con SQL Server per il `PersistedGrantDbContext`;
- aggiunta una migration dedicata al persisted grant store;
- configurata l'applicazione automatica delle migration all'avvio dell'IdentityServer.

Motivo tecnico:

- con `Reference Token`, refresh token e revocation, i persisted grants non devono restare solo in memoria;
- lo store in-memory è utile per demo rapide, ma non è adatto a una revocation affidabile e non sopravvive ai riavvii del processo;
- l'operational store persistente è la base corretta per conservare authorization code, reference token, refresh token e stato di revoca.

Impatto:

- il ciclo di vita dei token gestiti dall'IdentityServer è ora persistente su database;
- la revocation non dipende più dalla vita del processo locale;
- introspection e revocation lavorano contro uno stato token coerente e durevole.

## Introduzione del logout coordinato con token revocation nel client MVC

File coinvolti:

- `ImageGallery.Client/Program.cs`
- `ImageGallery.Client/Controllers/AuthenticationController.cs`
- `ImageGallery.Client/Services/ITokenRevocationService.cs`
- `ImageGallery.Client/Services/TokenRevocationService.cs`

Correzione applicata:

- registrato un client HTTP dedicato al revocation endpoint dell'IdentityServer;
- introdotto il servizio `ITokenRevocationService` per centralizzare la revoca dei token dell'utente corrente;
- aggiornato il logout MVC per revocare prima `refresh_token`, poi `access_token`, e solo dopo eseguire `SignOut(...)`.

Motivo tecnico:

- il client salva localmente i token OIDC dell'utente autenticato;
- fare solo `SignOut(...)` chiude la sessione locale e quella OIDC, ma non revoca automaticamente i token già rilasciati;
- con `Reference Token` e introspection è una best practice tentare la revoca esplicita, soprattutto del refresh token, così il token non può più essere riutilizzato dopo il logout.

Impatto:

- il logout utente ora include anche un tentativo esplicito di revocation;
- l'implementazione è `fail-open`: se la revocation fallisce, il logout prosegue comunque e l'errore viene tracciato nei log;
- il client MVC separa chiaramente le chiamate business verso la API dalle chiamate tecniche verso il revocation endpoint.

## Punti da verificare e allineare

Va ricontrollato che in `AllowedScopes` del client siano coerenti:

- il nome dello scope richiesto dal client;
- il nome dello scope effettivamente definito in `ApiScopes`;
- il nome dello scope associato alla `ApiResource`.

## Prossimi passaggi suggeriti

Questa sezione va aggiornata man mano che il lavoro prosegue.

Passi probabili successivi:

1. Verificare il comportamento completo con login, consenso, emissione token e chiamata API.
2. Proteggere eventuali altri controller API con policy o attributi `[Authorize]`.
3. Aggiungere test automatici per autenticazione e autorizzazione.
4. Eventualmente ripristinare OpenAPI o Swagger se servirà per documentare/testare gli endpoint protetti.

## Come aggiornare questo documento

Ogni volta che viene fatta una modifica su OAuth2 o OpenID Connect, aggiornare questo file aggiungendo:

- il file modificato;
- la modifica fatta;
- il motivo tecnico della modifica;
- l'impatto sul funzionamento del progetto;
- eventuali dipendenze o allineamenti richiesti in altri progetti della soluzione.

## Nota finale

Questo documento non è pensato come documentazione teorica generica su OAuth2/OpenID Connect, ma come diario tecnico strutturato delle modifiche applicate alla soluzione `ImageGallery`.
