using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentPublishing;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace PredelNews.Web.Setup;

public class TaxonomySeedSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentPublishingService _contentPublishingService;
    private readonly ISlugGenerator _slugGenerator;
    private readonly ILogger<TaxonomySeedSetup> _logger;

    private static readonly Guid SuperUserKey = Constants.Security.SuperUserKey;

    public TaxonomySeedSetup(
        IContentService contentService,
        IContentTypeService contentTypeService,
        IContentPublishingService contentPublishingService,
        ISlugGenerator slugGenerator,
        ILogger<TaxonomySeedSetup> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _contentPublishingService = contentPublishingService;
        _slugGenerator = slugGenerator;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredelNews: Seeding taxonomy data...");

        await SeedCategoriesAsync();
        await SeedRegionsAsync();

        _logger.LogInformation("PredelNews: Taxonomy seed complete");
    }

    private async Task SeedCategoriesAsync()
    {
        var categoryType = _contentTypeService.Get(DocumentTypes.Category);
        if (categoryType == null) return;

        var categoryRoot = FindNodeByAlias(DocumentTypes.CategoryRoot);
        if (categoryRoot == null) return;

        var existingChildren = _contentService.GetPagedChildren(categoryRoot.Id, 0, 100, out _, null, null);
        if (existingChildren.Any())
        {
            _logger.LogInformation("Categories already seeded — skipping");
            return;
        }

        var categories = new[]
        {
            "Общество", "Политика", "Криминално", "Икономика / Бизнес",
            "Спорт", "Култура", "Любопитно", "Хайлайф",
        };

        foreach (var name in categories)
        {
            var node = _contentService.Create(name, categoryRoot.Id, DocumentTypes.Category);
            node.SetValue(PropertyAliases.CategoryName, name);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        _logger.LogInformation("Seeded {Count} categories", categories.Length);
    }

    private async Task SeedRegionsAsync()
    {
        var regionType = _contentTypeService.Get(DocumentTypes.Region);
        if (regionType == null) return;

        var regionRoot = FindNodeByAlias(DocumentTypes.RegionRoot);
        if (regionRoot == null) return;

        var existingChildren = _contentService.GetPagedChildren(regionRoot.Id, 0, 100, out _, null, null);
        if (existingChildren.Any())
        {
            _logger.LogInformation("Regions already seeded — skipping");
            return;
        }

        var regions = new[]
        {
            "Благоевград", "Кюстендил", "Перник", "София", "България",
        };

        foreach (var name in regions)
        {
            var node = _contentService.Create(name, regionRoot.Id, DocumentTypes.Region);
            node.SetValue(PropertyAliases.RegionName, name);
            _contentService.Save(node);
            await PublishAsync(node);
        }

        _logger.LogInformation("Seeded {Count} regions", regions.Length);
    }

    private IContent? FindNodeByAlias(string contentTypeAlias)
    {
        var rootContent = _contentService.GetRootContent();
        if (rootContent == null) return null;

        foreach (var root in rootContent)
        {
            if (root.ContentType.Alias == contentTypeAlias) return root;

            var children = _contentService.GetPagedChildren(root.Id, 0, 100, out _, null, null);
            var match = children.FirstOrDefault(c => c.ContentType.Alias == contentTypeAlias);
            if (match != null) return match;
        }

        return null;
    }

    private async Task PublishAsync(IContent content)
    {
        var cultures = new[] { new CulturePublishScheduleModel { Culture = "*" } };
        await _contentPublishingService.PublishAsync(content.Key, cultures, SuperUserKey);
    }
}
