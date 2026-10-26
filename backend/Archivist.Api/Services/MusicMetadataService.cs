using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Archivist.Api.Models;

namespace Archivist.Api.Services
{
    public class MusicMetadataService : IMusicMetadataService
    {
        private readonly DeezerClient _deezer;
        private readonly GeniusClient _genius;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MusicMetadataService> _logger;

        public MusicMetadataService(DeezerClient deezer, GeniusClient genius, IMemoryCache cache, ILogger<MusicMetadataService> logger)
        {
            _deezer = deezer;
            _genius = genius;
            _cache = cache;
            _logger = logger;
        }

        // Results are remembered for 12 hours, so repeat searches cost nothing and don't use up API limits.
        private static string Key(string kind, string query) => $"{kind}:{query.Trim().ToLowerInvariant()}";

        public Task<TrackDetails?> GetTrackMetadataAsync(string query) =>
            _cache.GetOrCreateAsync(Key("track", query), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                var (track, geniusFailed) = await LoadTrackAsync(query);
                // If Genius had a hiccup, only remember the result briefly so credits can fill in soon.
                if (geniusFailed) entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return track;
            });

        public Task<AlbumDetails?> GetAlbumMetadataAsync(string query) =>
            _cache.GetOrCreateAsync(Key("album", query), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return await LoadAlbumAsync(query);
            });

        private async Task<(TrackDetails? Track, bool GeniusFailed)> LoadTrackAsync(string query)
        {
            // 1. Deezer gives the basics: title, artist, cover art, year, BPM (when it has one).
            var item = await _deezer.GetTrackAsync(query);
            if (item is null) return (null, false);

            var title = item["title"]?.GetValue<string>() ?? query;
            var artist = item["artist"]?["name"]?.GetValue<string>() ?? "";

            var track = new TrackDetails
            {
                Id = item["id"]?.ToString() ?? "",
                Title = title,
                Artist = artist,
                AlbumArt = item["album"]?["cover_xl"]?.GetValue<string>() ?? "",
                Year = ParseYear(item["release_date"]?.GetValue<string>()),
                Bpm = (int)Math.Round(item["bpm"]?.GetValue<double>() ?? 0), // 0 = unknown, the UI hides it
                Credits = new Credits { LeadProducer = "", CoProducers = new(), Writers = new(), MixingEngineer = "" },
                Samples = new()
            };

            // 2. Genius adds credits and samples. If it fails, still show the Deezer basics.
            var geniusFailed = false;
            try
            {
                var song = await _genius.FindSongAsync(title, artist);
                if (song is not null) FillFromGenius(track, song);
            }
            catch (Exception ex)
            {
                geniusFailed = true;
                _logger.LogWarning(ex, "Genius lookup failed for {Query}", query);
            }

            return (track, geniusFailed);
        }

        private async Task<AlbumDetails?> LoadAlbumAsync(string query)
        {
            var album = await _deezer.GetAlbumAsync(query);
            if (album is null) return null;

            var tracks = (album["tracks"]?["data"]?.AsArray() ?? new JsonArray())
                .Where(t => t is not null)
                .Select((t, i) => new AlbumTrack { Number = i + 1, Title = t!["title"]?.GetValue<string>() ?? "" })
                .ToList();

            return new AlbumDetails
            {
                Id = album["id"]?.ToString() ?? "",
                Title = album["title"]?.GetValue<string>() ?? query,
                Artist = album["artist"]?["name"]?.GetValue<string>() ?? "",
                AlbumArt = album["cover_xl"]?.GetValue<string>() ?? "",
                Year = ParseYear(album["release_date"]?.GetValue<string>()),
                Tracks = tracks
            };
        }

        private static void FillFromGenius(TrackDetails track, JsonNode song)
        {
            var producers = Names(song["producer_artists"]);
            track.Credits.LeadProducer = producers.FirstOrDefault() ?? "";
            track.Credits.CoProducers = producers.Skip(1).ToList();
            track.Credits.Writers = Names(song["writer_artists"]);

            var mix = song["custom_performances"]?.AsArray().FirstOrDefault(p =>
                p?["label"]?.GetValue<string>()?.Contains("Mix", StringComparison.OrdinalIgnoreCase) == true);
            track.Credits.MixingEngineer = string.Join(", ", Names(mix?["artists"]));

            foreach (var rel in song["song_relationships"]?.AsArray() ?? new JsonArray())
            {
                var type = rel?["relationship_type"]?.GetValue<string>() ?? rel?["type"]?.GetValue<string>();
                if (type is not ("samples" or "interpolates")) continue;

                foreach (var s in rel!["songs"]?.AsArray() ?? new JsonArray())
                {
                    if (s is null) continue;
                    track.Samples.Add(new Sample
                    {
                        Id = s["id"]?.ToString() ?? Guid.NewGuid().ToString(),
                        Title = s["title"]?.GetValue<string>() ?? "",
                        Artist = s["artist_names"]?.GetValue<string>() ?? "",
                        Year = s["release_date_components"]?["year"]?.ToString() ?? "",
                        Type = type == "samples" ? "Sample" : "Interpolation"
                    });
                }
            }
        }

        private static List<string> Names(JsonNode? array) =>
            (array?.AsArray() ?? new JsonArray())
                .Select(a => a?["name"]?.GetValue<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .ToList();

        // Dates look like "2017-04-14", "2017-04" or "2017".
        private static int ParseYear(string? date) =>
            date is { Length: >= 4 } && int.TryParse(date[..4], out var y) ? y : 0;
    }
}