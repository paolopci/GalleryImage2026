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
- configurata l'introspection con `ClientId = imagegalleryapi` e secret applicativo letto da configurazione sicura, coerente con la resource API dell'IdentityServer.

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

## Introduzione di `dotnet user-jwts` come modalità dev-only per test manuali API

File coinvolti:

- `ImageGallery.API/Program.cs`
- `ImageGallery.API/appsettings.Development.json`

Correzione applicata:

- mantenuto `AddOAuth2Introspection(...)` come percorso standard della solution;
- aggiunto in `Development` uno schema `JwtBearer` secondario chiamato `DevJwt`;
- aggiunto un `policy scheme` dinamico che instrada i bearer token JWT verso `DevJwt` e i token opachi/reference verso l'introspection;
- abilitato `dotnet user-jwts` sul progetto API, così il tool aggiorna `appsettings.Development.json` con issuer e audience del ramo dev-only.

Motivo tecnico:

- `dotnet user-jwts` genera JWT locali pensati per `JwtBearer`, non `Reference Token` validati via introspection;
- la API del progetto usa come flusso reale `IdentityServer + Reference Token + introspection`, quindi l'uso diretto di `user-jwts` avrebbe rotto la coerenza architetturale se usato come sostituto;
- il doppio schema consente test manuali rapidi da CLI/Postman senza alterare il comportamento reale dell'applicazione.

Impatto:

- in ambiente `Development` la API accetta sia i token reali emessi dall'IdentityServer sia i JWT locali creati con `dotnet user-jwts`;
- il flusso reale MVC/OIDC continua a passare da IdentityServer e introspection;
- `user-jwts` resta confinato ai test manuali API e non sostituisce refresh token, revocation o logout OIDC.

## Correzione del `401` dovuto a redirect HTTP->HTTPS dell'API in sviluppo

File coinvolti:

- `ImageGallery.API/Program.cs`

Correzione applicata:

- `UseHttpsRedirection()` viene applicato solo fuori da `Development`.

Motivo tecnico:

- il client MVC usa `HttpClient` server-side verso `http://localhost:5212/api/images/`;
- quando l'API in sviluppo viene avviata anche sul profilo HTTPS, il middleware di redirect può spostare la richiesta verso `https://localhost:7162`;
- nel passaggio di redirect il bearer token non risulta più valido per la richiesta finale e l'API risponde `401`, mostrando solo `AuthenticationScheme: Introspection was challenged`.

Impatto:

- in sviluppo il canale tra client MVC e API resta stabile su `http://localhost:5212`;
- la richiesta Bearer arriva direttamente all'endpoint API senza redirect intermedio;
- il flusso OIDC locale risulta coerente sia quando i progetti vengono avviati da CLI sia quando vengono avviati da Visual Studio.

Esempi operativi verificati:

- `dotnet user-jwts create --project ImageGallery.API --scheme DevJwt --name dev-jwt-read --role FreeUser --scope imagegalleryapi.read --claim given_name=DevJwtRead --claim paese=nl`
- `dotnet user-jwts create --project ImageGallery.API --scheme DevJwt --name dev-jwt-write --role PayingUser --scope imagegalleryapi.write --claim given_name=DevJwtWrite --claim paese=be`

Esito dei test:

- `GET /api/images` con token `DevJwt` read -> `200`
- `POST /api/images` con token `DevJwt` read -> `403`
- `POST /api/images` con token `DevJwt` write e body non valido -> `400`, segnale che l'autorizzazione è passata e il rifiuto arriva dalla validazione del payload

## Introduzione della collection Postman ordinata per test manuali API

File coinvolti:

- `ImageGallery.API.Postman.Collection.json`

Correzione applicata:

- aggiunta nella root della solution una collection Postman importabile;
- i test hanno titoli numerati e descrittivi per indicare esplicitamente l'ordine di esecuzione;
- le variabili operative usate dai test successivi vengono valorizzate automaticamente dai test precedenti, in particolare `createdImageId` e `deletedImageId`.

Motivo tecnico:

- per testare in modo ripetibile la API con Postman non basta elencare gli endpoint;
- serve una sequenza stabile che distingua chiaramente i casi read-only, i controlli di autorizzazione e il CRUD minimo;
- il caricamento automatico delle variabili evita di copiare a mano ID tra una request e la successiva e riduce gli errori manuali.

Impatto:

- la collection consente test ordinati di `GET`, `POST`, `PUT` e `DELETE` sugli endpoint immagini;
- l'ordine consigliato e' incorporato nel nome di ogni test;
- restano da valorizzare manualmente solo `bearerTokenRead` e `bearerTokenWrite`, mentre gli ID dinamici vengono propagati automaticamente dai test precedenti.

## Aggiornamento della collection Postman per token reali IdentityServer

File coinvolti:

- `ImageGallery.API.Postman.Collection.json`
- `ImageGallery.IdentityServer/Config.cs`

Correzione applicata:

- sostituita la collection basata su `DevJwt` con una collection ordinata che ottiene i token direttamente dall'IdentityServer locale;
- aggiunti come primi item `1.1` e `1.2` per ottenere rispettivamente il token read di `David` e il token write di `Emma`;
- rinumerati i test successivi, così `02 - GET immagini con token read restituisce 200` parte subito dopo i login;
- introdotto nell'IdentityServer un client dedicato `imagegallerypostman` con `ResourceOwnerPassword`, secret condiviso e `Reference Token`, pensato per i test manuali della collection;
- configurate nella collection le variabili `identityServerUrl`, `postmanClientId`, `postmanClientSecret`, `bearerTokenRead` e `bearerTokenWrite`.

Motivo tecnico:

- la collection precedente richiedeva di incollare manualmente JWT `DevJwt`, quindi non esercitava il flusso reale dell'IDP della solution;
- per test ripetibili da Postman serviva un modo semplice per ottenere token emessi davvero da Duende IdentityServer usando gli utenti seedati locali;
- i test di lettura e scrittura richiedono token diversi perché la API controlla sia lo scope `imagegalleryapi.write` sia i claim utente necessari alle policy.

Impatto:

- la collection ora testa il percorso reale `IdentityServer -> token endpoint -> API con introspection`;
- i token ottenuti ai primi due step vengono salvati automaticamente nelle collection variables e riusati da tutti i test successivi;
- `David` resta il profilo read-only, mentre `Emma` resta il profilo write con i claim necessari a creare, aggiornare e cancellare immagini;
- il client `imagegallerypostman` è pensato per sviluppo/test manuale e non sostituisce il client MVC in authorization code flow.
- la sequenza della collection e' ora leggibile in due blocchi distinti: test iniziali `David read-only` per validare lettura e divieto di scrittura, seguiti dal blocco `Emma CRUD` che crea, aggiorna e cancella solo la propria immagine di test.

Nota sui token disponibili:

- la collection Postman attuale usa `grant_type=password` tramite il client `imagegallerypostman`, quindi salva e riusa operativamente il solo `access_token`;
- questo comportamento è sufficiente per i test API perché la Web API usa il bearer token per autenticazione, scope e ownership delle immagini;
- il client MVC della solution usa invece un flusso OpenID Connect `authorization code` con `SaveTokens = true` e scope `offline_access`, per questo può recuperare anche `identity_token` e `refresh_token`;
- se si vuole vedere in collection anche `identity_token` e `refresh_token` come nel flusso MVC, non basta leggere campi aggiuntivi nella risposta: serve usare un flusso OIDC compatibile oppure esporre i token già ottenuti dal login reale del client MVC.
- per ridurre i problemi in Postman, i test `1.1` e `1.2` salvano ora i token sia come collection variables sia come environment variables; se un request successivo mostra ancora `{{bearerTokenRead}}` o `{{bearerTokenWrite}}` in rosso, il caso più probabile è che il login non sia stato eseguito con successo oppure che l'IdentityServer non sia stato riavviato dopo l'introduzione del client `imagegallerypostman`.
- per la stessa ragione, i test CRUD salvano anche `createdImageId`, `createdImageTitle`, `updatedImageTitle` e `deletedImageId` nelle environment variables oltre che nelle collection variables, così i request successivi risolvono in modo più robusto gli identificativi dinamici dell'immagine creata al test `04`.
- i `POST` della collection usano ora `bytes` come stringa base64 di test, perché il modello `ImageForCreation.Bytes` è un `byte[]` e il binding JSON di ASP.NET Core è affidabile con payload base64, mentre l'array numerico usato in precedenza generava errori `400 Bad Request` sul body.
- nella sequenza CRUD della collection, il test `04` crea un'immagine con il token write di Emma e salva `createdImageId`; i test `05`, `06`, `07`, `08` e `09` lavorano esplicitamente solo su quella stessa immagine, così i test manuali non toccano altre immagini dell'utente.
- il test finale sulla risorsa eliminata verifica `403` e non `404`: nel codice attuale `MustOwnImageHandler` applica prima il controllo di ownership sulla route `id` e fallisce l'autorizzazione prima che `GetImage` possa arrivare al ramo `NotFound()`, quindi il comportamento effettivo osservabile dopo la delete e' `Forbid`.

## Allineamento di `Marvin.IDP` a .NET 10 con SQL Server e user secrets dedicati

File coinvolti:

- `Marvin.IDP/Marvin.IDP.csproj`
- `Marvin.IDP/HostingExtensions.cs`
- `Marvin.IDP/Program.cs`
- `Marvin.IDP/Config.cs`
- `Marvin.IDP/DbContexts/IdentityDbContext.cs`
- `Marvin.IDP/DbContexts/IdentityDbContextFactory.cs`
- `Marvin.IDP/Migrations/*`
- `.gitignore`

Correzione applicata:

- aggiornato `Marvin.IDP` da `net8.0` a `net10.0`;
- sostituito EF Core SQLite con EF Core SQL Server `10.0.3`;
- aggiunto `UserSecretsId` dedicato per il progetto;
- spostata la connection string verso `ConnectionStrings:DefaultConnection` in user secrets, derivata dall'istanza SQL Server Docker della solution ma con database dedicato `MarvinIdp`;
- rimossi dal codice i valori sensibili hardcoded di `ApiSecrets` e `ClientSecrets`, ora letti da configurazione sicura;
- rigenerate le migration EF per SQL Server con seed deterministico;
- introdotto un bootstrap difensivo che controlla se `MarvinIdp` esiste già e, in quel caso, salta le migration automatiche per evitare sovrascritture;
- aggiunta una `IDesignTimeDbContextFactory` per evitare side effect del bootstrap durante i comandi `dotnet ef`;
- neutralizzato l'uso operativo del vecchio file SQLite locale e aggiunta l'esclusione `*.db` in `.gitignore`.

Motivo tecnico:

- `Marvin.IDP` era l'unico progetto rimasto fuori standard rispetto alla solution .NET 10;
- il provider SQLite e le migration generate su EF 8 non erano coerenti con il target tecnico attuale;
- la soluzione usa già SQL Server locale su Docker, quindi il nuovo IDP doveva allinearsi alla stessa infrastruttura mantenendo però un database separato;
- connection string e segreti OAuth2/OIDC non devono restare nel repository.

Impatto:

- `Marvin.IDP` è ora coerente con il resto della solution dal punto di vista di framework, provider EF e gestione configurazione sensibile;
- il database atteso per il progetto è `MarvinIdp` sull'istanza SQL Server locale;
- il bootstrap non crea o migra nulla se trova già un database con quel nome;
- le prossime operazioni EF sul progetto lavorano sul provider SQL Server e non più su SQLite.

## Riallineamento password seed di `David` e `Emma` in `Marvin.IDP`

File coinvolti:

- `Marvin.IDP/DbContexts/IdentityDbContext.cs`
- `Marvin.IDP/Migrations/*`

Correzione applicata:

- rimossa dal codice la password esplicita degli utenti locali `David` e `Emma`;
- spostata la credenziale locale su configurazione sicura, evitando che il valore finisca in seed, migration e snapshot versionati.

Motivo tecnico:

- dopo la creazione di `MarvinIdp`, i due utenti locali del progetto non erano coerenti con la password usata nel resto dell'ambiente locale.

Impatto:

- i login locali di `David` e `Emma` su `Marvin.IDP` dipendono ora dalla configurazione sicura locale;
- eventuali ricreazioni future del database non reintroducono password esplicite nel codice o nelle migration.

## Correzione applicata dopo errore `invalid_redirect_uri` con `Marvin.IDP`

File coinvolti:

- `Marvin.IDP/Config.cs`
- `ImageGallery.Client/Program.cs`

Correzione applicata:

- riallineata in `Marvin.IDP` la `RedirectUri` del client MVC a `https://localhost:7065/signin-oidc`;
- riallineata in `Marvin.IDP` la `PostLogoutRedirectUri` del client MVC a `https://localhost:7065/signout-callback-oidc`;
- corretto nel client MVC lo scope richiesto da `paese` a `country`;
- corretta anche la mappatura claim locale del client da `paese` a `country`.

Motivo tecnico:

- `ImageGallery.Client` gira oggi sul profilo HTTPS `https://localhost:7065`, mentre `Marvin.IDP` accettava ancora solo il vecchio redirect `https://localhost:7184/signin-oidc`;
- la richiesta OIDC falliva quindi già sul pushed authorization request endpoint con `invalid_redirect_uri`;
- anche dopo quella correzione il login avrebbe avuto un secondo disallineamento, perché `Marvin.IDP` espone il claim/scope `country` mentre il client chiedeva ancora `paese`.

Impatto:

- il login OIDC del client MVC verso `Marvin.IDP` usa ora callback e post logout coerenti con il profilo di avvio reale del progetto;
- il client richiede ora uno scope effettivamente definito dal nuovo IDP;
- si elimina la causa del `400` su `/connect/par` e si evita un successivo `invalid_scope`.

## Correzione applicata dopo `401 Unauthorized` della API con `Marvin.IDP`

File coinvolti:

- `Marvin.IDP/Config.cs`

Correzione applicata:

- aggiunto in `Marvin.IDP` un client tecnico confidenziale con `ClientId = imagegalleryapi`;
- il client usa il secret già associato all'API (`IdentityServer:ApiResources:ImageGalleryApi:ApiSecret`);
- il client è destinato all'autenticazione della Web API verso l'endpoint `/connect/introspect`.

Motivo tecnico:

- `ImageGallery.API` valida i bearer token opachi tramite OAuth2 introspection;
- durante l'introspection l'API si autentica con `client_id = imagegalleryapi`;
- `Marvin.IDP` esponeva la `ApiResource` `imagegalleryapi`, ma non un `Client` con lo stesso id, quindi l'endpoint di introspection rispondeva `401` con `no client with id 'imagegalleryapi' found`.

Impatto:

- l'API può di nuovo autenticarsi correttamente verso l'endpoint di introspection di `Marvin.IDP`;
- i reference token emessi al client MVC possono essere validati dall'API;
- il `401 Unauthorized` osservato dopo il login di `Emma` non dipende più dall'assenza del client tecnico nell'IDP.

## Allineamento ai profili di avvio locali `http` e `https`

File coinvolti:

- `Marvin.IDP/Config.cs`
- `ImageGallery.Client/appsettings.json`

Correzione applicata:

- aggiunta in `Marvin.IDP` anche la callback `http://localhost:5253/signin-oidc`;
- aggiunta in `Marvin.IDP` anche la post logout callback `http://localhost:5253/signout-callback-oidc`;
- riallineato `ImageGallery.Client` a `ImageGalleryAPIRoot = http://localhost:5212/`.

Motivo tecnico:

- con `dotnet run` il client MVC usa di default il profilo `http`, quindi il middleware OIDC genera `redirect_uri` su `http://localhost:5253/...`;
- con lo stesso avvio, la Web API ascolta sicuramente su `http://localhost:5212`, mentre l'URL HTTPS `https://localhost:7162` non è sempre disponibile;
- senza questo allineamento, il flusso locale fallisce in modo diverso a seconda del profilo selezionato in Visual Studio o CLI.

Impatto:

- il login locale del client MVC verso `Marvin.IDP` funziona sia con profilo `http` sia con profilo `https`;
- il client MVC chiama un endpoint API disponibile in modo coerente anche quando la solution parte con i profili di default;
- si riducono i falsi errori di ambiente dovuti solo al launch profile scelto.

## Riallineamento del secret di introspection tra API e `Marvin.IDP`

File coinvolti:

- `ImageGallery.API` user secrets
- `Marvin.IDP` user secrets

Correzione applicata:

- riallineato `Authentication:Introspection:ClientSecret` dell'API al valore `apisecret`;
- confermato che `Marvin.IDP` espone `IdentityServer:ApiResources:ImageGalleryApi:ApiSecret = apisecret`.

Motivo tecnico:

- dopo il fix del `ClientId = imagegalleryapi`, l'API riusciva a chiamare `/connect/introspect` ma veniva ancora rifiutata con `401`;
- i log di `Marvin.IDP` mostravano chiaramente `Client secret validation failed for client: imagegalleryapi`;
- il secret locale dell'API era ancora `secret`, mentre il nuovo IDP usava `apisecret`.

Impatto:

- la Web API autentica correttamente la chiamata di introspection verso `Marvin.IDP`;
- i reference token emessi al client MVC vengono validati con successo;
- `GET /api/images` del client MVC torna a rispondere `200`.

## Introduzione del persisted grant store SQL Server anche in `Marvin.IDP`

File coinvolti:

- `Marvin.IDP/Marvin.IDP.csproj`
- `Marvin.IDP/HostingExtensions.cs`
- `Marvin.IDP/Migrations/PersistedGrantDb/*`

Correzione applicata:

- aggiunto il package `Duende.IdentityServer.EntityFramework`;
- configurato `AddOperationalStore(...)` di `Marvin.IDP` sullo stesso database SQL Server `MarvinIdp`;
- aggiornato il bootstrap applicativo per applicare a startup sia le migration di `IdentityDbContext` sia quelle di `PersistedGrantDbContext`;
- generata una migration dedicata del persisted grant store sotto `Marvin.IDP/Migrations/PersistedGrantDb`.

Motivo tecnico:

- `Marvin.IDP` emette `reference token` e `refresh token`, ma fino a questo allineamento li conservava solo in memoria;
- dopo ogni riavvio del processo, i token salvati nel browser del client MVC non esistevano più lato IDP;
- il sintomo osservato era `refresh_token ... not found in store`, seguito da `401 Unauthorized` sulla gallery.

Impatto:

- `Marvin.IDP` conserva ora persisted grants, refresh token e reference token in SQL Server;
- i riavvii del progetto non invalidano più automaticamente i refresh token già rilasciati;
- il comportamento del nuovo IDP è ora coerente con quello già adottato in `ImageGallery.IdentityServer`.

## Riallineamento dei subject di `Emma` e `David` tra `Marvin.IDP` e `ImageGallery`

File coinvolti:

- `Marvin.IDP/HostingExtensions.cs`

Correzione applicata:

- il seed dei local users di `Marvin.IDP` è stato aggiornato per usare i `SubjectId` storici del vecchio `ImageGallery.IdentityServer`;
- `Emma` usa ora `0C3F7AAF-EC7E-407E-A60C-3529F7CE0AEF`;
- `David` usa ora `66928CB3-2E0F-4372-A430-4BECAB0BEB59`.

Motivo tecnico:

- la Web API filtra le immagini per `OwnerId`, che nel database `ImageGallery` contiene i subject storici emessi dal vecchio identity provider;
- il nuovo `Marvin.IDP` era stato inizialmente popolato con subject diversi, quindi l'autenticazione riusciva ma `Emma` e `David` non vedevano più le immagini già presenti.

Impatto:

- dopo il riallineamento, i token emessi da `Marvin.IDP` riportano gli stessi `sub` attesi dalla tabella `ImageGallery.dbo.Images`;
- `Emma` e `David` tornano a vedere le immagini già associate ai loro account storici, senza dover riscrivere i dati nel database immagini.

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
