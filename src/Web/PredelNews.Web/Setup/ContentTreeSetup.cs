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
            _logger.LogInformation("Home page already exists — refreshing templates and republishing");

            // Umbraco's IContentService.Create() does not auto-set TemplateId from the document
            // type's default template. Content published before TemplateSetup ran has TemplateId=0
            // in NuCache, causing "No template exists to render the document" on every request.
            // Fix: explicitly set TemplateId to the doc-type default, save, then republish.
            ApplyDefaultTemplate(existingHome);
            _contentService.Save(existingHome);
            await PublishAsync(existingHome);

            // Apply the same fix to all published children (containers, static pages, contact).
            var allChildren = _contentService.GetPagedChildren(existingHome.Id, 0, 100, out _);
            foreach (var child in allChildren.Where(c => c.Published))
            {
                ApplyDefaultTemplate(child);
                _contentService.Save(child);
                await PublishAsync(child);
                _logger.LogInformation("Refreshed template for: {Name}", child.Name);
            }

            _logger.LogInformation("PredelNews: Content tree setup complete");
            return;
        }

        // --- First-boot: create the entire content tree ---

        var home = _contentService.Create("PredelNews", Constants.System.Root, DocumentTypes.HomePage);
        ApplyDefaultTemplate(home);
        _contentService.Save(home);
        await PublishAsync(home);

        // Container and archive nodes
        var children = new (string alias, string name)[]
        {
            (DocumentTypes.NewsRoot,    "novini"),
            (DocumentTypes.CategoryRoot, "kategoriya"),
            (DocumentTypes.RegionRoot,  "region"),
            (DocumentTypes.TagRoot,     "tag"),
            (DocumentTypes.AuthorRoot,  "avtor"),
            (DocumentTypes.AllNewsPage, "vsichki-novini"),
        };

        foreach (var (alias, name) in children)
        {
            var type = _contentTypeService.Get(alias);
            if (type == null) continue;

            var node = _contentService.Create(name, home.Id, alias);
            ApplyDefaultTemplate(node);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        // Static pages — use Latin URL names, set Bulgarian display title via property
        var staticPages = new (string urlName, string displayTitle)[]
        {
            ("za-nas",            "За нас"),
            ("reklama",           "Реклама"),
            ("reklamna-oferta",   "Рекламна оферта"),
        };

        foreach (var (urlName, displayTitle) in staticPages)
        {
            var node = _contentService.Create(urlName, home.Id, DocumentTypes.StaticPage);
            node.SetValue(PropertyAliases.PageTitle, displayTitle);
            ApplyDefaultTemplate(node);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        // Contact page
        var contactPage = _contentService.Create("kontakti", home.Id, DocumentTypes.ContactPage);
        contactPage.SetValue(PropertyAliases.PageTitle, "Контакти");
        ApplyDefaultTemplate(contactPage);
        _contentService.Save(contactPage);
        await PublishAsync(contactPage);

        // Privacy policy
        var privacyPage = _contentService.Create("politika-za-poveritelnost", home.Id, DocumentTypes.StaticPage);
        privacyPage.SetValue(PropertyAliases.PageTitle, "Политика за поверителност");
        ApplyDefaultTemplate(privacyPage);
        _contentService.Save(privacyPage);
        await PublishAsync(privacyPage);

        // Site settings — data container, not published, no template needed
        var settings = _contentService.Create("_settings", home.Id, DocumentTypes.SiteSettings);
        settings.SetValue(PropertyAliases.SiteName, "PredelNews");
        _contentService.Save(settings);

        _logger.LogInformation("PredelNews: Content tree setup complete");
    }

    /// <summary>
    /// Sets <see cref="IContent.TemplateId"/> to the default template of the content's document
    /// type. Must be called before <see cref="IContentService.Save"/> / PublishAsync so that
    /// NuCache stores a non-zero TemplateId and Umbraco can route the request correctly.
    /// </summary>
    private void ApplyDefaultTemplate(IContent content)
    {
        var contentType = _contentTypeService.Get(content.ContentType.Alias);
        var defaultTemplate = contentType?.DefaultTemplate;
        if (defaultTemplate != null)
            content.TemplateId = defaultTemplate.Id;
    }

    private async Task PublishAsync(IContent content)
    {
        var cultures = new[] { new CulturePublishScheduleModel { Culture = "*" } };
        await _contentPublishingService.PublishAsync(content.Key, cultures, SuperUserKey);
    }
}
