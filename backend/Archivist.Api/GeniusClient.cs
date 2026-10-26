using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

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

        // Looks through the top search results for one whose title AND artist really match.
        // Returns null when nothing matches, so we show "Not listed" instead of someone else's credits.
        public async Task<JsonNode?> FindSongAsync(string title, string artist)
        {
            var cleanTitle = CleanTitle(title);
            var search = await GetAsync($"search?q={Uri.EscapeDataString($"{cleanTitle} {artist}")}");

            var wantTitle = TextUtil.Normalize(TextUtil.StripBrackets(cleanTitle));
            var wantArtist = TextUtil.Normalize(artist);

            foreach (var hit in search["hits"]?.AsArray() ?? new JsonArray())
            {
                var result = hit?["result"];
                if (result is null) continue;

                var hitTitle = TextUtil.Normalize(TextUtil.StripBrackets(result["title"]?.GetValue<string>() ?? ""));
                var hitArtists = TextUtil.Normalize(result["artist_names"]?.GetValue<string>()
                                           ?? result["primary_artist"]?["name"]?.GetValue<string>() ?? "");

                var titleOk = hitTitle == wantTitle || hitTitle.StartsWith(wantTitle + " ");
                var artistOk = hitArtists.Contains(wantArtist);
                if (!titleOk || !artistOk) continue;

                var id = result["id"]?.GetValue<int>();
                if (id is null) continue;

                var song = await GetAsync($"songs/{id}");
                return song["song"];
            }

            return null;
        }

        // "Juicy (2005 Remaster)" -> "Juicy", "Song - Radio Edit" -> "Song"
        private static readonly Regex Noise = new(
            @"\s*[\(\[][^\)\]]*(remaster|version|edit|mix|mono|stereo|live|deluxe|explicit|radio|feat|ft\.)[^\)\]]*[\)\]]" +
            @"|\s+-\s+.*(remaster|version|edit|mix|mono|stereo|live|radio).*$",
            RegexOptions.IgnoreCase);

        private static string CleanTitle(string title) => Noise.Replace(title, "").Trim();
    }
}