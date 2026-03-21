using System.Text.RegularExpressions;
using PredelNews.Core.Constants;

namespace PredelNews.Core.Services;

public static partial class SearchQuerySanitizer
{
    private static readonly string[] LuceneSingleCharSpecials = ["+", "-", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "/"];

    public static string? Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Strip HTML tags
        var result = HtmlTagRegex().Replace(input, string.Empty);

        // Escape backslash first (before adding new backslashes)
        result = result.Replace(@"\", @"\\");

        // Escape two-character Lucene operators before single chars
        result = result.Replace("&&", @"\&&");
        result = result.Replace("||", @"\||");

        // Escape single-character Lucene special characters
        foreach (var ch in LuceneSingleCharSpecials)
        {
            result = result.Replace(ch, @"\" + ch);
        }

        result = result.Trim();

        if (result.Length > SearchConstants.MaxQueryLength)
            result = result[..SearchConstants.MaxQueryLength];

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
