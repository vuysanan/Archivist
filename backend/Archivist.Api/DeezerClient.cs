using System.Text.Json.Nodes;

namespace Archivist.Api.Services
{
    // Deezer's public API is free and needs no key or account.
    // It supplies the basics: title, artist, cover art, release date, tracklist (and BPM when known).
    public class DeezerClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<DeezerClient> _logger;

        // Karaoke and tribute uploads copy real song names, so we steer away from them.
        private static readonly string[] JunkPhrases =
        {
            "karaoke", "tribute", "instrumental", "originally performed",
            "made famous", "in the style of", "backing track"
        };

        public DeezerClient(HttpClient http, ILogger<DeezerClient> logger)
        {
            _http = http;
            _logger = logger;
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
            var search = await GetAsync($"search/{type}?q={Uri.EscapeDataString(query)}&limit=100");
            var results = search["data"]?.AsArray() ?? new JsonArray();

            // Shows up in the Render logs, so we can see exactly what Deezer sent back.
            _logger.LogInformation("Deezer {Type} search '{Query}' returned {Count} results. Top: {Top}",
                type, query, results.Count,
                string.Join(" | ", results.Take(8).Select(r => $"{r?["title"]} - {r?["artist"]?["name"]}")));

            var best = PickBest(results, query);
            var id = best?["id"]?.GetValue<long>();
            return id is null ? null : await GetAsync($"{type}/{id}");
        }

        // Chooses the result that best fits what was typed:
        //  1. It must contain every word you typed (in the title or artist name).
        //  2. Karaoke / tribute / instrumental uploads come last.
        //  3. Results whose artist name appears in your search come first.
        //  4. Then an exact title match, then the most popular (Deezer's "rank", tracks only).
        // If nothing contains every word, fall back to Deezer's own top non-karaoke result.
        public static JsonNode? PickBest(JsonArray results, string query, bool requireMatch = false)
        {
            var want = TextUtil.Normalize(query);
            var wantWords = want.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var scored = results
                .Where(r => r is not null)
                .Select(r =>
                {
                    var rawTitle = r!["title"]?.GetValue<string>() ?? "";
                    var title = TextUtil.Normalize(TextUtil.StripBrackets(rawTitle));
                    var artist = TextUtil.Normalize(r["artist"]?["name"]?.GetValue<string>() ?? "");
                    var everything = TextUtil.Normalize($"{rawTitle} {r["album"]?["title"]?.GetValue<string>()}");
                    var words = $"{artist} {title}".Split(' ');

                    return new
                    {
                        Item = r,
                        Matches = wantWords.All(w => words.Contains(w)),
                        Junk = JunkPhrases.Any(j => everything.Contains(j) && !want.Contains(j)),
                        ArtistTyped = artist.Length > 0 && $" {want} ".Contains($" {artist} "),
                        Exact = want == title || want == $"{artist} {title}" || want == $"{title} {artist}",
                        Rank = r["rank"]?.GetValue<long>() ?? 0
                    };
                })
                .ToList();

            if (!scored.Any(x => x.Matches))
            {
                if (requireMatch) return null;
                return (scored.FirstOrDefault(x => !x.Junk) ?? scored.FirstOrDefault())?.Item;
            }

            return scored.Where(x => x.Matches)
                         .OrderBy(x => x.Junk)
                         .ThenByDescending(x => x.ArtistTyped)
                         .ThenByDescending(x => x.Exact)
                         .ThenByDescending(x => x.Rank)
                         .Select(x => x.Item)
                         .First();
        }

        // Finds a 30-second preview clip for a song. Returns null if there isn't a clear match.
        // Preview links expire after a while, so these are fetched fresh when someone presses play.
        public async Task<string?> GetPreviewAsync(string query)
        {
            var search = await GetAsync($"search/track?q={Uri.EscapeDataString(query)}&limit=25");
            var best = PickBest(search["data"]?.AsArray() ?? new JsonArray(), query, requireMatch: true);
            var url = best?["preview"]?.GetValue<string>();
            return string.IsNullOrWhiteSpace(url) ? null : url;
        }

        public Task<JsonNode?> GetTrackAsync(string query) => FindAsync(query, "track");
        public Task<JsonNode?> GetAlbumAsync(string query) => FindAsync(query, "album");
    }
}