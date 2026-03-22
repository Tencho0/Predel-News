using Microsoft.AspNetCore.OutputCaching;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Web.NotificationHandlers;

public class SitemapCacheInvalidator : INotificationAsyncHandler<ContentPublishedNotification>
{
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ILogger<SitemapCacheInvalidator> _logger;

    public SitemapCacheInvalidator(
        IOutputCacheStore outputCacheStore,
        ILogger<SitemapCacheInvalidator> logger)
    {
        _outputCacheStore = outputCacheStore;
        _logger = logger;
    }

    public async Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        await _outputCacheStore.EvictByTagAsync("sitemap", cancellationToken);
        _logger.LogInformation("Evicted sitemap output cache after content publish");
    }
}
