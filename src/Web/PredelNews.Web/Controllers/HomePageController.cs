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

public class HomePageController : RenderController
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ContentMapperService _mapper;

    public HomePageController(
        ILogger<HomePageController> logger,
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
        var model = new HomePageViewModel
        {
            PageTitle = "Начало",
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle),
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
        };

        // Featured articles from picker, fallback to 6 most recent
        var featuredPicks = content.Value<IEnumerable<IPublishedContent>>(PropertyAliases.FeaturedArticles)?.ToList();
        var allArticles = GetAllPublishedArticles();

        if (featuredPicks != null && featuredPicks.Count > 0)
        {
            model.FeaturedArticle = _mapper.MapArticleSummary(featuredPicks[0]);
            model.HeadlineLinks = _mapper.MapArticleSummaries(featuredPicks.Skip(1).Take(5));
        }
        else
        {
            var recent = allArticles.Take(6).ToList();
            if (recent.Count > 0)
            {
                model.FeaturedArticle = _mapper.MapArticleSummary(recent[0]);
                model.HeadlineLinks = _mapper.MapArticleSummaries(recent.Skip(1));
            }
        }

        // National headlines
        var nationalOverride = content.Value<IEnumerable<IPublishedContent>>(PropertyAliases.NationalHeadlinesOverride)?.ToList();
        if (nationalOverride != null && nationalOverride.Count > 0)
        {
            model.NationalHeadlines = _mapper.MapArticleSummaries(nationalOverride.Take(3));
        }
        else
        {
            var nationalArticles = allArticles
                .Where(a =>
                {
                    var region = a.Value<IPublishedContent>(PropertyAliases.Region);
                    var regionName = region?.Value<string>(PropertyAliases.RegionName) ?? region?.Name;
                    return regionName == "България";
                })
                .Take(3);
            model.NationalHeadlines = _mapper.MapArticleSummaries(nationalArticles);
        }

        // Category blocks
        var categoryRoot = content.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.CategoryRoot);
        if (categoryRoot?.Children != null)
        {
            foreach (var category in categoryRoot.Children.Where(c => c.IsPublished()))
            {
                var catName = category.Value<string>(PropertyAliases.CategoryName) ?? category.Name ?? "";
                var catArticles = allArticles
                    .Where(a =>
                    {
                        var cat = a.Value<IPublishedContent>(PropertyAliases.Category);
                        return cat?.Id == category.Id;
                    })
                    .Take(4)
                    .ToList();

                if (catArticles.Count == 0) continue;

                model.CategoryBlocks.Add(new CategoryBlockViewModel
                {
                    CategoryName = catName,
                    CategorySlug = category.UrlSegment ?? "",
                    CategoryUrl = category.Url() ?? "",
                    Articles = _mapper.MapArticleSummaries(catArticles),
                });
            }
        }

        // Latest articles (paginated)
        var page = int.TryParse(Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
        const int pageSize = 20;
        model.LatestArticlesTotalCount = allArticles.Count;
        model.CurrentPage = page;
        model.LatestArticles = _mapper.MapArticleSummaries(
            allArticles.Skip((page - 1) * pageSize).Take(pageSize));

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }

    private List<IPublishedContent> GetAllPublishedArticles()
    {
        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var newsRoot = root?.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.NewsRoot);
        if (newsRoot?.Children == null) return [];

        return newsRoot.Children
            .Where(c => c.ContentType.Alias == DocumentTypes.Article && c.IsPublished())
            .OrderByDescending(c => c.Value<DateTime>(PropertyAliases.PublishDate))
            .ToList();
    }
}
