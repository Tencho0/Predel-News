using PredelNews.Core.Models;

namespace PredelNews.Core.ViewModels;

public class SearchViewModel : BasePageViewModel
{
    public string Query { get; set; } = string.Empty;
    public SearchResult? Result { get; set; }
    public bool HasSearched => !string.IsNullOrWhiteSpace(Query);
}
