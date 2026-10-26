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
            var limit = type == "track" ? 10 : 1;
            var search = await GetAsync($"search/{type}?q={Uri.EscapeDataString(query)}&limit={limit}");
            var results = search["data"]?.AsArray() ?? new JsonArray();

            var best = type == "track" ? PickBestTrack(results, query) : results.FirstOrDefault();
            var id = best?["id"]?.GetValue<long>();
            return id is null ? null : await GetAsync($"{type}/{id}");
        }

        // Among results that really contain every word you typed (in the title or artist name),
        // prefer an exact title match, then the most popular one (Deezer's "rank").
        // This avoids picking a hit song that only mentions your search in a "feat." credit.
        // If nothing matches, fall back to Deezer's own top result.
        private static JsonNode? PickBestTrack(JsonArray results, string query)
        {
            var want = TextUtil.Normalize(query);
            var wantWords = want.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = results
                .Where(r => r is not null)
                .Select(r =>
                {
                    var title = TextUtil.Normalize(TextUtil.StripBrackets(r!["title"]?.GetValue<string>() ?? ""));
                    var artist = TextUtil.Normalize(r["artist"]?["name"]?.GetValue<string>() ?? "");
                    var words = $"{artist} {title}".Split(' ');
                    return new
                    {
                        Item = r,
                        Exact = title == want,
                        Matches = wantWords.All(w => words.Contains(w)),
                        Rank = r["rank"]?.GetValue<long>() ?? 0
                    };
                })
                .ToList();

            return scored.Where(x => x.Matches)
                         .OrderByDescending(x => x.Exact)
                         .ThenByDescending(x => x.Rank)
                         .Select(x => x.Item)
                         .FirstOrDefault()
                   ?? scored.FirstOrDefault()?.Item;
        }

        public Task<JsonNode?> GetTrackAsync(string query) => FindAsync(query, "track");
        public Task<JsonNode?> GetAlbumAsync(string query) => FindAsync(query, "album");
    }
}