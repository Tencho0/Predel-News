namespace PredelNews.Core.ViewModels;

public class CategoryBlockViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public string CategoryUrl { get; set; } = string.Empty;
    public List<ArticleSummaryViewModel> Articles { get; set; } = [];
}
