using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace PredelNews.Web.Controllers;

public class NewsRootController : RenderController
{
    public NewsRootController(
        ILogger<NewsRootController> logger,
        ICompositeViewEngine compositeViewEngine,
        IUmbracoContextAccessor umbracoContextAccessor)
        : base(logger, compositeViewEngine, umbracoContextAccessor) { }

    public override IActionResult Index() => RedirectPermanent("/vsichki-novini/");
}
