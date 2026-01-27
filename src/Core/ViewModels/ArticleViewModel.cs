using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class ArticleViewModel : BasePageViewModel
{
    public required Article Article { get; set; }
    public IReadOnlyList<ArticleSummary> RelatedArticles { get; set; } = Array.Empty<ArticleSummary>();
    public ArticleSummary? PreviousArticle { get; set; }
    public ArticleSummary? NextArticle { get; set; }
}
