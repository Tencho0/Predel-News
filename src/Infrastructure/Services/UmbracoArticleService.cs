using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Infrastructure.Caching;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace PredelNews.Infrastructure.Services;

public class UmbracoArticleService : IArticleService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UmbracoArticleService> _logger;

    public UmbracoArticleService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback,
        IPublishedUrlProvider publishedUrlProvider,
        ICacheService cacheService,
        ILogger<UmbracoArticleService> logger)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedValueFallback = publishedValueFallback;
        _publishedUrlProvider = publishedUrlProvider;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Article?> GetBySlugAsync(string categorySlug, string articleSlug)
    {
        var cacheKey = CacheKeys.Article(categorySlug, articleSlug);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<Article?>(null);
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<Article?>(null);

            var categoryNode = root.Children?
                .FirstOrDefault(c => c.ContentType.Alias == "category" &&
                                      GetValue<string>(c, "slug")?.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) == true);

            if (categoryNode is null) return Task.FromResult<Article?>(null);

            var articleNode = categoryNode.Children?
                .FirstOrDefault(a => a.ContentType.Alias == "article" &&
                                      GetValue<string>(a, "slug")?.Equals(articleSlug, StringComparison.OrdinalIgnoreCase) == true);

            if (articleNode is null) return Task.FromResult<Article?>(null);

            return Task.FromResult<Article?>(MapToArticle(articleNode, categoryNode));
        });
    }

    public async Task<Article?> GetByIdAsync(int id)
    {
        var cacheKey = CacheKeys.ArticleById(id);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<Article?>(null);
            }

            var node = context.Content?.GetById(id);
            if (node is null || node.ContentType.Alias != "article")
            {
                return Task.FromResult<Article?>(null);
            }

            var categoryNode = node.Parent;
            return Task.FromResult<Article?>(MapToArticle(node, categoryNode));
        });
    }

    public async Task<PagedResult<ArticleSummary>> GetLatestAsync(int page = 1, int pageSize = 20)
    {
        return await _cacheService.GetOrSetAsync(
            $"{CacheKeys.LatestArticles}:{page}:{pageSize}",
            () => Task.FromResult(GetArticlesPaged(page, pageSize)));
    }

    public async Task<PagedResult<ArticleSummary>> GetByCategoryAsync(string categorySlug, int page = 1, int pageSize = 20)
    {
        var cacheKey = CacheKeys.CategoryArticles(categorySlug, page);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult(PagedResult<ArticleSummary>.Empty(pageSize));
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult(PagedResult<ArticleSummary>.Empty(pageSize));

            var categoryNode = root.Children?
                .FirstOrDefault(c => c.ContentType.Alias == "category" &&
                                      GetValue<string>(c, "slug")?.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) == true);

            if (categoryNode is null) return Task.FromResult(PagedResult<ArticleSummary>.Empty(pageSize));

            var articles = categoryNode.Children?
                .Where(a => a.ContentType.Alias == "article" && a.IsPublished())
                .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
                .ToList() ?? new List<IPublishedContent>();

            var totalCount = articles.Count;
            var pagedArticles = articles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToArticleSummary(a, categoryNode))
                .ToList();

            return Task.FromResult(new PagedResult<ArticleSummary>(pagedArticles, totalCount, page, pageSize));
        });
    }

    public async Task<PagedResult<ArticleSummary>> GetByTagAsync(string tagSlug, int page = 1, int pageSize = 20)
    {
        var cacheKey = CacheKeys.TagArticles(tagSlug, page);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult(PagedResult<ArticleSummary>.Empty(pageSize));
            }

            var articles = GetAllArticles()
                .Where(a =>
                {
                    var tags = GetValue<IEnumerable<IPublishedContent>>(a, "tags");
                    return tags?.Any(t => GetValue<string>(t, "slug")?.Equals(tagSlug, StringComparison.OrdinalIgnoreCase) == true) == true;
                })
                .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
                .ToList();

            var totalCount = articles.Count;
            var pagedArticles = articles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToArticleSummary(a, a.Parent))
                .ToList();

            return Task.FromResult(new PagedResult<ArticleSummary>(pagedArticles, totalCount, page, pageSize));
        });
    }

    public async Task<PagedResult<ArticleSummary>> GetByAuthorAsync(string authorSlug, int page = 1, int pageSize = 20)
    {
        var cacheKey = CacheKeys.AuthorArticles(authorSlug, page);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult(PagedResult<ArticleSummary>.Empty(pageSize));
            }

            var articles = GetAllArticles()
                .Where(a =>
                {
                    var author = GetValue<IPublishedContent>(a, "author");
                    return author != null && GetValue<string>(author, "slug")?.Equals(authorSlug, StringComparison.OrdinalIgnoreCase) == true;
                })
                .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
                .ToList();

            var totalCount = articles.Count;
            var pagedArticles = articles
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => MapToArticleSummary(a, a.Parent))
                .ToList();

            return Task.FromResult(new PagedResult<ArticleSummary>(pagedArticles, totalCount, page, pageSize));
        });
    }

    public async Task<IReadOnlyList<ArticleSummary>> GetFeaturedAsync(int count = 5)
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.FeaturedArticles, () =>
        {
            var articles = GetAllArticles()
                .Where(a => GetValue<bool>(a, "isFeatured"))
                .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
                .Take(count)
                .Select(a => MapToArticleSummary(a, a.Parent))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArticleSummary>>(articles);
        });
    }

    public async Task<IReadOnlyList<ArticleSummary>> GetBreakingNewsAsync(int count = 3)
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.BreakingNews, () =>
        {
            var articles = GetAllArticles()
                .Where(a => GetValue<bool>(a, "isBreakingNews"))
                .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
                .Take(count)
                .Select(a => MapToArticleSummary(a, a.Parent))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArticleSummary>>(articles);
        }, TimeSpan.FromMinutes(1));
    }

    public async Task<IReadOnlyList<ArticleSummary>> GetMostReadAsync(int days = 7, int count = 10)
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.MostRead, () =>
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var articles = GetAllArticles()
                .Where(a => GetValue<DateTime>(a, "publishDate") >= cutoffDate)
                .OrderByDescending(a => GetValue<int>(a, "viewCount"))
                .Take(count)
                .Select(a => MapToArticleSummary(a, a.Parent))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArticleSummary>>(articles);
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<IReadOnlyList<ArticleSummary>> GetRelatedAsync(int articleId, int count = 5)
    {
        var cacheKey = CacheKeys.RelatedArticles(articleId);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<ArticleSummary>>(Array.Empty<ArticleSummary>());
            }

            var articleNode = context.Content?.GetById(articleId);
            if (articleNode is null)
            {
                return Task.FromResult<IReadOnlyList<ArticleSummary>>(Array.Empty<ArticleSummary>());
            }

            var categoryNode = articleNode.Parent;
            var articleTags = GetValue<IEnumerable<IPublishedContent>>(articleNode, "tags")?.ToList() ?? new List<IPublishedContent>();
            var tagIds = articleTags.Select(t => t.Id).ToHashSet();

            var relatedByCategory = categoryNode?.Children?
                .Where(a => a.ContentType.Alias == "article" && a.Id != articleId && a.IsPublished())
                .ToList() ?? new List<IPublishedContent>();

            var scored = relatedByCategory
                .Select(a =>
                {
                    var aTags = GetValue<IEnumerable<IPublishedContent>>(a, "tags")?.ToList() ?? new List<IPublishedContent>();
                    var commonTags = aTags.Count(t => tagIds.Contains(t.Id));
                    return new { Article = a, Score = commonTags + 1 };
                })
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => GetValue<DateTime>(x.Article, "publishDate"))
                .Take(count)
                .Select(x => MapToArticleSummary(x.Article, categoryNode))
                .ToList();

            return Task.FromResult<IReadOnlyList<ArticleSummary>>(scored);
        });
    }

    public Task IncrementViewCountAsync(int articleId)
    {
        _logger.LogDebug("View count increment requested for article {ArticleId}", articleId);
        return Task.CompletedTask;
    }

    private T? GetValue<T>(IPublishedContent content, string alias)
    {
        return content.Value<T>(_publishedValueFallback, alias);
    }

    private PagedResult<ArticleSummary> GetArticlesPaged(int page, int pageSize)
    {
        var articles = GetAllArticles()
            .OrderByDescending(a => GetValue<DateTime>(a, "publishDate"))
            .ToList();

        var totalCount = articles.Count;
        var pagedArticles = articles
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToArticleSummary(a, a.Parent))
            .ToList();

        return new PagedResult<ArticleSummary>(pagedArticles, totalCount, page, pageSize);
    }

    private IEnumerable<IPublishedContent> GetAllArticles()
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return Enumerable.Empty<IPublishedContent>();
        }

        var root = context.Content?.GetAtRoot().FirstOrDefault();
        if (root is null) return Enumerable.Empty<IPublishedContent>();

        return root.Children?
            .Where(c => c.ContentType.Alias == "category")
            .SelectMany(c => c.Children?.Where(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? Enumerable.Empty<IPublishedContent>())
            ?? Enumerable.Empty<IPublishedContent>();
    }

    private Article MapToArticle(IPublishedContent node, IPublishedContent? categoryNode)
    {
        var authorNode = GetValue<IPublishedContent>(node, "author");
        var featuredImage = GetValue<IPublishedContent>(node, "featuredImage");
        var tags = GetValue<IEnumerable<IPublishedContent>>(node, "tags")?.ToList() ?? new List<IPublishedContent>();

        return new Article
        {
            Id = node.Id,
            Key = node.Key,
            Title = GetValue<string>(node, "title") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Subtitle = GetValue<string>(node, "subtitle"),
            Content = GetValue<string>(node, "content") ?? string.Empty,
            Excerpt = GetValue<string>(node, "excerpt"),
            PublishDate = GetValue<DateTime>(node, "publishDate"),
            UpdatedDate = node.UpdateDate,
            Author = MapToAuthor(authorNode),
            Category = MapToCategory(categoryNode),
            Tags = tags.Select(MapToTag).ToList(),
            FeaturedImage = MapToMediaImage(featuredImage),
            ViewCount = GetValue<int>(node, "viewCount"),
            IsFeatured = GetValue<bool>(node, "isFeatured"),
            IsBreakingNews = GetValue<bool>(node, "isBreakingNews"),
            MetaTitle = GetValue<string>(node, "metaTitle"),
            MetaDescription = GetValue<string>(node, "metaDescription"),
            CanonicalUrl = GetValue<string>(node, "canonicalUrl")
        };
    }

    private ArticleSummary MapToArticleSummary(IPublishedContent node, IPublishedContent? categoryNode)
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
            AuthorName = authorNode != null ? GetValue<string>(authorNode, "name") ?? "Unknown" : "Unknown",
            CategoryName = categoryNode != null ? GetValue<string>(categoryNode, "name") ?? categoryNode.Name : "Uncategorized",
            CategorySlug = categoryNode != null ? GetValue<string>(categoryNode, "slug") ?? categoryNode.UrlSegment ?? string.Empty : string.Empty,
            FeaturedImage = MapToMediaImage(featuredImage),
            ViewCount = GetValue<int>(node, "viewCount"),
            IsFeatured = GetValue<bool>(node, "isFeatured"),
            IsBreakingNews = GetValue<bool>(node, "isBreakingNews")
        };
    }

    private Author MapToAuthor(IPublishedContent? node)
    {
        if (node is null)
        {
            return new Author
            {
                Name = "Unknown",
                Slug = "unknown"
            };
        }

        return new Author
        {
            Id = node.Id,
            Key = node.Key,
            Name = GetValue<string>(node, "name") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Bio = GetValue<string>(node, "bio"),
            Email = GetValue<string>(node, "email"),
            Avatar = MapToMediaImage(GetValue<IPublishedContent>(node, "avatar")),
            TwitterHandle = GetValue<string>(node, "twitterHandle"),
            FacebookUrl = GetValue<string>(node, "facebookUrl"),
            LinkedInUrl = GetValue<string>(node, "linkedInUrl")
        };
    }

    private Category MapToCategory(IPublishedContent? node)
    {
        if (node is null)
        {
            return new Category
            {
                Name = "Uncategorized",
                Slug = "uncategorized"
            };
        }

        return new Category
        {
            Id = node.Id,
            Key = node.Key,
            Name = GetValue<string>(node, "name") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Description = GetValue<string>(node, "description"),
            Image = MapToMediaImage(GetValue<IPublishedContent>(node, "image")),
            SortOrder = node.SortOrder,
            IsMainNavigation = GetValue<bool>(node, "isMainNavigation"),
            MetaTitle = GetValue<string>(node, "metaTitle"),
            MetaDescription = GetValue<string>(node, "metaDescription")
        };
    }

    private Tag MapToTag(IPublishedContent node)
    {
        return new Tag
        {
            Id = node.Id,
            Key = node.Key,
            Name = GetValue<string>(node, "name") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty
        };
    }

    private MediaImage? MapToMediaImage(IPublishedContent? node)
    {
        if (node is null) return null;

        return new MediaImage
        {
            Id = node.Id,
            Url = node.Url(_publishedUrlProvider) ?? string.Empty,
            AltText = GetValue<string>(node, "altText") ?? node.Name,
            Width = GetValue<int>(node, "umbracoWidth"),
            Height = GetValue<int>(node, "umbracoHeight")
        };
    }
}
