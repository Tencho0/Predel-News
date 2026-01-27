using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Infrastructure.Caching;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace PredelNews.Infrastructure.Services;

public class UmbracoCategoryService : ICategoryService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UmbracoCategoryService> _logger;

    public UmbracoCategoryService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback,
        IPublishedUrlProvider publishedUrlProvider,
        ICacheService cacheService,
        ILogger<UmbracoCategoryService> logger)
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

    public async Task<Category?> GetBySlugAsync(string slug)
    {
        var cacheKey = CacheKeys.Category(slug);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<Category?>(null);
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<Category?>(null);

            var categoryNode = root.Children?
                .FirstOrDefault(c => c.ContentType.Alias == "category" &&
                                      GetValue<string>(c, "slug")?.Equals(slug, StringComparison.OrdinalIgnoreCase) == true);

            if (categoryNode is null) return Task.FromResult<Category?>(null);

            var category = MapToCategory(categoryNode);
            category.ArticleCount = categoryNode.Children?
                .Count(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? 0;

            return Task.FromResult<Category?>(category);
        });
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return null;
        }

        var node = context.Content?.GetById(id);
        if (node is null || node.ContentType.Alias != "category")
        {
            return null;
        }

        var category = MapToCategory(node);
        category.ArticleCount = node.Children?
            .Count(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? 0;

        return await Task.FromResult(category);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.AllCategories, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<Category>>(Array.Empty<Category>());
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<IReadOnlyList<Category>>(Array.Empty<Category>());

            var categories = root.Children?
                .Where(c => c.ContentType.Alias == "category" && c.IsPublished())
                .OrderBy(c => c.SortOrder)
                .Select(c =>
                {
                    var category = MapToCategory(c);
                    category.ArticleCount = c.Children?
                        .Count(a => a.ContentType.Alias == "article" && a.IsPublished()) ?? 0;
                    return category;
                })
                .ToList() ?? new List<Category>();

            return Task.FromResult<IReadOnlyList<Category>>(categories);
        });
    }

    public async Task<IReadOnlyList<Category>> GetMainNavigationAsync()
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.NavigationCategories, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<Category>>(Array.Empty<Category>());
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<IReadOnlyList<Category>>(Array.Empty<Category>());

            var categories = root.Children?
                .Where(c => c.ContentType.Alias == "category" &&
                           c.IsPublished() &&
                           GetValue<bool>(c, "isMainNavigation"))
                .OrderBy(c => c.SortOrder)
                .Select(MapToCategory)
                .ToList() ?? new List<Category>();

            return Task.FromResult<IReadOnlyList<Category>>(categories);
        });
    }

    private Category MapToCategory(IPublishedContent node)
    {
        var image = GetValue<IPublishedContent>(node, "image");

        return new Category
        {
            Id = node.Id,
            Key = node.Key,
            Name = GetValue<string>(node, "name") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Description = GetValue<string>(node, "description"),
            Image = image is not null ? new MediaImage
            {
                Id = image.Id,
                Url = image.Url(_publishedUrlProvider) ?? string.Empty,
                AltText = GetValue<string>(image, "altText") ?? image.Name,
                Width = GetValue<int>(image, "umbracoWidth"),
                Height = GetValue<int>(image, "umbracoHeight")
            } : null,
            SortOrder = node.SortOrder,
            IsMainNavigation = GetValue<bool>(node, "isMainNavigation"),
            MetaTitle = GetValue<string>(node, "metaTitle"),
            MetaDescription = GetValue<string>(node, "metaDescription")
        };
    }
}
