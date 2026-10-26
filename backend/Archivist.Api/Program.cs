using Archivist.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// One HTTP client per external service, then the service that combines them.
builder.Services.AddHttpClient<DeezerClient>();
builder.Services.AddHttpClient<GeniusClient>();
builder.Services.AddScoped<IMusicMetadataService, MusicMetadataService>();

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

app.UseHttpsRedirection();
app.UseCors("AllowViteApp");
app.UseAuthorization();
app.MapControllers();

app.Run();