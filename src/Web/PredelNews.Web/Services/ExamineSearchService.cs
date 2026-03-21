using System.Globalization;
using System.Text.RegularExpressions;
using Examine;
using Examine.Search;
using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;

namespace PredelNews.Web.Services;

public class ExamineSearchService : ISearchService
{
    private readonly IExamineManager _examineManager;
    private readonly ILogger<ExamineSearchService> _logger;

    public ExamineSearchService(IExamineManager examineManager, ILogger<ExamineSearchService> logger)
    {
        _examineManager = examineManager;
        _logger = logger;
    }

    public SearchPageViewModel Search(string? query, int page = 1)
    {
        var displayQuery = query?.Trim() ?? "";
        var sanitizedQuery = SearchQuerySanitizer.Sanitize(query);

        var baseBreadcrumbs = new List<BreadcrumbItem>
        {
            new() { Name = "Начало", Url = "/" },
            new() { Name = "Търсене" },
        };

        if (sanitizedQuery is null)
        {
            return new SearchPageViewModel
            {
                Query = displayQuery,
                Breadcrumbs = baseBreadcrumbs,
                PageTitle = "Търсене",
            };
        }

        if (!_examineManager.TryGetIndex(SearchConstants.ArticleIndexName, out var index))
        {
            _logger.LogWarning("PredelNews.Search: Article index '{IndexName}' not found", SearchConstants.ArticleIndexName);
            return new SearchPageViewModel
            {
                Query = displayQuery,
                HasError = true,
                ErrorMessage = "Търсенето временно не е налично. Моля, опитайте отново по-късно.",
                Breadcrumbs = baseBreadcrumbs,
                PageTitle = "Търсене",
            };
        }

        var searcher = index.Searcher;
        var results = searcher.CreateQuery("content")
            .ManagedQuery(sanitizedQuery, new[] { PropertyAliases.Headline, PropertyAliases.Subtitle, "bodyText", PropertyAliases.Tags })
            .Execute(QueryOptions.SkipTake((page - 1) * SearchConstants.SearchPageSize, SearchConstants.SearchPageSize));

        var totalResults = (int)results.TotalItemCount;
        var totalPages = (int)Math.Ceiling(totalResults / (double)SearchConstants.SearchPageSize);
        var queryWords = displayQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var searchResults = results.Select(r => MapSearchResult(r, queryWords)).ToList();

        return new SearchPageViewModel
        {
            Query = displayQuery,
            Results = searchResults,
            TotalResults = totalResults,
            Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalResults,
                PageSize = SearchConstants.SearchPageSize,
                BaseUrl = "/tarsene",
            },
            Breadcrumbs = baseBreadcrumbs,
            PageTitle = string.IsNullOrEmpty(displayQuery) ? "Търсене" : $"Търсене: {displayQuery}",
        };
    }

    private static SearchResultViewModel MapSearchResult(ISearchResult result, string[] queryWords)
    {
        var headline = result.Values.GetValueOrDefault(PropertyAliases.Headline) ?? "";
        var bodyText = result.Values.GetValueOrDefault("bodyText") ?? "";
        var articleUrl = result.Values.GetValueOrDefault("articleUrl") ?? "";
        var categoryName = result.Values.GetValueOrDefault(PropertyAliases.CategoryName);
        var regionName = result.Values.GetValueOrDefault(PropertyAliases.RegionName);
        var publishDateStr = result.Values.GetValueOrDefault(PropertyAliases.PublishDate) ?? "";
        var isSponsoredStr = result.Values.GetValueOrDefault(PropertyAliases.IsSponsored) ?? "0";

        var excerpt = bodyText.Length > 200 ? bodyText[..200] + "..." : bodyText;

        return new SearchResultViewModel
        {
            Headline = HighlightTerms(headline, queryWords),
            Excerpt = HighlightTerms(excerpt, queryWords),
            ArticleUrl = articleUrl,
            CategoryName = string.IsNullOrWhiteSpace(categoryName) ? null : categoryName,
            RegionName = string.IsNullOrWhiteSpace(regionName) ? null : regionName,
            PublishDate = DateTime.TryParse(publishDateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : DateTime.MinValue,
            IsSponsored = isSponsoredStr == "1",
        };
    }

    private static string HighlightTerms(string text, string[] queryWords)
    {
        if (string.IsNullOrEmpty(text) || queryWords.Length == 0)
            return text;

        foreach (var word in queryWords)
        {
            if (string.IsNullOrWhiteSpace(word))
                continue;

            text = Regex.Replace(
                text,
                Regex.Escape(word),
                m => $"<mark>{m.Value}</mark>",
                RegexOptions.IgnoreCase);
        }

        return text;
    }
}
