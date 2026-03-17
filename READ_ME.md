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
