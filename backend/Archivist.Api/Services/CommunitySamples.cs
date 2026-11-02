using Archivist.Api.Models;

namespace Archivist.Api.Services
{
    public static class CommunitySamples
    {
        // Returns a copy of the track with approved community samples added (marked as verified).
        // Samples already listed by Genius are skipped. The original (cached) track is never changed.
        public static TrackDetails Merge(TrackDetails track, IEnumerable<SampleSuggestion> approved)
        {
            var known = track.Samples.Select(s => TextUtil.Key(s.Artist, s.Title)).ToHashSet();

            var extra = approved
                .Where(a => known.Add(a.SampleKey))
                .Select(a => new Sample
                {
                    Id = $"community-{a.Id}",
                    Title = a.SampleTitle,
                    Artist = a.SampleArtist,
                    Year = a.SampleYear,
                    Type = a.Type,
                    CommunityVerified = true
                })
                .ToList();

            if (extra.Count == 0) return track;

            return new TrackDetails
            {
                Id = track.Id,
                Title = track.Title,
                Artist = track.Artist,
                AlbumArt = track.AlbumArt,
                Year = track.Year,
                Bpm = track.Bpm,
                Credits = track.Credits,
                Samples = track.Samples.Concat(extra).ToList()
            };
        }
    }
}