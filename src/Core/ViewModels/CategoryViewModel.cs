using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class CategoryViewModel : BasePageViewModel
{
    public required Category Category { get; set; }
    public PagedResult<ArticleSummary> Articles { get; set; } = PagedResult<ArticleSummary>.Empty();
    public IReadOnlyList<ArticleSummary> FeaturedInCategory { get; set; } = Array.Empty<ArticleSummary>();
}
