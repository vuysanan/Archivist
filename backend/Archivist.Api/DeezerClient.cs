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

        // Searches, picks the best match, then loads its full record. Returns null if nothing matches.
        private async Task<JsonNode?> FindAsync(string query, string type)
        {
            // For tracks, look at the top 5 results and keep the most popular one (Deezer's "rank").
            // That is usually the original release rather than a remaster, cover or karaoke version.
            var limit = type == "track" ? 5 : 1;
            var search = await GetAsync($"search/{type}?q={Uri.EscapeDataString(query)}&limit={limit}");

            var best = (search["data"]?.AsArray() ?? new JsonArray())
                .Where(r => r is not null)
                .OrderByDescending(r => r!["rank"]?.GetValue<long>() ?? 0)
                .FirstOrDefault();

            var id = best?["id"]?.GetValue<long>();
            return id is null ? null : await GetAsync($"{type}/{id}");
        }

        public Task<JsonNode?> GetTrackAsync(string query) => FindAsync(query, "track");
        public Task<JsonNode?> GetAlbumAsync(string query) => FindAsync(query, "album");
    }
}