using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Archivist.Api.Tests;

// Community suggestions: submit, hold as pending, approve or reject. Uses the in-memory store, no database needed.
public class SuggestionTests
{
    private static WebApplicationFactory<Program> CreateFactory(string? adminKey = "test-key") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            if (adminKey is not null) builder.UseSetting("Admin:Key", adminKey);
        });

    private static object ValidSuggestion(string? website = null) => new
    {
        trackTitle = "DNA.",
        trackArtist = "Kendrick Lamar",
        sampleTitle = "Mary Jane",
        sampleArtist = "Rick James",
        sampleYear = "1978",
        type = "Sample",
        website
    };

    private static HttpRequestMessage AdminRequest(HttpMethod method, string url, string key = "test-key")
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Admin-Key", key);
        return request;
    }

    private static async Task<JsonElement> PendingAsync(HttpClient client)
    {
        var response = await client.SendAsync(AdminRequest(HttpMethod.Get, "/api/admin/suggestions"));
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task A_new_suggestion_is_accepted_and_waits_as_pending()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/suggestions", ValidSuggestion());
        var pending = await PendingAsync(client);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, pending.GetArrayLength());
        Assert.Equal("Mary Jane", pending[0].GetProperty("sampleTitle").GetString());
    }

    [Fact]
    public async Task A_filled_in_honeypot_is_ignored_without_telling_the_bot()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/suggestions", ValidSuggestion(website: "http://spam.example"));
        var pending = await PendingAsync(client);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, pending.GetArrayLength());
    }

    [Fact]
    public async Task The_same_suggestion_is_not_stored_twice()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/suggestions", ValidSuggestion());
        await client.PostAsJsonAsync("/api/suggestions", ValidSuggestion());
        var pending = await PendingAsync(client);

        Assert.Equal(1, pending.GetArrayLength());
    }

    [Fact]
    public async Task A_suggestion_with_missing_details_is_rejected()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/suggestions", new { trackTitle = "DNA.", trackArtist = "Kendrick Lamar" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Approving_moves_a_suggestion_out_of_the_pending_list()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/suggestions", ValidSuggestion());
        var id = (await PendingAsync(client))[0].GetProperty("id").GetGuid();

        var approve = await client.SendAsync(AdminRequest(HttpMethod.Post, $"/api/admin/suggestions/{id}/approve"));
        var pending = await PendingAsync(client);

        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);
        Assert.Equal(0, pending.GetArrayLength());
    }

    [Fact]
    public async Task Admin_pages_need_the_right_key()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var noKey = await client.GetAsync("/api/admin/suggestions");
        var wrongKey = await client.SendAsync(AdminRequest(HttpMethod.Get, "/api/admin/suggestions", key: "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, noKey.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongKey.StatusCode);
    }

    [Fact]
    public async Task Admin_is_switched_off_when_no_key_is_configured()
    {
        await using var factory = CreateFactory(adminKey: null);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/admin/suggestions");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}