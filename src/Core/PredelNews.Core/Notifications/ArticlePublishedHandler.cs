using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using PredelNews.Core.Interfaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Core.Notifications;

public class ArticlePublishingHandler : INotificationAsyncHandler<ContentPublishingNotification>
{
    private readonly IContentService _contentService;
    private readonly IAuditLogRepository _auditLog;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly ILogger<ArticlePublishingHandler> _logger;

    public ArticlePublishingHandler(
        IContentService contentService,
        IAuditLogRepository auditLog,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        ILogger<ArticlePublishingHandler> logger)
    {
        _contentService = contentService;
        _auditLog = auditLog;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _logger = logger;
    }

    public async Task HandleAsync(ContentPublishingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.PublishedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            // Check if this is a re-publish (article was already published before)
            if (entity.Id > 0)
            {
                var existing = _contentService.GetById(entity.Id);
                if (existing?.Published == true)
                {
                    // This is a re-publish (correction) — set updatedDate
                    entity.SetValue(PropertyAliases.UpdatedDate, DateTime.UtcNow);
                    _logger.LogInformation("Article {ArticleId} re-published — updatedDate set", entity.Id);

                    var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
                    await _auditLog.LogAsync(
                        "article.republished",
                        currentUser?.Id,
                        currentUser?.Username,
                        "article",
                        entity.Id,
                        null,
                        null);
                }
            }
        }
    }
}

public class ArticlePublishedHandler : INotificationAsyncHandler<ContentPublishedNotification>
{
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IAuditLogRepository _auditLog;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly ILogger<ArticlePublishedHandler> _logger;

    public ArticlePublishedHandler(
        ICacheInvalidationService cacheInvalidation,
        IAuditLogRepository auditLog,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        ILogger<ArticlePublishedHandler> logger)
    {
        _cacheInvalidation = cacheInvalidation;
        _auditLog = auditLog;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _logger = logger;
    }

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.PublishedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            // Evict cache for pages that aggregate articles (home page, all-news archive).
            // Individual article/category/region pages rely on the 60s TTL for freshness
            // since ASP.NET Core output caching doesn't support per-request dynamic tags.
            await _cacheInvalidation.EvictByTagAsync("home", cancellationToken);
            await _cacheInvalidation.EvictByTagAsync("allnews", cancellationToken);

            _logger.LogInformation("Evicted output cache for published article {ArticleId}", entity.Id);

            // Audit log
            var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            await _auditLog.LogAsync(
                "article.published",
                currentUser?.Id,
                currentUser?.Username,
                "article",
                entity.Id,
                null,
                null);
        }
    }
}
