using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using PredelNews.Core.Constants;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

public class ContactPageController : RenderController
{
    public ContactPageController(
        ILogger<ContactPageController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor)
        : base(logger, compositeViewEngine, umbracoContextAccessor)
    {
    }

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

        ViewBag.Title = model.PageTitle;
        return CurrentTemplate(model);
    }
}
