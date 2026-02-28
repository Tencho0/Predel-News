using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace PredelNews.Web.Setup;

public class TemplateSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IFileService _fileService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<TemplateSetup> _logger;

    private static readonly Guid SuperUserKey = Constants.Security.SuperUserKey;

    public TemplateSetup(
        IFileService fileService,
        IContentTypeService contentTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<TemplateSetup> logger)
    {
        _fileService = fileService;
        _contentTypeService = contentTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredelNews: Setting up templates...");

        var templateMappings = new (string alias, string name, string docTypeAlias)[]
        {
            ("HomePage", "Home Page", DocumentTypes.HomePage),
            ("Article", "Article", DocumentTypes.Article),
            ("Category", "Category", DocumentTypes.Category),
            ("Region", "Region", DocumentTypes.Region),
            ("NewsTag", "News Tag", DocumentTypes.NewsTag),
            ("Author", "Author", DocumentTypes.Author),
            ("AllNewsPage", "All News Page", DocumentTypes.AllNewsPage),
            ("StaticPage", "Static Page", DocumentTypes.StaticPage),
            ("ContactPage", "Contact Page", DocumentTypes.ContactPage),
        };

        foreach (var (alias, name, docTypeAlias) in templateMappings)
        {
            await EnsureTemplateAsync(alias, name, docTypeAlias);
        }

        _logger.LogInformation("PredelNews: Template setup complete");
    }

    private async Task EnsureTemplateAsync(string alias, string name, string docTypeAlias)
    {
        var existing = _fileService.GetTemplate(alias);
        if (existing == null)
        {
            var template = new Template(_shortStringHelper, name, alias);
            _fileService.SaveTemplate(template);
            _logger.LogInformation("Created template: {Alias}", alias);
            existing = _fileService.GetTemplate(alias);
        }

        if (existing == null) return;

        var contentType = _contentTypeService.Get(docTypeAlias);
        if (contentType == null) return;

        if (contentType.DefaultTemplate?.Alias != alias)
        {
            contentType.SetDefaultTemplate(existing);
            if (!contentType.AllowedTemplates.Any(t => t.Alias == alias))
            {
                contentType.AllowedTemplates = contentType.AllowedTemplates.Append(existing);
            }
            await _contentTypeService.SaveAsync(contentType, SuperUserKey);
            _logger.LogInformation("Assigned template {Alias} to doc type {DocType}", alias, docTypeAlias);
        }
    }
}
