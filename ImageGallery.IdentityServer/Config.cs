using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace ImageGallery.IdentityServer;

public static class Config
{
    // Le IdentityResources rappresentano le informazioni identitarie
    // che l'IDP può rilasciare al client autenticato.
    // In questo esempio il client chiede:
    // - openid: obbligatoria per OpenID Connect e per ottenere l'identità dell'utente;
    // - profile: per ricevere claim base del profilo utente.
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),

            // Definisce una IdentityResource chiamata "roles" che espone il claim "role",
            // così i client che richiedono questo scope possono ricevere i ruoli dell'utente autenticato.
            new IdentityResource("roles", "Your role(s)", new []{"role"})
        };

    // Gli ApiScope descrivono i permessi delegati verso API protette.
    // Qui è vuoto perché il file, allo stato attuale, configura solo
    // l'autenticazione OIDC del client e non espone scope API aggiuntivi.
    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            // Questo scope rappresenta un permesso applicativo più esplicito:
            // il client che lo richiede ottiene accesso completo alle operazioni
            // esposte dall'API Image Gallery.
            new ApiScope("imagegalleryapi.fullaccess","Image Gallery API Full Access")
        };

    // Gli ApiResources rappresentano le API protette esposte dall'IdentityServer.
    // In questo caso definiamo la risorsa "imagegalleryapi" a cui i client possono
    // richiedere accesso tramite lo scope omonimo.
    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
        new ApiResource("imagegalleryapi", "Image Gallery API")
        {
            Scopes = { "imagegalleryapi.fullaccess" },
            UserClaims = { "given_name" }
        }
    };

    // La sezione Clients definisce le applicazioni che possono usare questo
    // IdentityServer come Identity Provider (IDP).
    // Ogni client ha credenziali, grant type consentiti, redirect URI e scope autorizzati.
    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Questo client rappresenta ImageGallery.Client, cioè l'app MVC
            // che delega il login all'IDP invece di autenticare l'utente localmente.
            new Client()
            {
                ClientName="Image Gallery",
                // Manteniamo lo stesso ClientId usato dal client MVC,
                // così l'IdentityServer riconosce correttamente il chiamante.
                ClientId="imagegalleryclient",

                // Authorization Code Flow:
                // il browser viene reindirizzato all'IDP per il login,
                // poi il client riceve un authorization code da scambiare con i token.
                AllowedGrantTypes=GrantTypes.Code,
                RedirectUris =
                {
                    // Endpoint di callback del client MVC.
                    // Dopo l'autenticazione presso l'IDP, l'utente torna qui
                    // con la risposta OIDC gestita dal middleware signin-oidc.
                    "https://localhost:7065/signin-oidc"
                },
                // Endpoint di callback eseguito dopo il logout presso l'IDP.
                // Il middleware signout-callback-oidc usa questo URI per completare
                // il flusso di sign-out e reindirizzare correttamente l'utente.
                PostLogoutRedirectUris =
                {
                    "https://localhost:7065/signout-callback-oidc"
                },


                AllowedScopes =
                {
                    // Scope identitari che il client è autorizzato a richiedere.
                    // openid abilita il protocollo OpenID Connect.
                    // profile permette il rilascio di claim di profilo base.
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "roles",
                    "imagegalleryapi.fullaccess"
                },
                ClientSecrets =
                {
                    // Segreto condiviso usato dal client confidenziale
                    // quando scambia l'authorization code con i token.
                    new Secret("secret".Sha256())
                },

                // Se true, l'IDP mostra all'utente una schermata di consenso
                // prima di rilasciare i claim/scope richiesti dal client.
                RequireConsent=true

            }
        };
}
