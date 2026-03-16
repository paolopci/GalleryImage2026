# IdentityServer

## Scopo

`ImageGallery.IdentityServer` gestisce autenticazione e autorizzazione OAuth2/OpenID Connect per il client MVC e per l'API.

## Componenti principali

- `Config.cs`: definizione di client, scope, identity resources e api resources.
- `Pages`: Razor Pages del server di identita per login, consenso, logout e diagnostica.
- `HostingExtensions`: configurazione dell'host e dei servizi applicativi.
- `wwwroot`: asset statici del portale di autenticazione.

## Responsabilita

- autenticare gli utenti;
- emettere token di accesso e identity token;
- gestire consenso e logout;
- centralizzare la configurazione dei client OAuth2/OIDC.

## Allineamento documentale

Per modifiche a token, claim, scope, client e risorse protette, aggiornare anche `READ_ME.md` nella root del repository come richiesto dalle linee guida del progetto.
