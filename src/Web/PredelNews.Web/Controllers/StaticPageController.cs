using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.OutputCaching;
using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class StaticPageController : RenderController
{
    private readonly ISiteSettingsService _siteSettings;

    public StaticPageController(
        ILogger<StaticPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor,
        ISiteSettingsService siteSettings)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
        _siteSettings = siteSettings;
    }

    [OutputCache(PolicyName = "PublicPage")]
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

        var ogImage = content.Value<IPublishedContent>(PropertyAliases.OgImage);
        var ogImageUrl = ogImage?.GetCropUrl(width: 1200)
            ?? _siteSettings.GetSiteSettings().DefaultOgImageUrl;

        ViewBag.Title = model.PageTitle;
        ViewBag.SeoTitle = model.SeoTitle ?? model.PageTitle;
        ViewBag.SeoDescription = model.SeoDescription;
        ViewBag.CanonicalUrl = $"{Request.Scheme}://{Request.Host}{CurrentPage!.Url()}";
        ViewBag.OgImageUrl = ogImageUrl;
        return CurrentTemplate(model);
    }
}
