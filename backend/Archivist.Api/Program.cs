using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Archivist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddMemoryCache(); // remembers recent search results

// One HTTP client per external service, then the service that combines them.
builder.Services.AddHttpClient<DeezerClient>();
builder.Services.AddHttpClient<GeniusClient>();
builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

// Render sits in front of the app, so this makes the app see each visitor's real IP address.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Each visitor gets 30 requests per minute, so one person can't use up the shared Deezer/Genius limits.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1)
            }));
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

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("AllowViteApp");
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Lets the test project start the API in memory.
public partial class Program { }