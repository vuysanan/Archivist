using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace Archivist.Api.Services
{
    // Genius holds the credits (producers, writers) and the sample relationships.
    public class GeniusClient
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public GeniusClient(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        private async Task<JsonNode> GetAsync(string path)
        {
            var token = _config["Genius:AccessToken"] ?? throw new InvalidOperationException("Genius:AccessToken is not set.");
            using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.genius.com/" + path);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await _http.SendAsync(req);
            res.EnsureSuccessStatusCode();
            return JsonNode.Parse(await res.Content.ReadAsStringAsync())!["response"]!;
        }

        // Finds the best match for "title artist", then loads the full song record.
        public async Task<JsonNode?> FindSongAsync(string title, string artist)
        {
            var search = await GetAsync($"search?q={Uri.EscapeDataString($"{title} {artist}")}");
            var id = search["hits"]?.AsArray().FirstOrDefault()?["result"]?["id"]?.GetValue<int>();
            if (id is null) return null;

            var song = await GetAsync($"songs/{id}");
            return song["song"];
        }
    }
}