using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PredelNews.Core.Constants;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class StaticPageController : RenderController
{
    public StaticPageController(
        ILogger<StaticPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
    }

    public override IActionResult Index()
    {
        var content = CurrentPage!;
        var pageTitle = content.Value<string>(PropertyAliases.PageTitle) ?? content.Name ?? "";
        var mediaKitPdf = content.Value<IPublishedContent>(PropertyAliases.MediaKitPdf);

        var model = new StaticPageViewModel
        {
            PageTitle = pageTitle,
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? pageTitle,
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            Body = content.Value<string>(PropertyAliases.Body) ?? "",
            MediaKitPdfUrl = mediaKitPdf?.Url(),
            Breadcrumbs =
            [
                new BreadcrumbItem { Name = "Начало", Url = "/" },
                new BreadcrumbItem { Name = pageTitle },
            ],
        };

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }
}
