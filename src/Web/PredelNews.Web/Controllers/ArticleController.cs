using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PredelNews.Core.Constants;
using PredelNews.Core.ViewModels;
using PredelNews.Web.Services;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class ArticleController : RenderController
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ContentMapperService _mapper;

    public ArticleController(
        ILogger<ArticleController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        UmbracoHelper umbracoHelper,
        ContentMapperService mapper)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _umbracoHelper = umbracoHelper;
        _mapper = mapper;
    }

    public override IActionResult Index()
    {
        var content = CurrentPage!;
        var category = content.Value<IPublishedContent>(PropertyAliases.Category);
        var region = content.Value<IPublishedContent>(PropertyAliases.Region);
        var author = content.Value<IPublishedContent>(PropertyAliases.Author);
        var coverImage = content.Value<IPublishedContent>(PropertyAliases.CoverImage);
        var tags = content.Value<IEnumerable<IPublishedContent>>(PropertyAliases.Tags)?.ToList() ?? [];

        var headline = content.Value<string>(PropertyAliases.Headline) ?? content.Name ?? "";
        var categoryName = category?.Value<string>(PropertyAliases.CategoryName) ?? category?.Name;
        var shareUrl = $"{Request.Scheme}://{Request.Host}{content.Url()}";

        var model = new ArticleDetailViewModel
        {
            PageTitle = headline,
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? headline,
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            OgImageUrl = coverImage?.GetCropUrl(width: 1200, furtherOptions: "&format=webp"),
            CanonicalUrl = shareUrl,
            Headline = headline,
            Subtitle = content.Value<string>(PropertyAliases.Subtitle),
            Body = content.Value<string>(PropertyAliases.Body) ?? "",
            CoverImageUrl = coverImage?.GetCropUrl(width: 800, furtherOptions: "&format=webp"),
            CoverImageAlt = coverImage?.Name,
            CoverImageSrcSet = coverImage != null
                ? string.Join(", ",
                    $"{coverImage.GetCropUrl(width: 400, furtherOptions: "&format=webp")} 400w",
                    $"{coverImage.GetCropUrl(width: 800, furtherOptions: "&format=webp")} 800w",
                    $"{coverImage.GetCropUrl(width: 1200, furtherOptions: "&format=webp")} 1200w")
                : null,
            CategoryName = categoryName,
            CategoryUrl = category?.Url(),
            RegionName = region?.Value<string>(PropertyAliases.RegionName) ?? region?.Name,
            RegionUrl = region?.Url(),
            Tags = tags.Select(t => new TagViewModel
            {
                Name = t.Value<string>(PropertyAliases.TagName) ?? t.Name ?? "",
                Url = t.Url() ?? "",
            }).ToList(),
            AuthorName = author?.Value<string>(PropertyAliases.FullName) ?? author?.Name,
            AuthorUrl = author?.Url(),
            AuthorPhotoUrl = author?.Value<IPublishedContent>(PropertyAliases.Photo)?.GetCropUrl(width: 80, furtherOptions: "&format=webp"),
            AuthorBio = author?.Value<string>(PropertyAliases.Bio),
            PublishDate = content.Value<DateTime>(PropertyAliases.PublishDate),
            IsSponsored = content.Value<bool>(PropertyAliases.IsSponsored),
            SponsorName = content.Value<string>(PropertyAliases.SponsorName),
            ShareUrl = shareUrl,
            Breadcrumbs =
            [
                new BreadcrumbItem { Name = "Начало", Url = "/" },
                ..( categoryName != null ? new[] { new BreadcrumbItem { Name = categoryName, Url = category?.Url() } } : []),
                new BreadcrumbItem { Name = headline },
            ],
        };

        // Related articles
        var relatedOverride = content.Value<IEnumerable<IPublishedContent>>(PropertyAliases.RelatedArticlesOverride)?.ToList();
        if (relatedOverride != null && relatedOverride.Count > 0)
        {
            model.RelatedArticles = _mapper.MapArticleSummaries(relatedOverride.Take(4));
        }
        else
        {
            model.RelatedArticles = GetRelatedArticles(content, category, region, tags);
        }

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }

    private List<ArticleSummaryViewModel> GetRelatedArticles(
        IPublishedContent current,
        IPublishedContent? category,
        IPublishedContent? region,
        List<IPublishedContent> tags)
    {
        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var newsRoot = root?.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.NewsRoot);
        if (newsRoot?.Children == null) return [];

        var tagIds = tags.Select(t => t.Id).ToHashSet();
        var now = DateTime.UtcNow;

        var scored = newsRoot.Children
            .Where(c => c.ContentType.Alias == DocumentTypes.Article && c.IsPublished() && c.Id != current.Id)
            .Select(a =>
            {
                var score = 0.0;
                var aTags = a.Value<IEnumerable<IPublishedContent>>(PropertyAliases.Tags)?.ToList() ?? [];
                score += aTags.Count(t => tagIds.Contains(t.Id)) * 3;

                var aCat = a.Value<IPublishedContent>(PropertyAliases.Category);
                if (aCat != null && category != null && aCat.Id == category.Id) score += 2;

                var aRegion = a.Value<IPublishedContent>(PropertyAliases.Region);
                if (aRegion != null && region != null && aRegion.Id == region.Id) score += 1;

                var publishDate = a.Value<DateTime>(PropertyAliases.PublishDate);
                var daysDiff = (now - publishDate).TotalDays;
                score += Math.Max(0.1, 1.0 - daysDiff / 30.0);

                return (Article: a, Score: score, PublishDate: publishDate);
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.PublishDate)
            .Take(4)
            .Select(x => x.Article);

        return _mapper.MapArticleSummaries(scored);
    }
}
