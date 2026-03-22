using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using PredelNews.Core.Interfaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace PredelNews.Core.Notifications;

public class ArticleUnpublishedHandler : INotificationAsyncHandler<ContentUnpublishedNotification>
{
    private readonly ICacheInvalidationService _cacheInvalidation;
    private readonly IAuditLogRepository _auditLog;
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly ILogger<ArticleUnpublishedHandler> _logger;

    public ArticleUnpublishedHandler(
        ICacheInvalidationService cacheInvalidation,
        IAuditLogRepository auditLog,
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        ILogger<ArticleUnpublishedHandler> logger)
    {
        _cacheInvalidation = cacheInvalidation;
        _auditLog = auditLog;
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _logger = logger;
    }

    public async Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.UnpublishedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            // Evict cache for pages that aggregate articles (home page, all-news archive).
            // Individual article/category/region pages rely on the 60s TTL for freshness.
            await _cacheInvalidation.EvictByTagAsync("home", cancellationToken);
            await _cacheInvalidation.EvictByTagAsync("allnews", cancellationToken);

            _logger.LogInformation("Evicted output cache for unpublished article {ArticleId}", entity.Id);

            // Audit log
            var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            await _auditLog.LogAsync(
                "article.unpublished",
                currentUser?.Id,
                currentUser?.Username,
                "article",
                entity.Id,
                null,
                null);
        }
    }
}
