using System.Text.RegularExpressions;
using Examine;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Web.Search;

public partial class ArticleValueSetBuilder
{
    private readonly IContentService _contentService;

    public ArticleValueSetBuilder(IContentService contentService)
    {
        _contentService = contentService;
    }

    public ValueSet BuildValueSet(IContent content)
    {
        var headline = content.GetValue<string>(PropertyAliases.Headline) ?? "";
        var subtitle = content.GetValue<string>(PropertyAliases.Subtitle) ?? "";
        var bodyHtml = content.GetValue<string>(PropertyAliases.Body) ?? "";
        var bodyText = StripHtmlRegex().Replace(bodyHtml, " ").Trim();
        var slug = content.GetValue<string>(PropertyAliases.Slug) ?? "";
        var publishDate = content.GetValue<DateTime>(PropertyAliases.PublishDate);
        var isSponsored = content.GetValue<bool>(PropertyAliases.IsSponsored);

        var categoryName = ResolvePickedContentName(content, PropertyAliases.Category, PropertyAliases.CategoryName);
        var regionName = ResolvePickedContentName(content, PropertyAliases.Region, PropertyAliases.RegionName);
        var authorName = ResolvePickedContentName(content, PropertyAliases.Author, PropertyAliases.FullName);
        var tags = ResolveTagNames(content);

        var values = new Dictionary<string, object>
        {
            [PropertyAliases.Headline] = headline,
            [PropertyAliases.Subtitle] = subtitle,
            ["bodyText"] = bodyText,
            [PropertyAliases.Tags] = tags,
            [PropertyAliases.CategoryName] = categoryName,
            [PropertyAliases.RegionName] = regionName,
            ["authorName"] = authorName,
            [PropertyAliases.PublishDate] = publishDate.ToString("yyyy-MM-dd HH:mm:ss"),
            [PropertyAliases.IsSponsored] = isSponsored ? "1" : "0",
            ["articleUrl"] = $"/novini/{slug}/",
        };

        return new ValueSet(content.Id.ToString(), "content", "article", values);
    }

    private string ResolvePickedContentName(IContent content, string pickerAlias, string namePropertyAlias)
    {
        var pickerValue = content.GetValue<string>(pickerAlias);
        if (string.IsNullOrWhiteSpace(pickerValue))
            return "";

        if (Guid.TryParse(pickerValue, out var key))
        {
            var picked = _contentService.GetById(key);
            if (picked != null)
                return picked.GetValue<string>(namePropertyAlias) ?? picked.Name ?? "";
        }

        return "";
    }

    private string ResolveTagNames(IContent content)
    {
        var tagsValue = content.GetValue<string>(PropertyAliases.Tags);
        if (string.IsNullOrWhiteSpace(tagsValue))
            return "";

        var tagNames = new List<string>();
        var keys = tagsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var keyStr in keys)
        {
            if (Guid.TryParse(keyStr, out var key))
            {
                var tagContent = _contentService.GetById(key);
                if (tagContent != null)
                {
                    var tagName = tagContent.GetValue<string>(PropertyAliases.TagName) ?? tagContent.Name ?? "";
                    if (!string.IsNullOrWhiteSpace(tagName))
                        tagNames.Add(tagName);
                }
            }
        }

        return string.Join(" ", tagNames);
    }

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex StripHtmlRegex();
}
