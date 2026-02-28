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

public class AuthorController : RenderController
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ContentMapperService _mapper;

    public AuthorController(
        ILogger<AuthorController> logger,
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
        var authorName = content.Value<string>(PropertyAliases.FullName) ?? content.Name ?? "";
        var photo = content.Value<IPublishedContent>(PropertyAliases.Photo);

        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var newsRoot = root?.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.NewsRoot);
        var articles = newsRoot?.Children?
            .Where(a => a.ContentType.Alias == DocumentTypes.Article && a.IsPublished())
            .Where(a => a.Value<IPublishedContent>(PropertyAliases.Author)?.Id == content.Id)
            .OrderByDescending(a => a.Value<DateTime>(PropertyAliases.PublishDate))
            .ToList() ?? [];

        var page = int.TryParse(Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
        const int pageSize = 20;
        var totalPages = (int)Math.Ceiling(articles.Count / (double)pageSize);

        if (page > totalPages && totalPages > 0)
            return NotFound();

        var model = new ArchivePageViewModel
        {
            PageTitle = authorName,
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? authorName,
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            ArchiveTitle = authorName,
            AuthorName = authorName,
            AuthorPhotoUrl = photo?.GetCropUrl(width: 120, furtherOptions: "&format=webp"),
            AuthorBio = content.Value<string>(PropertyAliases.Bio),
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
                new BreadcrumbItem { Name = "Автори", Url = "/avtor" },
                new BreadcrumbItem { Name = authorName },
            ],
        };

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }
}
