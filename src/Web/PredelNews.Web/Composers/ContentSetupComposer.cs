using PredelNews.Core.Notifications;
using PredelNews.Web.Setup;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Web.Composers;

public class ContentSetupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Data type setup must run before content type setup
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TinyMceConfigSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentTypeSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentTreeSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TaxonomySeedSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, UserGroupSetup>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, SponsoredContentGuardHandler>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, CoverImageAltTextValidator>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, ArticleTagCountValidator>();
    }
}
