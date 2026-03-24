using Duende.AccessTokenManagement.OpenIdConnect;
using ImageGallery.Authorization;
using ImageGallery.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
                .AddJsonOptions(configure => configure.JsonSerializerOptions.PropertyNamingPolicy = null);


// Evita la rimappatura automatica dei claim JWT ai claim type Microsoft;
// in questo modo i claim restano con i nomi originali del token (es. "sub", "role", "name").
/*

In pratica:

* quando ASP.NET Core legge un token JWT, alcuni claim standard come sub, role, name possono 
  essere rinominati automaticamente in URI lunghi come http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name;

* con Clear() questa conversione viene azzerata;
  i claim restano con i nomi originali presenti nel token, quindi è più semplice lavorare 
  in modo coerente con OAuth2/OpenID Connect e con i claim standard JWT.
  evita ambiguità tra nomi claim standard e nomi trasformati;
  rende più prevedibile la lettura di User.Claims;
  riduce problemi quando il client o l'IdentityServer si aspettano claim come sub, role, given_name, email.

*/
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

const string IdentityServerAuthority = "https://localhost:5001";
const string ImageGalleryClientId = "imagegalleryclient";
const string OpenIdConnectClientSecretConfigurationKey = "Authentication:OpenIdConnect:ClientSecret";

var imageGalleryClientSecret = builder.Configuration[OpenIdConnectClientSecretConfigurationKey]
    ?? throw new InvalidOperationException(
        $"Configuration value '{OpenIdConnectClientSecretConfigurationKey}' is not configured.");

// configure authentication per usare OpenIDConnect
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = "/Authentication/AccessDenied";
})
      .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
      {
          options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.Authority = IdentityServerAuthority;
          options.ClientId = ImageGalleryClientId;
          options.ClientSecret = imageGalleryClientSecret;
          options.ResponseType = "code";

          // questi sono gli ambiti che voglio richiedere, non serve aggiungerli sono
          // aggiunti dei default dal middleware
          //  options.Scope.Add("openid");
          //   options.Scope.Add("profile");

          // redirect sul nostro host seguito dalla porta vedi 
          // file Config.cs in ImageGallery.IdentityServer
          //  -->   "https://localhost:7065/signin-oidc"
          // anche questo è impostato di default dal middleware quindi lo commento
          // options.CallbackPath = new PathString("signin-oidc");
          // permette di salvare i token ricevuti dal file provider identità IDP 
          // per poterli usare in seguito.


          // c'è un valore predefinito che è host:port/signout-callback-oidc
          // quindi lo commento
          // options.SignedOutCallbackPath = new PathString("signout-callback");
          options.SaveTokens = true;

          // Se true, dopo il login il middleware chiama lo UserInfo endpoint dell'IDP
          // usando l'access token ricevuto, per recuperare claim aggiuntivi del profilo
          // utente e aggiungerli all'identità autenticata locale.
          options.GetClaimsFromUserInfoEndpoint = true;
          options.ClaimActions.Remove("aud");
          options.ClaimActions.DeleteClaim("sid");
          options.ClaimActions.DeleteClaim("idp");
          //options.Scope.Add("imagegalleryapi.fullaccess");
          options.Scope.Add("imagegalleryapi.read");
          options.Scope.Add("imagegalleryapi.write");
          options.Scope.Add("paese");  // voglio anche il paese ritorna da UserInfo
          options.Scope.Add("offline_access");// questo se voglio usare il Refresh Token

          // Richiede i ruoli dell'utente all'IdentityServer e mappa il campo JSON "role"
          // come claim locale, così il client può usarlo per autorizzazioni e controlli sui ruoli.
          options.Scope.Add("roles");
          // Mappa i campi JSON restituiti dall'IdentityServer in claim locali:
          // "role" serve per gestire autorizzazioni basate sui ruoli, mentre "paese"
          // aggiunge all'utente autenticato il valore del paese recuperato dallo UserInfo endpoint.
          options.ClaimActions.MapJsonKey("role", "role");
          options.ClaimActions.MapUniqueJsonKey("paese", "paese");

          options.TokenValidationParameters = new()
          {
              NameClaimType = "given_name",
              RoleClaimType = "role"
          };

      });

// Registra la gestione automatica dei token OIDC dell'utente autenticato,
// inclusi recupero, storage nella sessione e refresh dell'access token.
builder.Services.AddOpenIdConnectAccessTokenManagement();

builder.Services.AddHttpClient("TokenRevocationClient", client =>
{
    client.BaseAddress = new Uri(IdentityServerAuthority);
});

builder.Services.AddScoped<ITokenRevocationService, TokenRevocationService>();

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImageGalleryAPIRoot"]!);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
}).AddUserAccessTokenHandler();

// per usare la policy che ho aggiunto in ImageGallery.Authorization library
builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy("UserCanAddImage", AuthorizationPolicies.CanAddImage());
});



var app = builder.Build();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// aggiungo l'autenticazione OATH2 prima dell'autorizzazione
// inserisci dopo UseRouting e prima dellìUseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
