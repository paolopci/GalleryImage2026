using AutoMapper;
using ImageGallery.API.DbContext;
using ImageGallery.API.Services;
using ImageGallery.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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



// Disattiva la rimappatura automatica dei claim JWT verso i claim type Microsoft.
// In questo modo claim come "sub", "role", "name" e "scope"
// restano con i nomi originali presenti nel token.
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // L'API si fida dei token emessi dall'IdentityServer locale.
        options.Authority = "https://localhost:5001";

        // L'audience deve corrispondere al nome della ApiResource definita nell'IDP.
        // utile se in futuro vuoi aggiungere altre regole di validazione.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            // serve a verificare che il token sia destinato a questa API
            ValidAudience = "imagegalleryapi",

            // Indica quale claim del token deve essere usato come nome dell'utente
            // autenticato all'interno dell'applicazione.
            NameClaimType = "given_name",

            // Indica quale claim del token contiene i ruoli dell'utente,
            // così ASP.NET Core può usarli nelle autorizzazioni basate sui ruoli.
            RoleClaimType = "role"

        };
    });

// Registra sia la policy di accesso generale alla API, basata sullo scope OAuth2,
// sia la policy più restrittiva riusata per l'upload immagini.
builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy("ImageGalleryApiFullAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "imagegalleryapi.fullaccess");
    });

    authorizationOptions.AddPolicy("UserCanAddImage", AuthorizationPolicies.CanAddImage());
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
