using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PredelNews.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace PredelNews.Web.Controllers;

public class SearchPageController : RenderController
{
    private readonly ISearchService _searchService;

    public SearchPageController(
        ILogger<SearchPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        ISearchService searchService)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _searchService = searchService;
    }

    public override IActionResult Index()
    {
        var q = Request.Query["q"].FirstOrDefault();
        var pageStr = Request.Query["page"].FirstOrDefault();
        int.TryParse(pageStr, out var page);
        if (page < 1) page = 1;

        var model = _searchService.Search(q, page);

        ViewBag.Title = model.PageTitle;
        ViewBag.SeoTitle = model.SeoTitle ?? model.PageTitle;
        ViewBag.SeoDescription = model.SeoDescription;

        return CurrentTemplate(model);
    }
}
