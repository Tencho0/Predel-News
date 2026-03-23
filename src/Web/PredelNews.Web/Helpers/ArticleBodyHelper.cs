using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using PredelNews.Core.Services;

namespace PredelNews.Web.Helpers;

public static class ArticleBodyHelper
{
    public static IHtmlContent RenderArticleBody(
        this IHtmlHelper html,
        string body,
        bool isSponsored)
    {
        var processed = ArticleBodyProcessor.Process(body, isSponsored);
        return new HtmlString(processed);
    }
}
