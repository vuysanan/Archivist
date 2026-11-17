using Archivist.Api.Services;

namespace Archivist.Api.Tests;

public class TextUtilTests
{
    [Theory]
    [InlineData("The Notorious B.I.G.", "the notorious big")]   // punctuation and capitals ignored
    [InlineData("Beyoncé", "beyonce")]                           // accents removed
    [InlineData("Simon & Garfunkel", "simon and garfunkel")]     // & becomes "and"
    [InlineData("  DNA.  ", "dna")]                              // extra spaces trimmed
    public void Normalize_makes_names_easy_to_compare(string input, string expected)
    {
        Assert.Equal(expected, TextUtil.Normalize(input));
    }

    [Theory]
    [InlineData("Dark Horse (feat. Juicy J)", "Dark Horse")]
    [InlineData("Song [Deluxe Edition]", "Song")]
    [InlineData("No Brackets", "No Brackets")]
    public void StripBrackets_removes_bracketed_extras(string input, string expected)
    {
        Assert.Equal(expected, TextUtil.StripBrackets(input));
    }

    [Fact]
    public void Key_ignores_capitals_punctuation_and_remaster_notes()
    {
        Assert.Equal("the notorious big|juicy", TextUtil.Key("The Notorious B.I.G.", "Juicy (2005 Remaster)"));
        Assert.Equal(TextUtil.Key("Rick James", "Mary Jane"), TextUtil.Key("rick james", "Mary Jane (Live 1982)"));
    }
}