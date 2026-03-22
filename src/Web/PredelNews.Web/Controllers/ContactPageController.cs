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

public class ContactPageController : RenderController
{
    private readonly ISiteSettingsService _siteSettings;

    public ContactPageController(
        ILogger<ContactPageController> logger,
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

        var model = new ContactPageViewModel
        {
            PageTitle = pageTitle,
            SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? pageTitle,
            SeoDescription = content.Value<string>(PropertyAliases.SeoDescription),
            IntroText = content.Value<string>(PropertyAliases.IntroText),
            PhoneNumber = content.Value<string>(PropertyAliases.PhoneNumber),
            DisplayEmail = content.Value<string>(PropertyAliases.DisplayEmail),
            Breadcrumbs =
            [
                new BreadcrumbItem { Name = "Начало", Url = "/" },
                new BreadcrumbItem { Name = pageTitle },
            ],
        };

        var ogImage = content.Value<IPublishedContent>(PropertyAliases.OgImage);
        var ogImageUrl = ogImage?.GetCropUrl(width: 1200)
            ?? _siteSettings.GetSiteSettings().DefaultOgImageUrl;

        ViewBag.Title = "Контакти";
        ViewBag.SeoTitle = content.Value<string>(PropertyAliases.SeoTitle) ?? "Контакти";
        ViewBag.SeoDescription = model.SeoDescription;
        ViewBag.CanonicalUrl = $"{Request.Scheme}://{Request.Host}{CurrentPage!.Url()}";
        ViewBag.OgImageUrl = ogImageUrl;
        ViewBag.Breadcrumbs = model.Breadcrumbs;
        return CurrentTemplate(model);
    }
}
