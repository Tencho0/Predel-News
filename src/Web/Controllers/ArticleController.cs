using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class ArticleController : RenderController
{
    private readonly IArticleService _articleService;
    private readonly ICategoryService _categoryService;
    private readonly ILogger<ArticleController> _logger;

    public ArticleController(
        ILogger<ArticleController> logger,
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
        var articleSlug = CurrentPage?.UrlSegment;
        var categorySlug = CurrentPage?.Parent?.UrlSegment;

        if (string.IsNullOrEmpty(articleSlug) || string.IsNullOrEmpty(categorySlug))
        {
            return NotFound();
        }

        var article = await _articleService.GetBySlugAsync(categorySlug, articleSlug);
        if (article == null)
        {
            return NotFound();
        }

        await _articleService.IncrementViewCountAsync(article.Id);

        var relatedTask = _articleService.GetRelatedAsync(article.Id, 5);
        var navigationTask = _categoryService.GetMainNavigationAsync();
        var mostReadTask = _articleService.GetMostReadAsync(7, 10);

        await Task.WhenAll(relatedTask, navigationTask, mostReadTask);

        var model = new ArticleViewModel
        {
            CurrentPage = CurrentPage,
            Article = article,
            RelatedArticles = await relatedTask,
            PageTitle = article.MetaTitle ?? article.Title,
            MetaDescription = article.MetaDescription ?? article.Excerpt ?? TruncateContent(article.Content, 160),
            CanonicalUrl = article.CanonicalUrl ?? article.GetUrl(),
            OgType = "article",
            OgTitle = article.Title,
            OgDescription = article.Excerpt,
            OgImage = article.FeaturedImage?.Url,
            TwitterCard = "summary_large_image",
            TwitterTitle = article.Title,
            TwitterDescription = article.Excerpt,
            TwitterImage = article.FeaturedImage?.Url,
            NavigationCategories = await navigationTask,
            SidebarMostRead = await mostReadTask,
            Breadcrumbs = new List<BreadcrumbItem>
            {
                new() { Title = article.Category.Name, Url = article.Category.GetUrl() },
                new() { Title = article.Title, IsCurrentPage = true }
            }
        };

        return CurrentTemplate(model);
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        var stripped = content.StripHtml();
        if (stripped.Length <= maxLength)
            return stripped;

        return stripped.Substring(0, maxLength - 3) + "...";
    }
}
