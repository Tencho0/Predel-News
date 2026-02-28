using System.Text;
using System.Text.RegularExpressions;

namespace PredelNews.Core.Services;

public partial class SlugGenerator : ISlugGenerator
{
    private static readonly Dictionary<char, string> CyrillicMap = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d",
        ['е'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y",
        ['к'] = "k", ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o",
        ['п'] = "p", ['р'] = "r", ['с'] = "s", ['т'] = "t", ['у'] = "u",
        ['ф'] = "f", ['х'] = "h", ['ц'] = "ts", ['ч'] = "ch", ['ш'] = "sh",
        ['щ'] = "sht", ['ъ'] = "a", ['ь'] = "", ['ю'] = "yu", ['я'] = "ya",
    };

    public string GenerateSlug(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var lower = input.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);

        foreach (var c in lower)
        {
            if (CyrillicMap.TryGetValue(c, out var latin))
            {
                sb.Append(latin);
            }
            else if (char.IsAsciiLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('-');
            }
        }

        var slug = CollapseHyphensRegex().Replace(sb.ToString(), "-").Trim('-');
        return slug;
    }

    public async Task<string> GenerateUniqueSlugAsync(string input, Func<string, Task<bool>> slugExistsChecker)
    {
        var baseSlug = GenerateSlug(input);
        if (string.IsNullOrEmpty(baseSlug))
            return baseSlug;

        if (!await slugExistsChecker(baseSlug))
            return baseSlug;

        var counter = 2;
        while (true)
        {
            var candidate = $"{baseSlug}-{counter}";
            if (!await slugExistsChecker(candidate))
                return candidate;
            counter++;
        }
    }

    [GeneratedRegex("-{2,}")]
    private static partial Regex CollapseHyphensRegex();
}
