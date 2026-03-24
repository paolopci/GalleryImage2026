using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;
using Microsoft.Extensions.Configuration;

namespace ImageGallery.IdentityServer;

public static class TestUsers
{
    public const string DavidPasswordConfigurationKey = "IdentityServer:TestUsers:David:Password";
    public const string EmmaPasswordConfigurationKey = "IdentityServer:TestUsers:Emma:Password";

    public static List<TestUser> Users(IConfiguration configuration)
    {
        var davidPassword = GetRequiredConfiguration(configuration, DavidPasswordConfigurationKey);
        var emmaPassword = GetRequiredConfiguration(configuration, EmmaPasswordConfigurationKey);

        return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId="66928CB3-2E0F-4372-A430-4BECAB0BEB59",
                    Username="David",
                    Password=davidPassword,

                    Claims=new List<Claim>
                    {
                        new Claim("role","FreeUser"),
                        new Claim(JwtClaimTypes.GivenName,"David"),
                        new Claim(JwtClaimTypes.FamilyName,"Flagg"),
                        new Claim("paese","nl")
                    }
                },
                new TestUser
                {
                    SubjectId="0C3F7AAF-EC7E-407E-A60C-3529F7CE0AEF",
                    Username="Emma",
                    Password=emmaPassword,

                    Claims=new List<Claim>
                    {
                        new Claim("role","PayingUser"),
                        new Claim(JwtClaimTypes.GivenName,"Emma"),
                        new Claim(JwtClaimTypes.FamilyName,"Flagg"),
                          new Claim("paese","be")
                    }
                }
            };
    }

    public static void ValidateRequiredConfiguration(IConfiguration configuration)
    {
        _ = GetRequiredConfiguration(configuration, DavidPasswordConfigurationKey);
        _ = GetRequiredConfiguration(configuration, EmmaPasswordConfigurationKey);
    }

    private static string GetRequiredConfiguration(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is not configured.");
}
