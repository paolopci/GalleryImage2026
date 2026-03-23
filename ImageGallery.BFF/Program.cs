using Duende.Bff;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;

var builder = WebApplication.CreateBuilder(args);
var bffConfiguration = builder.Configuration.GetSection("Bff");

var identityServerAuthority = bffConfiguration["Authority"] ?? "https://localhost:5001";
var apiRoot = bffConfiguration["ApiBaseUrl"] ?? "https://localhost:7162";
var apiLocalPath = bffConfiguration["ApiLocalPath"] ?? "/api/{**catch-all}";
var bffClientId = bffConfiguration["ClientId"] ?? "imagegallerybff";
var bffClientSecret = bffConfiguration["ClientSecret"]
    ?? throw new InvalidOperationException("Missing configuration value 'Bff:ClientSecret'. Configure it via user secrets.");

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddControllersWithViews()
    .AddJsonOptions(configure =>
        configure.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDistributedMemoryCache();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "__Host-ImageGallery.Bff";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Authority = identityServerAuthority;
        options.ClientId = bffClientId;
        options.ClientSecret = bffClientSecret;
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("offline_access");
        options.Scope.Add("roles");
        options.Scope.Add("paese");
        options.Scope.Add("imagegalleryapi.read");
        options.Scope.Add("imagegalleryapi.write");

        options.ClaimActions.DeleteClaim("sid");
        options.ClaimActions.DeleteClaim("idp");
        options.ClaimActions.MapJsonKey("role", "role");
        options.ClaimActions.MapUniqueJsonKey("paese", "paese");

        options.TokenValidationParameters = new()
        {
            NameClaimType = "given_name",
            RoleClaimType = "role"
        };
    });

builder.Services
    .AddBff()
    .AddServerSideSessions();

builder.Services
    .AddReverseProxy()
    .AddBffExtensions();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseBff();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRemoteBffApiEndpoint(apiLocalPath, new Uri(apiRoot))
    .WithAccessToken(RequiredTokenType.User);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
