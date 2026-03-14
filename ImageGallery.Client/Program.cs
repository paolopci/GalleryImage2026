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

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImageGalleryAPIRoot"]!);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
});

// configure authentication per usare OpenIDConnect
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
}).AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
      .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
      {
          options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.Authority = "https://localhost:5001";
          options.ClientId = "imagegalleryclient";
          options.ClientSecret = "secret";
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
