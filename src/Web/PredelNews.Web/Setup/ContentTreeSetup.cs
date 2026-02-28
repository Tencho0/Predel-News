using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Web.Setup;

public class ContentTreeSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _contentPublishingService;
    private readonly ILogger<ContentTreeSetup> _logger;

    private static readonly Guid SuperUserKey = Constants.Security.SuperUserKey;

    public ContentTreeSetup(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService contentPublishingService,
        ILogger<ContentTreeSetup> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _contentPublishingService = contentPublishingService;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredelNews: Setting up content tree...");

        var homePageType = _contentTypeService.Get(DocumentTypes.HomePage);
        if (homePageType == null)
        {
            _logger.LogWarning("HomePage document type not found — skipping content tree setup");
            return;
        }

        var rootContent = _contentService.GetRootContent();
        var existingHome = rootContent?.FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.HomePage);

        if (existingHome != null)
        {
            _logger.LogInformation("Home page already exists — skipping content tree setup");
            return;
        }

        // Create home page
        var home = _contentService.Create("PredelNews", Constants.System.Root, DocumentTypes.HomePage);
        _contentService.Save(home);
        await PublishAsync(home);

        // Create children under home page
        var children = new (string alias, string name)[]
        {
            (DocumentTypes.NewsRoot, "Новини"),
            (DocumentTypes.CategoryRoot, "Категории"),
            (DocumentTypes.RegionRoot, "Региони"),
            (DocumentTypes.TagRoot, "Тагове"),
            (DocumentTypes.AuthorRoot, "Автори"),
            (DocumentTypes.AllNewsPage, "Всички новини"),
        };

        foreach (var (alias, name) in children)
        {
            var type = _contentTypeService.Get(alias);
            if (type == null) continue;

            var node = _contentService.Create(name, home.Id, alias);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        // Static pages
        foreach (var name in new[] { "За нас", "Реклама", "Рекламна оферта" })
        {
            var node = _contentService.Create(name, home.Id, DocumentTypes.StaticPage);
            node.SetValue(PropertyAliases.PageTitle, name);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        // Contact page
        var contactPage = _contentService.Create("Контакти", home.Id, DocumentTypes.ContactPage);
        contactPage.SetValue(PropertyAliases.PageTitle, "Контакти");
        _contentService.Save(contactPage);
        await PublishAsync(contactPage);

        // Privacy policy
        var privacyPage = _contentService.Create("Политика за поверителност", home.Id, DocumentTypes.StaticPage);
        privacyPage.SetValue(PropertyAliases.PageTitle, "Политика за поверителност");
        _contentService.Save(privacyPage);
        await PublishAsync(privacyPage);

        // Site settings (not published)
        var settings = _contentService.Create("_settings", home.Id, DocumentTypes.SiteSettings);
        settings.SetValue(PropertyAliases.SiteName, "PredelNews");
        _contentService.Save(settings);

        _logger.LogInformation("PredelNews: Content tree setup complete");
    }

    private async Task PublishAsync(IContent content)
    {
        var cultures = new[] { new CulturePublishScheduleModel { Culture = "*" } };
        await _contentPublishingService.PublishAsync(content.Key, cultures, SuperUserKey);
    }
}
