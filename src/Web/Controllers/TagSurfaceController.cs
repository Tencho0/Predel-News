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

public class TagSurfaceController : SurfaceController
{
    private readonly IArticleService _articleService;
    private readonly Core.Interfaces.ITagService _tagService;
    private readonly ICategoryService _categoryService;

    public TagSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IArticleService articleService,
        Core.Interfaces.ITagService tagService,
        ICategoryService categoryService)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _articleService = articleService;
        _tagService = tagService;
        _categoryService = categoryService;
    }

    [HttpGet]
    [Route("tag/{slug}")]
    public async Task<IActionResult> Index(string slug, [FromQuery] int page = 1)
    {
        var tag = await _tagService.GetBySlugAsync(slug);
        if (tag == null)
        {
            return NotFound();
        }

        var articlesTask = _articleService.GetByTagAsync(slug, page, 20);
        var navigationTask = _categoryService.GetMainNavigationAsync();
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);

        await Task.WhenAll(articlesTask, navigationTask, mostReadTask);

        var model = new TagViewModel
        {
            Tag = tag,
            Articles = await articlesTask,
            PageTitle = $"Articles tagged with #{tag.Name}",
            MetaDescription = $"All articles tagged with {tag.Name} on Predel News",
            CanonicalUrl = tag.GetUrl(),
            NavigationCategories = await navigationTask,
            SidebarMostRead = await mostReadTask,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = "Tags", Url = "/tags" },
                new() { Title = tag.Name, IsCurrentPage = true }
            }
        };

        return View("~/Views/Tag/Index.cshtml", model);
    }
}
