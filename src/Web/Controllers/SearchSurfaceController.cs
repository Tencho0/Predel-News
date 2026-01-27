using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Interfaces;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Website.Controllers;

namespace PredelNews.Web.Controllers;

public class SearchSurfaceController : SurfaceController
{
    private readonly ISearchService _searchService;
    private readonly ICategoryService _categoryService;
    private readonly IArticleService _articleService;

    public SearchSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        ISearchService searchService,
        ICategoryService categoryService,
        IArticleService articleService)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _searchService = searchService;
        _categoryService = categoryService;
        _articleService = articleService;
    }

    [HttpGet]
    [Route("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int page = 1)
    {
        var query = q?.Trim() ?? string.Empty;

        var navigationTask = _categoryService.GetMainNavigationAsync();
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);

        var model = new SearchViewModel
        {
            Query = query,
            PageTitle = string.IsNullOrEmpty(query) ? "Search" : $"Search results for \"{query}\"",
            MetaDescription = $"Search results for {query} on Predel News"
        };

        if (!string.IsNullOrEmpty(query))
        {
            model.Result = await _searchService.SearchAsync(query, page);
        }

        await Task.WhenAll(navigationTask, mostReadTask);

        model.NavigationCategories = await navigationTask;
        model.SidebarMostRead = await mostReadTask;
        model.Breadcrumbs = new List<BreadcrumbItem>
        {
            new() { Title = "Search", IsCurrentPage = true }
        };

        return View("~/Views/Search/Index.cshtml", model);
    }

    [HttpGet]
    [Route("api/search/suggestions")]
    public async Task<IActionResult> Suggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return Ok(Array.Empty<string>());
        }

        var suggestions = await _searchService.GetSuggestionsAsync(q, 10);
        return Ok(suggestions);
    }
}
