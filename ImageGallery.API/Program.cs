using AutoMapper;
using ImageGallery.API.Authorization;
using ImageGallery.API.DbContext;
using ImageGallery.API.Services;
using ImageGallery.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAutoMapper(_ => { }, AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddDbContext<GalleryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
builder.Services.AddScoped<IAuthorizationHandler, MustOwnImageHandler>();
builder.Services.AddHttpContextAccessor();



// Disattiva la rimappatura automatica dei claim JWT verso i claim type Microsoft.
// In questo modo claim come "sub", "role", "name" e "scope"
// restano con i nomi originali presenti nel token.
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // L'API si fida dei token JWT emessi dall'IdentityServer locale.
        options.Authority = "https://localhost:5001";

        // L'audience deve corrispondere al nome della ApiResource definita nell'IDP.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = "imagegalleryapi",
            NameClaimType = "given_name",
            RoleClaimType = "role"
        };
    });

// Registra sia la policy di accesso generale alla API, basata sullo scope OAuth2,
// sia la policy più restrittiva riusata per l'upload immagini.
builder.Services.AddAuthorization(authorizationOptions =>
{
    // Policy di accesso completo all'API.
    // Richiede un utente autenticato e lo scope OAuth2 dedicato al full access.
    authorizationOptions.AddPolicy("ImageGalleryApiFullAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "imagegalleryapi.fullaccess");
    });

    // Policy riusabile per consentire l'aggiunta di immagini.
    // I requisiti specifici sono centralizzati nella classe AuthorizationPolicies.
    authorizationOptions.AddPolicy("UserCanAddImage", AuthorizationPolicies.CanAddImage());

    // Policy destinata ai client applicativi autorizzati a operazioni di scrittura.
    // Verifica la presenza dello scope OAuth2 associato al write.
    authorizationOptions.AddPolicy("ClientApplicationCanWrite", policyBuilder =>
    {
        policyBuilder.RequireClaim("scope", "imagegalleryapi.write");
    });

    // Policy base per gli scenari in cui l'utente deve risultare proprietario della risorsa.
    // Qui viene imposto il prerequisito minimo di autenticazione; eventuali controlli
    // aggiuntivi sulla ownership possono essere applicati in handler o filtri dedicati.
    authorizationOptions.AddPolicy("MustOwnImage", policyBuilder =>
    {
        policyBuilder.RequireAuthenticatedUser();
        policyBuilder.AddRequirements(new MustOwnImageRequirement());
    });

});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GalleryDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    await dbContext.SeedImagesFromFolderAsync(app.Environment.WebRootPath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
