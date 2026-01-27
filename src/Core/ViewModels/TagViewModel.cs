using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class TagViewModel : BasePageViewModel
{
    public required Tag Tag { get; set; }
    public PagedResult<ArticleSummary> Articles { get; set; } = PagedResult<ArticleSummary>.Empty();
}
