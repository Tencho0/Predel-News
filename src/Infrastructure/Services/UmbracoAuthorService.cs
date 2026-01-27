using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;
using PredelNews.Infrastructure.Caching;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace PredelNews.Infrastructure.Services;

public class UmbracoAuthorService : IAuthorService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IPublishedValueFallback _publishedValueFallback;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly ICacheService _cacheService;
    private readonly ILogger<UmbracoAuthorService> _logger;

    public UmbracoAuthorService(
        IUmbracoContextAccessor umbracoContextAccessor,
        IPublishedValueFallback publishedValueFallback,
        IPublishedUrlProvider publishedUrlProvider,
        ICacheService cacheService,
        ILogger<UmbracoAuthorService> logger)
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

    public async Task<Author?> GetBySlugAsync(string slug)
    {
        var cacheKey = CacheKeys.Author(slug);

        return await _cacheService.GetOrSetAsync(cacheKey, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<Author?>(null);
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<Author?>(null);

            var authorsContainer = root.Children?.FirstOrDefault(c => c.ContentType.Alias == "authorsContainer");
            if (authorsContainer is null) return Task.FromResult<Author?>(null);

            var authorNode = authorsContainer.Children?
                .FirstOrDefault(a => a.ContentType.Alias == "author" &&
                                      GetValue<string>(a, "slug")?.Equals(slug, StringComparison.OrdinalIgnoreCase) == true);

            if (authorNode is null) return Task.FromResult<Author?>(null);

            return Task.FromResult<Author?>(MapToAuthor(authorNode));
        });
    }

    public async Task<Author?> GetByIdAsync(int id)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
        {
            return null;
        }

        var node = context.Content?.GetById(id);
        if (node is null || node.ContentType.Alias != "author")
        {
            return null;
        }

        return await Task.FromResult(MapToAuthor(node));
    }

    public async Task<IReadOnlyList<Author>> GetAllAsync()
    {
        return await _cacheService.GetOrSetAsync(CacheKeys.AllAuthors, () =>
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var context))
            {
                return Task.FromResult<IReadOnlyList<Author>>(Array.Empty<Author>());
            }

            var root = context.Content?.GetAtRoot().FirstOrDefault();
            if (root is null) return Task.FromResult<IReadOnlyList<Author>>(Array.Empty<Author>());

            var authorsContainer = root.Children?.FirstOrDefault(c => c.ContentType.Alias == "authorsContainer");
            if (authorsContainer is null) return Task.FromResult<IReadOnlyList<Author>>(Array.Empty<Author>());

            var authors = authorsContainer.Children?
                .Where(a => a.ContentType.Alias == "author" && a.IsPublished())
                .Select(MapToAuthor)
                .ToList() ?? new List<Author>();

            return Task.FromResult<IReadOnlyList<Author>>(authors);
        });
    }

    private Author MapToAuthor(IPublishedContent node)
    {
        var avatar = GetValue<IPublishedContent>(node, "avatar");

        return new Author
        {
            Id = node.Id,
            Key = node.Key,
            Name = GetValue<string>(node, "name") ?? node.Name,
            Slug = GetValue<string>(node, "slug") ?? node.UrlSegment ?? string.Empty,
            Bio = GetValue<string>(node, "bio"),
            Email = GetValue<string>(node, "email"),
            Avatar = avatar is not null ? new MediaImage
            {
                Id = avatar.Id,
                Url = avatar.Url(_publishedUrlProvider) ?? string.Empty,
                AltText = GetValue<string>(avatar, "altText") ?? avatar.Name,
                Width = GetValue<int>(avatar, "umbracoWidth"),
                Height = GetValue<int>(avatar, "umbracoHeight")
            } : null,
            TwitterHandle = GetValue<string>(node, "twitterHandle"),
            FacebookUrl = GetValue<string>(node, "facebookUrl"),
            LinkedInUrl = GetValue<string>(node, "linkedInUrl")
        };
    }
}
