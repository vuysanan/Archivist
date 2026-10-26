using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Archivist.Api.Services
{
    // Small helpers for comparing song and artist names.
    public static class TextUtil
    {
        // "Song (feat. Someone)" -> "Song"
        public static string StripBrackets(string s) => Regex.Replace(s, @"\s*[\(\[].*?[\)\]]", "");

        // Lowercase, no accents or punctuation, so "The Notorious B.I.G." equals "the notorious big".
        public static string Normalize(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                else if (c == '&') sb.Append(" and ");
                else if (char.IsWhiteSpace(c)) sb.Append(' ');
            }
            return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }
    }
}