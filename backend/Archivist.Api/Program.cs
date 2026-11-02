using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Archivist.Api.Data;
using Archivist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache(); // remembers recent search results

// One HTTP client per external service, then the service that combines them.
builder.Services.AddHttpClient<DeezerClient>();
builder.Services.AddHttpClient<GeniusClient>();
builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

// Community suggestions are saved in Postgres when a connection string is set.
// Without one (local development, tests) they are kept in memory and lost on restart.
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ArchivistDbContext>(o => o.UseNpgsql(ToNpgsql(connectionString)));
    builder.Services.AddScoped<ISuggestionStore, EfSuggestionStore>();
}
else
{
    builder.Services.AddSingleton<ISuggestionStore, InMemorySuggestionStore>();
}

// Render sits in front of the app, so this makes the app see each visitor's real IP address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Each visitor gets 30 requests per minute, so one person can't use up the shared Deezer/Genius limits.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1) }));

    // Stricter rule for the suggestion form: 5 per hour per visitor.
    options.AddPolicy("suggest", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromHours(1) }));
});

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                  ?? new[] { "http://localhost:5173" };

    options.AddPolicy("AllowViteApp", policy =>
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Create the suggestions table on first start. If the database is unreachable, the rest of the site still works.
if (!string.IsNullOrWhiteSpace(connectionString))
{
    try
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ArchivistDbContext>().Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not prepare the database. Suggestions won't work until it is reachable.");
    }
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowViteApp");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Accepts Neon's "postgresql://user:password@host/db" address as-is and converts it for Npgsql.
static string ToNpgsql(string connection)
{
    if (!connection.StartsWith("postgres://") && !connection.StartsWith("postgresql://")) return connection;

    var uri = new Uri(connection);
    var userInfo = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        SslMode = SslMode.Require
    }.ConnectionString;
}

// Lets the test project start the API in memory.
public partial class Program { }