using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.OutputCaching;
using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;
using PredelNews.Web.Services;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class AllNewsPageController : RenderController
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ContentMapperService _mapper;
    private readonly ISiteSettingsService _siteSettings;

    public AllNewsPageController(
        ILogger<AllNewsPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        UmbracoHelper umbracoHelper,
        ContentMapperService mapper,
        ISiteSettingsService siteSettings)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _umbracoHelper = umbracoHelper;
        _mapper = mapper;
        _siteSettings = siteSettings;
    }

    [OutputCache(PolicyName = "PublicPage", Tags = ["allnews"])]
    public override IActionResult Index()
    {
        var content = CurrentPage!;

        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var newsRoot = root?.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.NewsRoot);
        var articles = newsRoot?.Children?
            .Where(a => a.ContentType.Alias == DocumentTypes.Article && a.IsPublished())
            .OrderByDescending(a => a.Value<DateTime>(PropertyAliases.PublishDate))
            .ToList() ?? [];

        var page = int.TryParse(Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
        const int pageSize = 20;
        var totalPages = (int)Math.Ceiling(articles.Count / (double)pageSize);

        if (page > totalPages && totalPages > 0)
            return NotFound();

        var model = new ArchivePageViewModel
        {
            PageTitle = "Всички новини",
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? "Всички новини",
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            ArchiveTitle = "Всички новини",
            TotalArticleCount = articles.Count,
            Articles = _mapper.MapArticleSummaries(articles.Skip((page - 1) * pageSize).Take(pageSize)),
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = articles.Count,
                PageSize = pageSize,
                BaseUrl = content.Url() ?? "",
            },
            Breadcrumbs =
            [
                new BreadcrumbItem { Name = "Начало", Url = "/" },
                new BreadcrumbItem { Name = "Всички новини" },
            ],
        };

        var ogImage = content.Value<IPublishedContent>(PropertyAliases.OgImage);
        var ogImageUrl = ogImage?.GetCropUrl(width: 1200)
            ?? _siteSettings.GetSiteSettings().DefaultOgImageUrl;

        ViewBag.Title = "Всички новини";
        ViewBag.SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? "Всички новини";
        ViewBag.SeoDescription = model.SeoDescription;
        ViewBag.CanonicalUrl = $"{Request.Scheme}://{Request.Host}{content.Url()}";
        ViewBag.OgImageUrl = ogImageUrl;
        ViewBag.Breadcrumbs = model.Breadcrumbs;
        return CurrentTemplate(model);
    }
}
