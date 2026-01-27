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

public class AuthorSurfaceController : SurfaceController
{
    private readonly IArticleService _articleService;
    private readonly IAuthorService _authorService;
    private readonly ICategoryService _categoryService;

    public AuthorSurfaceController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider,
        IArticleService articleService,
        IAuthorService authorService,
        ICategoryService categoryService)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
        _articleService = articleService;
        _authorService = authorService;
        _categoryService = categoryService;
    }

    [HttpGet]
    [Route("author/{slug}")]
    public async Task<IActionResult> Index(string slug, [FromQuery] int page = 1)
    {
        var author = await _authorService.GetBySlugAsync(slug);
        if (author == null)
        {
            return NotFound();
        }

        var articlesTask = _articleService.GetByAuthorAsync(slug, page, 20);
        var navigationTask = _categoryService.GetMainNavigationAsync();
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);

        await Task.WhenAll(articlesTask, navigationTask, mostReadTask);

        var articles = await articlesTask;
        author.ArticleCount = articles.TotalItems;

        var model = new AuthorViewModel
        {
            Author = author,
            Articles = articles,
            PageTitle = $"Articles by {author.Name}",
            MetaDescription = author.Bio ?? $"All articles written by {author.Name} on Predel News",
            CanonicalUrl = author.GetUrl(),
            OgImage = author.Avatar?.Url,
            NavigationCategories = await navigationTask,
            SidebarMostRead = await mostReadTask,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = "Authors", Url = "/authors" },
                new() { Title = author.Name, IsCurrentPage = true }
            }
        };

        return View("~/Views/Author/Index.cshtml", model);
    }
}
