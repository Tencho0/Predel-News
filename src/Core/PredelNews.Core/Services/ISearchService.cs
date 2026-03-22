using PredelNews.Core.ViewModels;

namespace PredelNews.Core.Services;

public interface ISearchService
{
    SearchPageViewModel Search(string? query, int page = 1);
}
