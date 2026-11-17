using Archivist.Api.Models;
using Archivist.Api.Services;

namespace Archivist.Api.Tests;

public class CommunitySamplesTests
{
    private static TrackDetails Track(params Sample[] samples) => new()
    {
        Id = "1", Title = "DNA.", Artist = "Kendrick Lamar", AlbumArt = "", Year = 2017,
        Credits = new Credits { LeadProducer = "", CoProducers = new(), Writers = new(), MixingEngineer = "" },
        Samples = samples.ToList()
    };

    private static SampleSuggestion Suggestion(string title, string artist) => new()
    {
        SampleTitle = title, SampleArtist = artist, SampleYear = "1978", Type = "Sample",
        SampleKey = TextUtil.Key(artist, title)
    };

    [Fact]
    public void Approved_suggestions_are_added_and_marked_community_verified()
    {
        var merged = CommunitySamples.Merge(Track(), new[] { Suggestion("Mary Jane", "Rick James") });

        var added = Assert.Single(merged.Samples);
        Assert.True(added.CommunityVerified);
        Assert.Equal("Mary Jane", added.Title);
    }

    [Fact]
    public void A_sample_Genius_already_lists_is_not_added_again()
    {
        var genius = new Sample { Id = "1", Title = "Mary Jane (Live 1982)", Artist = "Rick James", Year = "1982", Type = "Sample" };

        var merged = CommunitySamples.Merge(Track(genius), new[] { Suggestion("Mary Jane", "Rick James") });

        Assert.Single(merged.Samples);
        Assert.False(merged.Samples[0].CommunityVerified);
    }

    [Fact]
    public void The_original_track_is_left_untouched()
    {
        var original = Track();

        CommunitySamples.Merge(original, new[] { Suggestion("Mary Jane", "Rick James") });

        Assert.Empty(original.Samples);
    }

    [Fact]
    public void With_nothing_to_add_the_same_track_comes_back()
    {
        var original = Track();

        Assert.Same(original, CommunitySamples.Merge(original, Array.Empty<SampleSuggestion>()));
    }
}