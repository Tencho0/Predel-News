using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace PredelNews.Web.Controllers;

public class HomeController : RenderController
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ILogger<HomeController> logger,
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
        var model = new HomeViewModel
        {
            CurrentPage = CurrentPage,
            PageTitle = "Home",
            MetaDescription = "Latest news and articles from Predel News",
            OgType = "website"
        };

        var featuredTask = _articleService.GetFeaturedAsync(5);
        var breakingTask = _articleService.GetBreakingNewsAsync(3);
        var latestTask = _articleService.GetLatestAsync(1, 10);
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);
        var categoriesTask = _categoryService.GetMainNavigationAsync();

        await Task.WhenAll(featuredTask, breakingTask, latestTask, mostReadTask, categoriesTask);

        model.FeaturedArticles = await featuredTask;
        model.BreakingNews = await breakingTask;
        model.LatestArticles = (await latestTask).Items;
        model.MostReadArticles = await mostReadTask;
        model.NavigationCategories = await categoriesTask;
        model.SidebarMostRead = await mostReadTask;

        var allCategories = await _categoryService.GetAllAsync();
        var categoryBlocks = new List<CategoryBlock>();

        foreach (var category in allCategories.Take(4))
        {
            var articles = await _articleService.GetByCategoryAsync(category.Slug, 1, 4);
            if (articles.Items.Any())
            {
                categoryBlocks.Add(new CategoryBlock
                {
                    Category = category,
                    Articles = articles.Items
                });
            }
        }

        model.CategoryBlocks = categoryBlocks;

        return CurrentTemplate(model);
    }
}
