using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PredelNews.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class SearchPageController : RenderController
{
    private readonly ISearchService _searchService;
    private readonly ISiteSettingsService _siteSettings;

    public SearchPageController(
        ILogger<SearchPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        ISearchService searchService,
        ISiteSettingsService siteSettings)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _searchService = searchService;
        _siteSettings = siteSettings;
    }

    public override IActionResult Index()
    {
        var q = Request.Query["q"].FirstOrDefault();
        var pageStr = Request.Query["page"].FirstOrDefault();
        int.TryParse(pageStr, out var page);
        if (page < 1) page = 1;

        var model = _searchService.Search(q, page);

        var searchTitle = string.IsNullOrEmpty(q) ? "Търсене" : $"Търсене: {q}";

        // Build canonical URL with only relevant search params (q, page)
        var canonicalParams = new List<string>();
        if (!string.IsNullOrEmpty(q))
            canonicalParams.Add($"q={Uri.EscapeDataString(q)}");
        if (page > 1)
            canonicalParams.Add($"page={page}");
        var canonicalQuery = canonicalParams.Count > 0 ? "?" + string.Join("&", canonicalParams) : "";

        ViewBag.Title = searchTitle;
        ViewBag.SeoTitle = searchTitle;
        ViewBag.SeoDescription = model.SeoDescription;
        ViewBag.CanonicalUrl = $"{Request.Scheme}://{Request.Host}{CurrentPage!.Url()}{canonicalQuery}";
        ViewBag.OgImageUrl = _siteSettings.GetSiteSettings().DefaultOgImageUrl;
        ViewBag.Breadcrumbs = new List<PredelNews.Core.ViewModels.BreadcrumbItem>
        {
            new() { Name = "Начало", Url = "/" },
            new() { Name = searchTitle },
        };

        return CurrentTemplate(model);
    }
}
