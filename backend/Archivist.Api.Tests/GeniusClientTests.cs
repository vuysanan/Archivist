using Archivist.Api.Services;

namespace Archivist.Api.Tests;

public class GeniusClientTests
{
    // The cleaned title is what we send to Genius, so remaster notes must not get in the way.
    [Theory]
    [InlineData("Juicy (2005 Remaster)", "Juicy")]
    [InlineData("Song Name - Radio Edit", "Song Name")]
    [InlineData("Dark Horse (feat. Juicy J)", "Dark Horse")]
    [InlineData("DNA.", "DNA.")]
    [InlineData("Nice For What", "Nice For What")]
    public void CleanTitle_removes_remaster_and_edit_notes(string title, string expected)
    {
        Assert.Equal(expected, GeniusClient.CleanTitle(title));
    }
}