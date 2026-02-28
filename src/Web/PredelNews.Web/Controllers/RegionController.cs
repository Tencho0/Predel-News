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

public class RegionController : RenderController
{
    private readonly UmbracoHelper _umbracoHelper;
    private readonly ContentMapperService _mapper;

    public RegionController(
        ILogger<RegionController> logger,
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
        var regionName = content.Value<string>(PropertyAliases.RegionName) ?? content.Name ?? "";

        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var newsRoot = root?.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.NewsRoot);
        var articles = newsRoot?.Children?
            .Where(a => a.ContentType.Alias == DocumentTypes.Article && a.IsPublished())
            .Where(a => a.Value<IPublishedContent>(PropertyAliases.Region)?.Id == content.Id)
            .OrderByDescending(a => a.Value<DateTime>(PropertyAliases.PublishDate))
            .ToList() ?? [];

        var page = int.TryParse(Request.Query["page"], out var p) ? Math.Max(1, p) : 1;
        const int pageSize = 20;
        var totalPages = (int)Math.Ceiling(articles.Count / (double)pageSize);

        if (page > totalPages && totalPages > 0)
            return NotFound();

        var model = new ArchivePageViewModel
        {
            PageTitle = regionName,
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? regionName,
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            ArchiveTitle = regionName,
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
                new BreadcrumbItem { Name = "Региони", Url = "/region" },
                new BreadcrumbItem { Name = regionName },
            ],
        };

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }
}
