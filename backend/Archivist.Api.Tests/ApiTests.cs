using System.Net;
using System.Net.Http.Json;
using Archivist.Api.Models;
using Archivist.Api.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Archivist.Api.Tests;

// Starts the whole API in memory, but swaps Deezer and Genius for a fake,
// so these tests need no internet and use none of your API limits.
public class ApiTests
{
    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IMusicMetadataService>();
                services.AddScoped<IMusicMetadataService, FakeMetadataService>();
            }));

    private class FakeMetadataService : IMusicMetadataService
    {
        public Task<TrackDetails?> GetTrackMetadataAsync(string query) =>
            Task.FromResult<TrackDetails?>(query == "nothing" ? null : new TrackDetails
            {
                Id = "1",
                Title = query,
                Artist = "Test Artist",
                AlbumArt = "",
                Year = 2020,
                Credits = new Credits { LeadProducer = "Test Producer", CoProducers = new(), Writers = new(), MixingEngineer = "" },
                Samples = new()
            });

        public Task<AlbumDetails?> GetAlbumMetadataAsync(string query) =>
            Task.FromResult<AlbumDetails?>(query == "nothing" ? null : new AlbumDetails
            {
                Id = "1",
                Title = query,
                Artist = "Test Artist",
                Tracks = new() { new AlbumTrack { Number = 1, Title = "First" }, new AlbumTrack { Number = 2, Title = "Second" } }
            });
    }

    [Fact]
    public async Task Search_returns_the_track()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/archive/search?q=DNA.");
        var track = await response.Content.ReadFromJsonAsync<TrackDetails>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("DNA.", track!.Title);
        Assert.Equal("Test Producer", track.Credits.LeadProducer);
    }

    [Fact]
    public async Task Album_search_returns_the_tracklist()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var album = await client.GetFromJsonAsync<AlbumDetails>("/api/archive/album?q=DAMN.");

        Assert.Equal(2, album!.Tracks.Count);
        Assert.Equal("First", album.Tracks[0].Title);
    }

    [Theory]
    [InlineData("/api/archive/search?q=")]
    [InlineData("/api/archive/search?q=%20%20")]
    [InlineData("/api/archive/album?q=")]
    public async Task An_empty_search_is_rejected(string url)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/archive/search?q=nothing")]
    [InlineData("/api/archive/album?q=nothing")]
    public async Task A_search_with_no_match_returns_404(string url)
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Too_many_requests_get_a_429()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var last = HttpStatusCode.OK;
        for (var i = 0; i < 31; i++) // the limit is 30 per minute
            last = (await client.GetAsync("/api/archive/search?q=DNA.")).StatusCode;

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }

    [Fact]
    public async Task Only_the_configured_site_is_allowed_by_CORS()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        async Task<bool> IsAllowed(string origin)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/archive/search?q=DNA.");
            request.Headers.Add("Origin", origin);
            var response = await client.SendAsync(request);
            return response.Headers.Contains("Access-Control-Allow-Origin");
        }

        Assert.True(await IsAllowed("http://localhost:5173"));
        Assert.False(await IsAllowed("https://some-other-site.example"));
    }
}