using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface ISearchService
{
    Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 20);
    Task<IReadOnlyList<string>> GetSuggestionsAsync(string query, int count = 10);
}
