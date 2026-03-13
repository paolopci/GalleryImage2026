using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace ImageGallery.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
            { };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {      
            // questo è il setting che facciamo a lato IDP
            new Client()
            {
                ClientName="Image Gallery",
                ClientId="imagegalleryclient",
                AllowedGrantTypes=GrantTypes.Code,
                RedirectUris =
                {
                    // per la porta devi vedere il client project, appsettings https
                    "https://localhost:7065/signin-oidc"
                },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                },
                ClientSecrets =
                {
                    new Secret("secret".Sha256())
                },
                // una volta loggato aggiungo una schermata di consenso
                RequireConsent=true

            }
        };
}
