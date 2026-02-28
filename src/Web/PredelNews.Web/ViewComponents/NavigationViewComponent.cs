using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Constants;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace PredelNews.Web.ViewComponents;

public class NavigationViewComponent : ViewComponent
{
    private readonly UmbracoHelper _umbracoHelper;

    public NavigationViewComponent(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    public IViewComponentResult Invoke(string? section = null)
    {
        var root = _umbracoHelper.ContentAtRoot().FirstOrDefault();
        var model = new NavigationViewModel();

        if (root == null)
            return GetView(section, model);

        var categoryRoot = root.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.CategoryRoot);
        if (categoryRoot?.Children != null)
        {
            model.Categories = categoryRoot.Children
                .Where(c => c.IsPublished())
                .Select(c => new NavItem
                {
                    Name = c.Value<string>(PropertyAliases.CategoryName) ?? c.Name ?? string.Empty,
                    Url = c.Url() ?? string.Empty,
                })
                .ToList();
        }

        var regionRoot = root.Children?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.RegionRoot);
        if (regionRoot?.Children != null)
        {
            model.Regions = regionRoot.Children
                .Where(c => c.IsPublished())
                .Select(c => new NavItem
                {
                    Name = c.Value<string>(PropertyAliases.RegionName) ?? c.Name ?? string.Empty,
                    Url = c.Url() ?? string.Empty,
                })
                .ToList();
        }

        return GetView(section, model);
    }

    private IViewComponentResult GetView(string? section, NavigationViewModel model)
    {
        return section switch
        {
            "footerCategories" => View("FooterCategories", model),
            "footerRegions" => View("FooterRegions", model),
            _ => View("Default", model),
        };
    }
}
