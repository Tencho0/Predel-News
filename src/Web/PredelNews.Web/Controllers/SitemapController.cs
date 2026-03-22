using System.Security;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace PredelNews.Web.Controllers;

[Route("sitemap.xml")]
public class SitemapController : Controller
{
    private readonly UmbracoHelper _umbracoHelper;

    public SitemapController(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    [HttpGet]
    [OutputCache(PolicyName = "PublicPage", Tags = ["sitemap"])]
    public IActionResult Index()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        foreach (var root in _umbracoHelper.ContentAtRoot())
        {
            AppendContent(sb, baseUrl, root);
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }

    private void AppendContent(StringBuilder sb, string baseUrl, Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent content)
    {
        // Check container types first — they have no template (URL = "#")
        // but we must still recurse into their children
        var alias = content.ContentType.Alias;
        if (alias is "siteSettings")
            return; // SiteSettings has no children to index

        if (alias is "newsRoot" or "categoryRoot" or "regionRoot" or "tagRoot" or "authorRoot")
        {
            if (content.Children != null)
            {
                foreach (var child in content.Children)
                {
                    AppendContent(sb, baseUrl, child);
                }
            }
            return;
        }

        var url = content.Url();

        // Skip content with no real URL (unpublished, no template, etc.)
        if (string.IsNullOrEmpty(url) || url == "#"
            || url.StartsWith("/umbraco/", StringComparison.OrdinalIgnoreCase))
            return;

        var escapedUrl = SecurityElement.Escape($"{baseUrl}{url}");
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{escapedUrl}</loc>");
        sb.AppendLine($"    <lastmod>{content.UpdateDate:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("  </url>");

        if (content.Children != null)
        {
            foreach (var child in content.Children)
            {
                AppendContent(sb, baseUrl, child);
            }
        }
    }
}
