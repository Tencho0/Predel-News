using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class HomeViewModel : BasePageViewModel
{
    public IReadOnlyList<ArticleSummary> FeaturedArticles { get; set; } = Array.Empty<ArticleSummary>();
    public IReadOnlyList<ArticleSummary> BreakingNews { get; set; } = Array.Empty<ArticleSummary>();
    public IReadOnlyList<ArticleSummary> LatestArticles { get; set; } = Array.Empty<ArticleSummary>();
    public IReadOnlyList<ArticleSummary> MostReadArticles { get; set; } = Array.Empty<ArticleSummary>();
    public IReadOnlyList<CategoryBlock> CategoryBlocks { get; set; } = Array.Empty<CategoryBlock>();
}

public class CategoryBlock
{
    public required Category Category { get; set; }
    public IReadOnlyList<ArticleSummary> Articles { get; set; } = Array.Empty<ArticleSummary>();
}
