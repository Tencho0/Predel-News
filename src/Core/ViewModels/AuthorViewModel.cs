using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class AuthorViewModel : BasePageViewModel
{
    public required Author Author { get; set; }
    public PagedResult<ArticleSummary> Articles { get; set; } = PagedResult<ArticleSummary>.Empty();
}
