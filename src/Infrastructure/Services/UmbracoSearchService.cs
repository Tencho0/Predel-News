using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Infrastructure.Caching;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace PredelNews.Infrastructure.Services;

public class UmbracoSearchService : ISearchService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UmbracoSearchService> _logger;

    public UmbracoSearchService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback,
        IPublishedUrlProvider publishedUrlProvider,
        ICacheService cacheService,
        ILogger<UmbracoSearchService> logger)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedValueFallback = publishedValueFallback;
        _publishedUrlProvider = publishedUrlProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    private T? GetValue<T>(IPublishedContent content, string alias)
    {
        return content.Value<T>(_publishedValueFallback, alias);
    }

    public async Task<SearchResult> SearchAsync(string query, int page = 1, int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new SearchResult { Query = query };
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var cacheKey = CacheKeys.Search(normalizedQuery, page);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            var stopwatch = Stopwatch.StartNew();

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult(new SearchResult { Query = query });
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null)
            {
                return Task.FromResult(new SearchResult { Query = query });
            }

            var searchTerms = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var allArticles = root.Children?
                .Where(c => c.ContentType.Alias == "category")
                .SelectMany(c => c.Children?.Where(a => a.ContentType.Alias == "article" && a.IsPublished())
                    .Select(a => new { Article = a, Category = c }) ?? Enumerable.Empty<dynamic>())
                .ToList() ?? new List<dynamic>();

            var matchedArticles = allArticles
                .Where(item =>
                {
                    var title = (GetValue<string>(item.Article, "title") ?? item.Article.Name).ToLowerInvariant();
                    var excerpt = (GetValue<string>(item.Article, "excerpt") ?? string.Empty).ToLowerInvariant();
                    var content = (GetValue<string>(item.Article, "content") ?? string.Empty).ToLowerInvariant();

                    return searchTerms.All(term =>
                        title.Contains(term) ||
                        excerpt.Contains(term) ||
                        content.Contains(term));
                })
                .Select(item => new
                {
                    Article = (IPublishedContent)item.Article,
                    Category = (IPublishedContent)item.Category,
                    Score = CalculateRelevanceScore(item.Article, searchTerms)
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => GetValue<DateTime>(x.Article, "publishDate"))
                .ToList();

            var totalCount = matchedArticles.Count;
            var pagedArticles = matchedArticles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => MapToArticleSummary(x.Article, x.Category))
                .ToList();

            var relatedCategories = matchedArticles
                .Select(x => x.Category)
                .Distinct()
                .Take(5)
                .Select(c => new Category
                {
                    Id = c.Id,
                    Key = c.Key,
                    Name = GetValue<string>(c, "name") ?? c.Name,
                    Slug = GetValue<string>(c, "slug") ?? c.UrlSegment ?? string.Empty
                })
                .ToList();

            stopwatch.Stop();

            return Task.FromResult(new SearchResult
            {
                Query = query,
                Articles = new PagedResult<ArticleSummary>(pagedArticles, totalCount, page, pageSize),
                RelatedCategories = relatedCategories,
                SearchDuration = stopwatch.Elapsed
            });
        }, TimeSpan.FromMinutes(2));
    }

    public async Task<IReadOnlyList<string>> GetSuggestionsAsync(string query, int count = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Array.Empty<string>();
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return Array.Empty<string>();
        }

        var root = context.Content?.GetAtRoot().FirstOrDefault();
        if (root is null) return Array.Empty<string>();

        var suggestions = root.Children?
            .Where(c => c.ContentType.Alias == "category")
            .SelectMany(c => c.Children?.Where(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? Enumerable.Empty<IPublishedContent>())
            .Select(a => GetValue<string>(a, "title") ?? a.Name)
            .Where(title => title.ToLowerInvariant().Contains(normalizedQuery))
            .Distinct()
            .Take(count)
            .ToList() ?? new List<string>();

        return await Task.FromResult<IReadOnlyList<string>>(suggestions);
    }

    private int CalculateRelevanceScore(IPublishedContent article, string[] searchTerms)
    {
        var score = 0;
        var title = (GetValue<string>(article, "title") ?? article.Name).ToLowerInvariant();
        var excerpt = (GetValue<string>(article, "excerpt") ?? string.Empty).ToLowerInvariant();

        foreach (var term in searchTerms)
        {
            if (title.Contains(term))
            {
                score += 10;
                if (title.StartsWith(term)) score += 5;
            }
            if (excerpt.Contains(term)) score += 3;
        }

        if (GetValue<bool>(article, "isFeatured")) score += 2;
        if (GetValue<bool>(article, "isBreakingNews")) score += 1;

        return score;
    }

    private ArticleSummary MapToArticleSummary(IPublishedContent node, IPublishedContent categoryNode)
    {
        var authorNode = GetValue<IPublishedContent>(node, "author");
        var featuredImage = GetValue<IPublishedContent>(node, "featuredImage");

        return new ArticleSummary
        {
            Id = node.Id,
            Key = node.Key,
            Title = GetValue<string>(node, "title") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Excerpt = GetValue<string>(node, "excerpt"),
            PublishDate = GetValue<DateTime>(node, "publishDate"),
            AuthorName = authorNode is not null ? GetValue<string>(authorNode, "name") ?? "Unknown" : "Unknown",
            CategoryName = GetValue<string>(categoryNode, "name") ?? categoryNode.Name,
            CategorySlug = GetValue<string>(categoryNode, "slug") ?? categoryNode.UrlSegment ?? string.Empty,
            FeaturedImage = featuredImage is not null ? new MediaImage
            {
                Id = featuredImage.Id,
                Url = featuredImage.Url(_publishedUrlProvider) ?? string.Empty,
                AltText = GetValue<string>(featuredImage, "altText") ?? featuredImage.Name,
                Width = GetValue<int>(featuredImage, "umbracoWidth"),
                Height = GetValue<int>(featuredImage, "umbracoHeight")
            } : null,
            ViewCount = GetValue<int>(node, "viewCount"),
            IsFeatured = GetValue<bool>(node, "isFeatured"),
            IsBreakingNews = GetValue<bool>(node, "isBreakingNews")
        };
    }
}
