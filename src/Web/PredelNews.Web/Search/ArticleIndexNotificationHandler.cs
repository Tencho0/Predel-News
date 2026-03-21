using Examine;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Web.Search;

public class ArticlePublishedIndexHandler : INotificationAsyncHandler<ContentPublishedNotification>
{
    private readonly IExamineManager _examineManager;
    private readonly ArticleValueSetBuilder _valueSetBuilder;
    private readonly ILogger<ArticlePublishedIndexHandler> _logger;

    public ArticlePublishedIndexHandler(
        IExamineManager examineManager,
        ArticleValueSetBuilder valueSetBuilder,
        ILogger<ArticlePublishedIndexHandler> logger)
    {
        _examineManager = examineManager;
        _valueSetBuilder = valueSetBuilder;
        _logger = logger;
    }

    public Task HandleAsync(ContentPublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!_examineManager.TryGetIndex(SearchConstants.ArticleIndexName, out var index))
        {
            _logger.LogWarning("PredelNews.Search: Article index '{IndexName}' not found. Cannot index published content", SearchConstants.ArticleIndexName);
            return Task.CompletedTask;
        }

        foreach (var content in notification.PublishedEntities)
        {
            if (content.ContentType.Alias != DocumentTypes.Article)
                continue;

            var valueSet = _valueSetBuilder.BuildValueSet(content);
            index.IndexItems(new[] { valueSet });
            _logger.LogDebug("PredelNews.Search: Indexed article {ContentId} in {IndexName}", content.Id, SearchConstants.ArticleIndexName);
        }

        return Task.CompletedTask;
    }
}

public class ArticleUnpublishedIndexHandler : INotificationAsyncHandler<ContentUnpublishedNotification>
{
    private readonly IExamineManager _examineManager;
    private readonly ILogger<ArticleUnpublishedIndexHandler> _logger;

    public ArticleUnpublishedIndexHandler(
        IExamineManager examineManager,
        ILogger<ArticleUnpublishedIndexHandler> logger)
    {
        _examineManager = examineManager;
        _logger = logger;
    }

    public Task HandleAsync(ContentUnpublishedNotification notification, CancellationToken cancellationToken)
    {
        if (!_examineManager.TryGetIndex(SearchConstants.ArticleIndexName, out var index))
        {
            _logger.LogWarning("PredelNews.Search: Article index '{IndexName}' not found. Cannot remove unpublished content", SearchConstants.ArticleIndexName);
            return Task.CompletedTask;
        }

        foreach (var content in notification.UnpublishedEntities)
        {
            if (content.ContentType.Alias != DocumentTypes.Article)
                continue;

            index.DeleteFromIndex(new[] { content.Id.ToString() });
            _logger.LogDebug("PredelNews.Search: Removed article {ContentId} from {IndexName}", content.Id, SearchConstants.ArticleIndexName);
        }

        return Task.CompletedTask;
    }
}
