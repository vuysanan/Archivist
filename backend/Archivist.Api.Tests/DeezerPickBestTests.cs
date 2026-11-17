using System.Text.Json.Nodes;
using Archivist.Api.Services;

namespace Archivist.Api.Tests;

public class DeezerPickBestTests
{
    // Builds one fake Deezer search result.
    private static string Item(int id, string title, string artist, string album = "", long rank = 0) =>
        $$"""{ "id": {{id}}, "title": "{{title}}", "artist": { "name": "{{artist}}" }, "album": { "title": "{{album}}" }, "rank": {{rank}} }""";

    private static JsonArray Results(params string[] items) =>
        JsonNode.Parse("[" + string.Join(",", items) + "]")!.AsArray();

    private static long PickedId(JsonArray results, string query) =>
        DeezerClient.PickBest(results, query)!["id"]!.GetValue<long>();

    [Fact]
    public void Picks_the_real_song_over_a_karaoke_copy()
    {
        var results = Results(
            Item(1, "Love in the Dark (Originally Performed by Adele) [Karaoke Instrumental]", "Delayed Karma", "25 Karaoke Tribute to Adele", 900000),
            Item(2, "Love in the Dark", "Adele", "25", 500000));

        Assert.Equal(2L, PickedId(results, "adele Love In The Dark"));
    }

    [Fact]
    public void Skips_a_hit_that_only_features_the_searched_name()
    {
        // "Dark Horse" credits Juicy J, but it is not a song called "Juicy".
        var results = Results(
            Item(1, "Dark Horse (feat. Juicy J)", "Katy Perry", "Prism", 999999),
            Item(2, "Juicy", "The Notorious B.I.G.", "Ready to Die", 100));

        Assert.Equal(2L, PickedId(results, "Juicy"));
    }

    [Fact]
    public void Prefers_the_artist_you_typed_over_a_lookalike_upload()
    {
        var results = Results(
            Item(1, "25 Adele", "T_y_A"),
            Item(2, "25", "Adele"));

        Assert.Equal(2L, PickedId(results, "adele 25"));
    }

    [Fact]
    public void Picks_the_most_popular_when_titles_tie()
    {
        var results = Results(
            Item(1, "Juicy", "Doja Cat", "", 100),
            Item(2, "Juicy", "The Notorious B.I.G.", "", 900));

        Assert.Equal(2L, PickedId(results, "juicy"));
    }

    [Fact]
    public void Falls_back_to_the_first_non_karaoke_result_when_nothing_matches()
    {
        var results = Results(
            Item(1, "Love in the Dark (Originally Performed by Adele) [Karaoke Instrumental]", "Delayed Karma", "25 Karaoke Tribute to Adele"),
            Item(2, "Love in the Dark", "Someone Else"));

        Assert.Equal(2L, PickedId(results, "adele love in the dark"));
    }

    [Fact]
    public void Returns_null_when_there_are_no_results()
    {
        Assert.Null(DeezerClient.PickBest(new JsonArray(), "anything"));
    }

    [Fact]
    public void For_previews_a_wrong_song_is_better_than_no_match_so_strict_mode_returns_null()
    {
        var results = Results(Item(1, "Something Else", "Another Artist"));

        Assert.Null(DeezerClient.PickBest(results, "rick james mary jane", requireMatch: true));
    }
}