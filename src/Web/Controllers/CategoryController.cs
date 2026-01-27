using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace PredelNews.Web.Controllers;

public class CategoryController : RenderController
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ILogger<CategoryController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        IArticleService articleService,
        ICategoryService categoryService)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _articleService = articleService;
        _categoryService = categoryService;
        _logger = logger;
    }

    public override IActionResult Index()
    {
        return IndexAsync().GetAwaiter().GetResult();
    }

    private async Task<IActionResult> IndexAsync()
    {
        var slug = CurrentPage?.UrlSegment;
        if (string.IsNullOrEmpty(slug))
        {
            return NotFound();
        }

        var category = await _categoryService.GetBySlugAsync(slug);
        if (category == null)
        {
            return NotFound();
        }

        var page = Request.Query.ContainsKey("page") && int.TryParse(Request.Query["page"], out var p) ? p : 1;

        var articlesTask = _articleService.GetByCategoryAsync(slug, page, 20);
        var navigationTask = _categoryService.GetMainNavigationAsync();
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);

        await Task.WhenAll(articlesTask, navigationTask, mostReadTask);

        var articles = await articlesTask;
        var featuredInCategory = articles.Items.Where(a => a.IsFeatured).Take(3).ToList();

        var model = new CategoryViewModel
        {
            CurrentPage = CurrentPage,
            Category = category,
            Articles = articles,
            FeaturedInCategory = featuredInCategory,
            PageTitle = category.MetaTitle ?? category.Name,
            MetaDescription = category.MetaDescription ?? category.Description ?? $"Latest articles in {category.Name}",
            CanonicalUrl = category.GetUrl(),
            OgType = "website",
            NavigationCategories = await navigationTask,
            SidebarMostRead = await mostReadTask,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = category.Name, Url = category.GetUrl(), IsCurrentPage = true }
            }
        };

        return CurrentTemplate(model);
    }
}
