using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Core.Notifications;

public class TaxonomyDeleteGuardHandler : INotificationAsyncHandler<ContentDeletingNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly ILogger<TaxonomyDeleteGuardHandler> _logger;

    private static readonly Dictionary<string, (string propertyAlias, string label)> GuardedTypes = new()
    {
        [DocumentTypes.Category] = (PropertyAliases.Category, "категорията"),
        [DocumentTypes.Region] = (PropertyAliases.Region, "региона"),
    };

    public TaxonomyDeleteGuardHandler(
        IContentService contentService,
        IContentTypeService contentTypeService,
        ILogger<TaxonomyDeleteGuardHandler> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _logger = logger;
    }

    public Task HandleAsync(ContentDeletingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.DeletedEntities)
        {
            var alias = entity.ContentType.Alias;
            if (!GuardedTypes.TryGetValue(alias, out var guard))
                continue;

            var articleType = _contentTypeService.Get(DocumentTypes.Article);
            if (articleType == null)
                continue;

            var entityKey = entity.Key.ToString("D");
            var count = CountReferencingArticles(articleType.Id, guard.propertyAlias, entityKey);

            if (count > 0)
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    $"Не може да изтриете {guard.label} — {count} {(count == 1 ? "статия я използва" : "статии я използват")}.",
                    EventMessageType.Error));
                _logger.LogWarning("{Type} {Id} deletion blocked: {Count} articles reference it",
                    alias, entity.Id, count);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private int CountReferencingArticles(int articleTypeId, string propertyAlias, string entityKey)
    {
        // Load articles in pages to avoid loading everything at once
        const int pageSize = 500;
        long pageIndex = 0;
        int count = 0;

        while (true)
        {
            var articles = _contentService.GetPagedOfTypes(
                new[] { articleTypeId }, pageIndex, pageSize, out long total, null, null);

            foreach (var article in articles)
            {
                var value = article.GetValue<string>(propertyAlias);
                if (value != null && value.Contains(entityKey, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            if ((pageIndex + 1) * pageSize >= total)
                break;
            pageIndex++;
        }

        return count;
    }
}
