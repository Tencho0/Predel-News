using PredelNews.Core.Constants;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace PredelNews.Web.Services;

public class ContentMapperService
{
    public ArticleSummaryViewModel MapArticleSummary(IPublishedContent article)
    {
        var category = article.Value<IPublishedContent>(PropertyAliases.Category);
        var region = article.Value<IPublishedContent>(PropertyAliases.Region);
        var author = article.Value<IPublishedContent>(PropertyAliases.Author);
        var coverImage = article.Value<IPublishedContent>(PropertyAliases.CoverImage);

        return new ArticleSummaryViewModel
        {
            Id = article.Id,
            Headline = article.Value<string>(PropertyAliases.Headline) ?? article.Name ?? string.Empty,
            Subtitle = article.Value<string>(PropertyAliases.Subtitle),
            Slug = article.Value<string>(PropertyAliases.Slug) ?? article.UrlSegment ?? string.Empty,
            CoverImageUrl = coverImage?.GetCropUrl(width: 400, furtherOptions: "&format=webp"),
            CoverImageAlt = coverImage?.Name,
            CategoryName = category?.Value<string>(PropertyAliases.CategoryName) ?? category?.Name,
            CategorySlug = category?.UrlSegment,
            RegionName = region?.Value<string>(PropertyAliases.RegionName) ?? region?.Name,
            RegionSlug = region?.UrlSegment,
            AuthorName = author?.Value<string>(PropertyAliases.FullName) ?? author?.Name,
            ArticleUrl = article.Url() ?? string.Empty,
            AuthorUrl = author?.Url(),
            PublishDate = article.Value<DateTime>(PropertyAliases.PublishDate),
            IsBreakingNews = article.Value<bool>(PropertyAliases.IsBreakingNews),
            IsSponsored = article.Value<bool>(PropertyAliases.IsSponsored),
            SponsorName = article.Value<string>(PropertyAliases.SponsorName),
        };
    }

    public List<ArticleSummaryViewModel> MapArticleSummaries(IEnumerable<IPublishedContent> articles)
    {
        return articles.Select(MapArticleSummary).ToList();
    }
}
