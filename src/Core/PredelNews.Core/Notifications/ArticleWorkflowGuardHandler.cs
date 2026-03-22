using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using PredelNews.Core.Interfaces;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Core.Notifications;

public class ArticleWorkflowGuardHandler : INotificationAsyncHandler<ContentSavingNotification>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly IContentService _contentService;
    private readonly IAuditLogRepository _auditLog;
    private readonly ILogger<ArticleWorkflowGuardHandler> _logger;

    private const string WriterGroupAlias = "writer";
    private const string EditorGroupAlias = "editor";
    private const string AdminGroupAlias = "admin";

    public ArticleWorkflowGuardHandler(
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        IContentService contentService,
        IAuditLogRepository auditLog,
        ILogger<ArticleWorkflowGuardHandler> logger)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _contentService = contentService;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.SavedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            if (currentUser == null)
                continue;

            var userGroups = currentUser.Groups.Select(g => g.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var isWriter = userGroups.Contains(WriterGroupAlias)
                           && !userGroups.Contains(EditorGroupAlias)
                           && !userGroups.Contains(AdminGroupAlias);

            var newStatus = entity.GetValue<string>(PropertyAliases.ArticleStatus);

            // Default new articles to Draft
            if (string.IsNullOrWhiteSpace(newStatus))
            {
                entity.SetValue(PropertyAliases.ArticleStatus, "Draft");
                newStatus = "Draft";
            }

            // Get existing status (null for new articles)
            string? existingStatus = null;
            if (entity.Id > 0)
            {
                var existing = _contentService.GetById(entity.Id);
                existingStatus = existing?.GetValue<string>(PropertyAliases.ArticleStatus);
            }

            // Writer lock: if article is "In Review", writer cannot edit
            if (isWriter && string.Equals(existingStatus, "In Review", StringComparison.OrdinalIgnoreCase))
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Статията е заключена за редакция.",
                    EventMessageType.Error));
                _logger.LogWarning("Writer {User} attempted to edit article {ArticleId} while In Review",
                    currentUser.Username, entity.Id);
                return;
            }

            // Writers can only set Draft or In Review
            if (isWriter && !string.Equals(newStatus, "Draft", StringComparison.OrdinalIgnoreCase)
                         && !string.Equals(newStatus, "In Review", StringComparison.OrdinalIgnoreCase))
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Авторите могат да задават само статус \"Draft\" или \"In Review\".",
                    EventMessageType.Error));
                return;
            }

            // Log status change
            if (!string.Equals(existingStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                await _auditLog.LogAsync(
                    "article.status.changed",
                    currentUser.Id,
                    currentUser.Username,
                    "article",
                    entity.Id,
                    existingStatus,
                    newStatus);
            }
        }
    }
}
