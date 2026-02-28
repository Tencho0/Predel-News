using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Web.Setup;

public class UserGroupSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly ILogger<UserGroupSetup> _logger;

    public UserGroupSetup(ILogger<UserGroupSetup> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        // User groups (Writer, Editor, Admin) are configured via the Umbraco backoffice.
        // Writer: Create/edit own content, no publish/schedule/unpublish, no Sponsored group, can browse media
        // Editor: All Writer + publish/schedule/unpublish, manage taxonomy/polls/authors, view/export email subscribers
        // Admin: Full access including isSponsored toggle, ad slots, users, site settings, audit logs
        // The SponsoredContentGuardHandler enforces isSponsored access server-side.
        _logger.LogInformation("PredelNews: User groups — configure Writer/Editor/Admin roles via backoffice");
        return Task.CompletedTask;
    }
}
