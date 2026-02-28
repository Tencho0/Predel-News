using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Core.Notifications;

public class ArticleTagCountValidator : INotificationAsyncHandler<ContentSavingNotification>
{
    private const int MaxTags = 10;
    private readonly ILogger<ArticleTagCountValidator> _logger;

    public ArticleTagCountValidator(ILogger<ArticleTagCountValidator> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.SavedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            var tagsValue = entity.GetValue<string>(PropertyAliases.Tags);
            if (string.IsNullOrWhiteSpace(tagsValue))
                continue;

            var tagCount = CountTags(tagsValue);
            if (tagCount > MaxTags)
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    $"Максималният брой тагове е {MaxTags}. Избрали сте {tagCount}.",
                    EventMessageType.Error));
                _logger.LogWarning("Article {Id}: Tag count {Count} exceeds maximum of {Max}",
                    entity.Id, tagCount, MaxTags);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    internal static int CountTags(string tagsValue)
    {
        if (string.IsNullOrWhiteSpace(tagsValue))
            return 0;

        // Umbraco stores picker values as comma-separated UDIs/GUIDs.
        // Count non-empty segments after splitting by comma.
        return tagsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }
}
