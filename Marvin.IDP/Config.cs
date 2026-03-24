using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;

namespace Marvin.IDP;

public static class Config
{
    public const string ApiSecretConfigurationKey = "IdentityServer:ApiResources:ImageGalleryApi:ApiSecret";
    public const string ClientSecretConfigurationKey = "IdentityServer:Clients:ImageGalleryClient:ClientSecret";
    public const string DavidPasswordConfigurationKey = "LocalUsers:David:Password";
    public const string EmmaPasswordConfigurationKey = "LocalUsers:Emma:Password";

    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        { 
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource("roles",
                "Your role(s)",
                new [] { "role" }),
            new IdentityResource("country",
                "The country you're living in",
                new List<string>() { "country" })
        };

    public static IEnumerable<ApiResource> ApiResources(IConfiguration configuration)
    {
        var apiSecret = GetRequiredConfiguration(configuration, ApiSecretConfigurationKey);

        return
     new ApiResource[]
         {
             new ApiResource("imagegalleryapi",
                 "Image Gallery API",
                 new [] { "role", "country" })
             {
                 Scopes = { "imagegalleryapi.fullaccess", 
                     "imagegalleryapi.read", 
                     "imagegalleryapi.write"},
                ApiSecrets = { new Secret(apiSecret.Sha256()) }
             }
         };
    }


    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
            { 
                new ApiScope("imagegalleryapi.fullaccess"),
                new ApiScope("imagegalleryapi.read"),
                new ApiScope("imagegalleryapi.write")};
 
    public static IEnumerable<Client> Clients(IConfiguration configuration)
    {
        var clientSecret = GetRequiredConfiguration(configuration, ClientSecretConfigurationKey);

        return
        new Client[] 
            {
                new Client()
                {
                    ClientName = "Image Gallery",
                    ClientId = "imagegalleryclient",
                    AllowedGrantTypes = GrantTypes.Code,
                    AccessTokenType = AccessTokenType.Reference,
                    AllowOfflineAccess = true,
                    UpdateAccessTokenClaimsOnRefresh = true,
                    AccessTokenLifetime = 120,
                    // AuthorizationCodeLifetime = ...
                    // IdentityTokenLifetime = ...
                    RedirectUris =
                    {
                        "https://localhost:7184/signin-oidc"
                    },
                    PostLogoutRedirectUris =
                    {
                        "https://localhost:7184/signout-callback-oidc"
                    },
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "roles",
                        //"imagegalleryapi.fullaccess",
                        "imagegalleryapi.read",
                        "imagegalleryapi.write",
                        "country"
                    },
                    ClientSecrets =
                    {
                        new Secret(clientSecret.Sha256())
                    }, 
                    RequireConsent = true
                }
            };
    }

    public static void ValidateRequiredConfiguration(IConfiguration configuration)
    {
        _ = GetRequiredConfiguration(configuration, "ConnectionStrings:DefaultConnection");
        _ = GetRequiredConfiguration(configuration, ApiSecretConfigurationKey);
        _ = GetRequiredConfiguration(configuration, ClientSecretConfigurationKey);
        _ = GetRequiredConfiguration(configuration, DavidPasswordConfigurationKey);
        _ = GetRequiredConfiguration(configuration, EmmaPasswordConfigurationKey);
    }

    private static string GetRequiredConfiguration(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is not configured.");
}
