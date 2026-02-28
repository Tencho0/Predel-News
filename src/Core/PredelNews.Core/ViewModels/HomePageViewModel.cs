namespace PredelNews.Core.ViewModels;

public class HomePageViewModel : BasePageViewModel
{
    public ArticleSummaryViewModel? FeaturedArticle { get; set; }
    public List<ArticleSummaryViewModel> HeadlineLinks { get; set; } = [];
    public List<ArticleSummaryViewModel> NationalHeadlines { get; set; } = [];
    public List<CategoryBlockViewModel> CategoryBlocks { get; set; } = [];
    public List<ArticleSummaryViewModel> LatestArticles { get; set; } = [];
    public int LatestArticlesTotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
}
