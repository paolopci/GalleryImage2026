using Duende.IdentityServer.EntityFramework.DbContexts;
using Marvin.IDP.DbContexts;
using Marvin.IDP.Entities;
using Marvin.IDP.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Marvin.IDP;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(
        this WebApplicationBuilder builder)
    {
        Config.ValidateRequiredConfiguration(builder.Configuration);

        // uncomment if you want to add a UI
        builder.Services.AddRazorPages();

        builder.Services.AddScoped<ILocalUserService, LocalUserService>();

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        builder.Services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        builder.Services.AddIdentityServer(options =>
            {
                // https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/api_scopes#authorization-based-on-scopes
                options.EmitStaticAudienceClaim = true;
            })
            .AddProfileService<LocalUserProfileService>()
            .AddInMemoryIdentityResources(Config.IdentityResources)
            .AddInMemoryApiScopes(Config.ApiScopes)
            .AddInMemoryApiResources(Config.ApiResources(builder.Configuration))
            .AddInMemoryClients(Config.Clients(builder.Configuration))
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = dbContextBuilder =>
                    dbContextBuilder.UseSqlServer(
                        connectionString,
                        sqlServerOptions =>
                            sqlServerOptions.MigrationsAssembly(typeof(HostingExtensions).Assembly.GetName().Name));
            });

        return builder.Build();
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 
        app.UseSerilogRequestLogging();
    
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // uncomment if you want to add a UI
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        // uncomment if you want to add a UI
        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var connectionString = app.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(sqlConnectionBuilder.InitialCatalog))
        {
            throw new InvalidOperationException("The SQL Server database name for Marvin.IDP is not configured.");
        }

        var targetDatabaseName = sqlConnectionBuilder.InitialCatalog;
        var masterConnectionBuilder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        };

        await using var masterConnection = new SqlConnection(masterConnectionBuilder.ConnectionString);
        await masterConnection.OpenAsync();

        await using var command = masterConnection.CreateCommand();
        command.CommandText = "SELECT CAST(CASE WHEN DB_ID(@databaseName) IS NULL THEN 0 ELSE 1 END AS bit);";
        command.Parameters.AddWithValue("@databaseName", targetDatabaseName);

        var databaseExists = (bool?)await command.ExecuteScalarAsync() ?? false;
        await using var scope = app.Services.CreateAsyncScope();
        var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var persistedGrantDbContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();

        if (databaseExists)
        {
            Log.Information(
                "The SQL Server database {DatabaseName} already exists. Applying pending Marvin.IDP migrations without recreating the database.",
                targetDatabaseName);
        }
        else
        {
            Log.Information(
                "The SQL Server database {DatabaseName} was not found. Creating the database and applying Marvin.IDP migrations.",
                targetDatabaseName);
        }

        await identityDbContext.Database.MigrateAsync();
        await persistedGrantDbContext.Database.MigrateAsync();

        Log.Information("The SQL Server database {DatabaseName} has been migrated for both local users and persisted grants.", targetDatabaseName);
    }

    public static async Task SeedLocalUsersAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var users = CreateLocalUsers(app.Configuration);
        var claims = CreateLocalUserClaims();

        foreach (var user in users)
        {
            var existingUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existingUser == null)
            {
                dbContext.Users.Add(user);
                continue;
            }

            existingUser.Subject = user.Subject;
            existingUser.UserName = user.UserName;
            existingUser.Password = user.Password;
            existingUser.Active = user.Active;
            existingUser.ConcurrencyStamp = user.ConcurrencyStamp;
        }

        foreach (var claim in claims)
        {
            var existingClaim = await dbContext.UserClaims.FirstOrDefaultAsync(c => c.Id == claim.Id);
            if (existingClaim == null)
            {
                dbContext.UserClaims.Add(claim);
                continue;
            }

            existingClaim.UserId = claim.UserId;
            existingClaim.Type = claim.Type;
            existingClaim.Value = claim.Value;
            existingClaim.ConcurrencyStamp = claim.ConcurrencyStamp;
        }

        await dbContext.SaveChangesAsync();
    }

    private static IReadOnlyCollection<User> CreateLocalUsers(IConfiguration configuration) =>
    [
        new User
        {
            Id = new Guid("13229d33-99e0-41b3-b18d-4f72127e3971"),
            Password = GetRequiredConfiguration(configuration, Config.DavidPasswordConfigurationKey),
            Subject = "66928CB3-2E0F-4372-A430-4BECAB0BEB59",
            UserName = "David",
            Active = true,
            ConcurrencyStamp = "ac7a96e5-1a77-4fd5-8c70-7b98958522dd"
        },
        new User
        {
            Id = new Guid("96053525-f4a5-47ee-855e-0ea77fa6c55a"),
            Password = GetRequiredConfiguration(configuration, Config.EmmaPasswordConfigurationKey),
            Subject = "0C3F7AAF-EC7E-407E-A60C-3529F7CE0AEF",
            UserName = "Emma",
            Active = true,
            ConcurrencyStamp = "af0346be-1962-4a5f-802d-b06abffa4dbc"
        }
    ];

    private static IReadOnlyCollection<UserClaim> CreateLocalUserClaims() =>
    [
        new UserClaim
        {
            Id = new Guid("7db30136-34c6-4251-8a9f-264674d206cd"),
            UserId = new Guid("13229d33-99e0-41b3-b18d-4f72127e3971"),
            Type = "given_name",
            Value = "David",
            ConcurrencyStamp = "3af9e238-d76e-4f27-b34e-e30df6816abe"
        },
        new UserClaim
        {
            Id = new Guid("832a16ef-96d5-4b2c-96bf-bc65f32b9cff"),
            UserId = new Guid("13229d33-99e0-41b3-b18d-4f72127e3971"),
            Type = "family_name",
            Value = "Flagg",
            ConcurrencyStamp = "4c554e1b-a056-495f-ba9a-e04b26e354c9"
        },
        new UserClaim
        {
            Id = new Guid("67955dba-061e-4c42-8236-9cb48e89ebed"),
            UserId = new Guid("13229d33-99e0-41b3-b18d-4f72127e3971"),
            Type = "country",
            Value = "nl",
            ConcurrencyStamp = "9b5cba36-3095-4b21-90c8-8fa20b412c19"
        },
        new UserClaim
        {
            Id = new Guid("e3fce794-44ef-4f7a-9f65-c0e9633dba38"),
            UserId = new Guid("13229d33-99e0-41b3-b18d-4f72127e3971"),
            Type = "role",
            Value = "FreeUser",
            ConcurrencyStamp = "0e3f227a-4276-456d-9e24-a7fb20716468"
        },
        new UserClaim
        {
            Id = new Guid("b623e5f0-262f-4a83-ae1e-846f7ac51592"),
            UserId = new Guid("96053525-f4a5-47ee-855e-0ea77fa6c55a"),
            Type = "given_name",
            Value = "Emma",
            ConcurrencyStamp = "e7de9d3e-aa13-4b29-9294-7e887915b582"
        },
        new UserClaim
        {
            Id = new Guid("4be7f198-4bd3-4746-961f-cb316fd22666"),
            UserId = new Guid("96053525-f4a5-47ee-855e-0ea77fa6c55a"),
            Type = "family_name",
            Value = "Flagg",
            ConcurrencyStamp = "71cb86f0-544c-4b04-935f-debd83675369"
        },
        new UserClaim
        {
            Id = new Guid("45de91c1-e4fc-4e75-90eb-0124d566baac"),
            UserId = new Guid("96053525-f4a5-47ee-855e-0ea77fa6c55a"),
            Type = "country",
            Value = "be",
            ConcurrencyStamp = "ec6af68a-3a9a-4454-8bb8-30ed737fbf1a"
        },
        new UserClaim
        {
            Id = new Guid("6f5f4520-c2e6-4245-846f-f1645bc3a06a"),
            UserId = new Guid("96053525-f4a5-47ee-855e-0ea77fa6c55a"),
            Type = "role",
            Value = "PayingUser",
            ConcurrencyStamp = "9b14a46b-d4f3-496c-ba2b-6f59d8455555"
        }
    ];

    private static string GetRequiredConfiguration(IConfiguration configuration, string key) =>
        configuration[key] ?? throw new InvalidOperationException($"Configuration value '{key}' is not configured.");
}
