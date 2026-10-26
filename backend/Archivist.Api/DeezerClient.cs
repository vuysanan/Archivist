using System.Text.Json.Nodes;

namespace Archivist.Api.Services
{
    // Deezer's public API is free and needs no key or account.
    // It supplies the basics: title, artist, cover art, release date, tracklist (and BPM when known).
    public class DeezerClient
    {
        private readonly HttpClient _http;

        public DeezerClient(HttpClient http)
        {
            _http = http;
        }

        private async Task<JsonNode> GetAsync(string path)
        {
            using var res = await _http.GetAsync("https://api.deezer.com/" + path);
            res.EnsureSuccessStatusCode();
            var json = JsonNode.Parse(await res.Content.ReadAsStringAsync())!;

            // Deezer reports some problems (like too many requests) as a normal 200 response with an "error" field.
            if (json["error"] is not null)
                throw new HttpRequestException($"Deezer error: {json["error"]}");

            return json;
        }

        // Searches, takes the top match, then loads its full record. Returns null if nothing matches.
        private async Task<JsonNode?> FindAsync(string query, string type)
        {
            var search = await GetAsync($"search/{type}?q={Uri.EscapeDataString(query)}&limit=1");
            var id = search["data"]?.AsArray().FirstOrDefault()?["id"]?.GetValue<long>();
            return id is null ? null : await GetAsync($"{type}/{id}");
        }

        public Task<JsonNode?> GetTrackAsync(string query) => FindAsync(query, "track");
        public Task<JsonNode?> GetAlbumAsync(string query) => FindAsync(query, "album");
    }
}