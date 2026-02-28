using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Security;

namespace PredelNews.Core.Notifications;

public class SponsoredContentGuardHandler : INotificationAsyncHandler<ContentSavingNotification>
{
    private readonly IBackOfficeSecurityAccessor _backOfficeSecurityAccessor;
    private readonly ILogger<SponsoredContentGuardHandler> _logger;

    private const string AdminGroupAlias = "admin";

    public SponsoredContentGuardHandler(
        IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
        ILogger<SponsoredContentGuardHandler> logger)
    {
        _backOfficeSecurityAccessor = backOfficeSecurityAccessor;
        _logger = logger;
    }

    public Task HandleAsync(ContentSavingNotification notification, CancellationToken cancellationToken)
    {
        foreach (var entity in notification.SavedEntities)
        {
            if (entity.ContentType.Alias != DocumentTypes.Article)
                continue;

            var isSponsored = entity.GetValue<bool>(PropertyAliases.IsSponsored);
            if (!isSponsored)
                continue;

            // Check if user is admin
            var currentUser = _backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
            if (currentUser == null)
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Не може да се зададе спонсорирано съдържание без автентикация.",
                    EventMessageType.Error));
                return Task.CompletedTask;
            }

            var isAdmin = currentUser.Groups.Any(g => g.Alias == AdminGroupAlias);
            if (!isAdmin)
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Само администратори могат да маркират съдържание като спонсорирано.",
                    EventMessageType.Error));
                _logger.LogWarning("User {User} attempted to set isSponsored on article {ArticleId}",
                    currentUser.Username, entity.Id);
                return Task.CompletedTask;
            }

            // Validate sponsor name is provided
            var sponsorName = entity.GetValue<string>(PropertyAliases.SponsorName);
            if (string.IsNullOrWhiteSpace(sponsorName))
            {
                notification.CancelOperation(new EventMessage("Грешка",
                    "Моля, въведете име на спонсора при маркиране като спонсорирано съдържание.",
                    EventMessageType.Error));
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }
}
