using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Infrastructure.Caching;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace PredelNews.Infrastructure.Services;

public class UmbracoTagService : ITagService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UmbracoTagService> _logger;

    public UmbracoTagService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback,
        ICacheService cacheService,
        ILogger<UmbracoTagService> logger)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _publishedValueFallback = publishedValueFallback;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Tag?> GetBySlugAsync(string slug)
    {
        var cacheKey = CacheKeys.Tag(slug);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<Tag?>(null);
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<Tag?>(null);

            var tagsContainer = root.Children?.FirstOrDefault(c => c.ContentType.Alias == "tagsContainer");
            if (tagsContainer is null) return Task.FromResult<Tag?>(null);

            var tagNode = tagsContainer.Children?
                .FirstOrDefault(t => t.ContentType.Alias == "tag" &&
                                      GetValue<string>(t, "slug")?.Equals(slug, StringComparison.OrdinalIgnoreCase) == true);

            if (tagNode is null) return Task.FromResult<Tag?>(null);

            return Task.FromResult<Tag?>(MapToTag(tagNode));
        });
    }

    public async Task<Tag?> GetByIdAsync(int id)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return null;
        }

        var node = context.Content?.GetById(id);
        if (node is null || node.ContentType.Alias != "tag")
        {
            return null;
        }

        return await Task.FromResult(MapToTag(node));
    }

    public async Task<IReadOnlyList<Tag>> GetAllAsync()
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.AllTags, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());

            var tagsContainer = root.Children?.FirstOrDefault(c => c.ContentType.Alias == "tagsContainer");
            if (tagsContainer is null) return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());

            var tags = tagsContainer.Children?
                .Where(t => t.ContentType.Alias == "tag" && t.IsPublished())
                .Select(MapToTag)
                .ToList() ?? new List<Tag>();

            return Task.FromResult<IReadOnlyList<Tag>>(tags);
        });
    }

    public async Task<IReadOnlyList<Tag>> GetPopularAsync(int count = 20)
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.PopularTags, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());

            var tagsContainer = root.Children?.FirstOrDefault(c => c.ContentType.Alias == "tagsContainer");
            if (tagsContainer is null) return Task.FromResult<IReadOnlyList<Tag>>(Array.Empty<Tag>());

            var allTags = tagsContainer.Children?
                .Where(t => t.ContentType.Alias == "tag" && t.IsPublished())
                .ToList() ?? new List<IPublishedContent>();

            var allArticles = root.Children?
                .Where(c => c.ContentType.Alias == "category")
                .SelectMany(c => c.Children?.Where(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? Enumerable.Empty<IPublishedContent>())
                .ToList() ?? new List<IPublishedContent>();

            var tagCounts = allTags.ToDictionary(
                t => t.Id,
                t => allArticles.Count(a =>
                {
                    var articleTags = GetValue<IEnumerable<IPublishedContent>>(a, "tags");
                    return articleTags?.Any(at => at.Id == t.Id) == true;
                }));

            var popularTags = allTags
                .Select(t =>
                {
                    var tag = MapToTag(t);
                    tag.ArticleCount = tagCounts.GetValueOrDefault(t.Id, 0);
                    return tag;
                })
                .Where(t => t.ArticleCount > 0)
                .OrderByDescending(t => t.ArticleCount)
                .Take(count)
                .ToList();

            return Task.FromResult<IReadOnlyList<Tag>>(popularTags);
        }, TimeSpan.FromMinutes(30));
    }

    private T? GetValue<T>(IPublishedContent content, string alias)
    {
        return content.Value<T>(_publishedValueFallback, alias);
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
}
