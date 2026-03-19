using System.Security.Claims;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;

namespace ImageGallery.IdentityServer;

public static class TestUsers
{
    public static List<TestUser> Users
    {
        get
        {
            return new List<TestUser>
            {
                new TestUser
                {
                    SubjectId="66928CB3-2E0F-4372-A430-4BECAB0BEB59",
                    Username="David",
                    Password="Micene@65",

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
                    Password="Micene@65",

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
    }
}
