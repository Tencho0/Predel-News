using HtmlAgilityPack;

namespace PredelNews.Core.Services;

public static class ArticleBodyProcessor
{
    private const string InternalDomain = "predelnews.com";

    /// <summary>
    /// When isSponsored is true, rewrites all external links to include rel="sponsored noopener".
    /// Internal links (predelnews.com) and relative links are not modified.
    /// </summary>
    public static string Process(string body, bool isSponsored)
    {
        if (string.IsNullOrEmpty(body)) return body;
        if (!isSponsored) return body;

        var doc = new HtmlDocument();
        doc.LoadHtml(body);

        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links == null) return body;

        foreach (var node in links)
        {
            var href = node.GetAttributeValue("href", string.Empty);
            if (!href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;
            if (href.Contains(InternalDomain, StringComparison.OrdinalIgnoreCase)) continue;

            node.SetAttributeValue("rel", "sponsored noopener");
        }

        return doc.DocumentNode.OuterHtml;
    }

    /// <summary>
    /// Splits HTML at the closing tag of the Nth paragraph.
    /// Returns (fullBody, "") if there are fewer than afterParagraph paragraphs.
    /// </summary>
    public static (string Before, string After) SplitAtParagraph(string body, int afterParagraph)
    {
        if (string.IsNullOrEmpty(body)) return (string.Empty, string.Empty);

        const string closeTag = "</p>";
        int found = 0;
        int searchFrom = 0;

        while (found < afterParagraph)
        {
            int idx = body.IndexOf(closeTag, searchFrom, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return (body, string.Empty);

            found++;
            searchFrom = idx + closeTag.Length;
        }

        return (body[..searchFrom], body[searchFrom..]);
    }
}
