using PredelNews.Core.Notifications;
using PredelNews.Web.NotificationHandlers;
using PredelNews.Web.Setup;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace PredelNews.Web.Composers;

public class ContentSetupComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Data type and template setup must run before content tree setup
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TinyMceConfigSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentTypeSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TemplateSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, ContentTreeSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, TaxonomySeedSetup>();
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, UserGroupSetup>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, SponsoredContentGuardHandler>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, CoverImageAltTextValidator>();
        builder.AddNotificationAsyncHandler<ContentSavingNotification, ArticleTagCountValidator>();
        builder.AddNotificationAsyncHandler<ContentDeletingNotification, TaxonomyDeleteGuardHandler>();

        // EPIC-04: Editorial workflow
        builder.AddNotificationAsyncHandler<ContentSavingNotification, ArticleWorkflowGuardHandler>();
        builder.AddNotificationAsyncHandler<ContentPublishingNotification, ArticlePublishingHandler>();
        builder.AddNotificationAsyncHandler<ContentPublishedNotification, ArticlePublishedHandler>();
        builder.AddNotificationAsyncHandler<ContentUnpublishedNotification, ArticleUnpublishedHandler>();

        // EPIC-08: SEO — sitemap cache invalidation on any content publish
        builder.AddNotificationAsyncHandler<ContentPublishedNotification, SitemapCacheInvalidator>();
    }
}
