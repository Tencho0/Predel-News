using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace PredelNews.Web.Setup;

public class ContentTypeSetup : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<ContentTypeSetup> _logger;

    private static readonly Guid SuperUserKey = Constants.Security.SuperUserKey;

    private IDataType _textstring = null!;
    private IDataType _textarea = null!;
    private IDataType _richtext = null!;
    private IDataType _articleRichtext = null!;
    private IDataType _articleTagsPicker = null!;
    private IDataType _mediaPicker = null!;
    private IDataType _contentPicker = null!;
    private IDataType _trueFalse = null!;
    private IDataType _dateTime = null!;
    private IDataType _emailAddress = null!;

    public ContentTypeSetup(
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<ContentTypeSetup> logger)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("PredelNews: Setting up content types...");

        await LoadDataTypesAsync();

        // 1. Compositions
        var seoComposition = await EnsureCompositionAsync("seoComposition", "SEO Composition", new[]
        {
            (PropertyAliases.SeoTitle, "SEO Title", _textstring, "SEO"),
            (PropertyAliases.SeoDescription, "SEO Description", _textarea, "SEO"),
            (PropertyAliases.OgImage, "OG Image", _mediaPicker, "SEO"),
        });

        await EnsureCompositionAsync("pageMetaComposition", "Page Meta Composition", new[]
        {
            (PropertyAliases.NavHide, "Hide from Navigation", _trueFalse, "Navigation"),
        });

        // 2. Container nodes
        var newsRoot = await EnsureSimpleTypeAsync(DocumentTypes.NewsRoot, "News Root", "icon-folder");
        var categoryRoot = await EnsureSimpleTypeAsync(DocumentTypes.CategoryRoot, "Category Root", "icon-folder");
        var regionRoot = await EnsureSimpleTypeAsync(DocumentTypes.RegionRoot, "Region Root", "icon-folder");
        var tagRoot = await EnsureSimpleTypeAsync(DocumentTypes.TagRoot, "Tag Root", "icon-folder");
        var authorRoot = await EnsureSimpleTypeAsync(DocumentTypes.AuthorRoot, "Author Root", "icon-folder");

        // 3. Taxonomy types
        var category = await EnsureTaxonomyTypeAsync(DocumentTypes.Category, "Category", "icon-categories",
            (PropertyAliases.CategoryName, "Category Name", _textstring),
            (PropertyAliases.SeoDescription, "SEO Description", _textarea));

        var region = await EnsureTaxonomyTypeAsync(DocumentTypes.Region, "Region", "icon-map-location",
            (PropertyAliases.RegionName, "Region Name", _textstring),
            (PropertyAliases.SeoDescription, "SEO Description", _textarea));

        var newsTag = await EnsureTaxonomyTypeAsync(DocumentTypes.NewsTag, "News Tag", "icon-tags",
            (PropertyAliases.TagName, "Tag Name", _textstring),
            (PropertyAliases.SeoDescription, "SEO Description", _textarea));

        var author = await EnsureAuthorTypeAsync();

        // 4. Content types
        var article = await EnsureArticleTypeAsync(seoComposition);
        var homePage = await EnsureHomePageTypeAsync(seoComposition);
        var siteSettings = await EnsureSiteSettingsTypeAsync();
        var staticPage = await EnsureStaticPageTypeAsync(seoComposition);
        var contactPage = await EnsureContactPageTypeAsync(seoComposition);
        var allNewsPage = await EnsureAllNewsPageTypeAsync(seoComposition);

        // 5. Allowed children
        await SetAllowedChildrenAsync(newsRoot, new[] { article });
        await SetAllowedChildrenAsync(categoryRoot, new[] { category });
        await SetAllowedChildrenAsync(regionRoot, new[] { region });
        await SetAllowedChildrenAsync(tagRoot, new[] { newsTag });
        await SetAllowedChildrenAsync(authorRoot, new[] { author });
        await SetAllowedChildrenAsync(homePage, new[] { newsRoot, categoryRoot, regionRoot, tagRoot, authorRoot, allNewsPage, staticPage, contactPage, siteSettings });

        _logger.LogInformation("PredelNews: Content type setup complete");
    }

    private async Task LoadDataTypesAsync()
    {
        var allDataTypes = (await _dataTypeService.GetAllAsync()).ToList();

        IDataType Find(string editorAlias) =>
            allDataTypes.FirstOrDefault(d => d.EditorAlias == editorAlias)
            ?? throw new InvalidOperationException($"Data type with editor alias '{editorAlias}' not found");

        _textstring = Find("Umbraco.Plain.String");
        _textarea = Find("Umbraco.TextArea");
        _richtext = Find("Umbraco.RichText");
        _mediaPicker = Find("Umbraco.MediaPicker3");
        _contentPicker = Find("Umbraco.ContentPicker");
        _trueFalse = Find("Umbraco.TrueFalse");
        _dateTime = Find("Umbraco.DateTime");
        _emailAddress = Find("Umbraco.EmailAddress");

        // Use custom Article Body RTE if available, fall back to default RTE
        _articleRichtext = allDataTypes
            .FirstOrDefault(d => d.Key == TinyMceConfigSetup.ArticleRteKey) ?? _richtext;

        // Use custom Article Tags Picker if available, fall back to default Content Picker
        _articleTagsPicker = allDataTypes
            .FirstOrDefault(d => d.Key == TinyMceConfigSetup.ArticleTagsPickerKey) ?? _contentPicker;
    }

    private async Task<IContentType> EnsureCompositionAsync(string alias, string name,
        (string alias, string name, IDataType dataType, string group)[] properties)
    {
        var existing = _contentTypeService.Get(alias);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = "icon-settings",
        };

        foreach (var (propAlias, propName, dataType, group) in properties)
        {
            ct.AddPropertyType(new PropertyType(_shortStringHelper, dataType, propAlias) { Name = propName }, group, group);
        }

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created composition: {Alias}", alias);
        return ct;
    }

    private async Task<IContentType> EnsureSimpleTypeAsync(string alias, string name, string icon)
    {
        var existing = _contentTypeService.Get(alias);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = icon,
        };

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created type: {Alias}", alias);
        return ct;
    }

    private async Task<IContentType> EnsureTaxonomyTypeAsync(string alias, string name, string icon,
        params (string alias, string name, IDataType dataType)[] properties)
    {
        var existing = _contentTypeService.Get(alias);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = icon,
        };

        foreach (var (propAlias, propName, dataType) in properties)
        {
            ct.AddPropertyType(new PropertyType(_shortStringHelper, dataType, propAlias) { Name = propName }, "Content", "Content");
        }

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created taxonomy type: {Alias}", alias);
        return ct;
    }

    private async Task<IContentType> EnsureAuthorTypeAsync()
    {
        var existing = _contentTypeService.Get(DocumentTypes.Author);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.Author,
            Name = "Author",
            Icon = "icon-user",
        };

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.FullName) { Name = "Full Name", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textarea, PropertyAliases.Bio) { Name = "Bio" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _mediaPicker, PropertyAliases.Photo) { Name = "Photo" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _emailAddress, PropertyAliases.Email) { Name = "Email" }, "Internal", "Internal");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created author type");
        return ct;
    }

    private async Task<IContentType> EnsureArticleTypeAsync(IContentType seoComposition)
    {
        var existing = _contentTypeService.Get(DocumentTypes.Article);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.Article,
            Name = "Article",
            Icon = "icon-document",
        };

        ct.AddContentType(seoComposition);

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.Headline) { Name = "Headline", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.Subtitle) { Name = "Subtitle" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.Slug) { Name = "Slug", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _articleRichtext, PropertyAliases.Body) { Name = "Body", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _mediaPicker, PropertyAliases.CoverImage) { Name = "Cover Image", Mandatory = true }, "Content", "Content");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.Category) { Name = "Category", Mandatory = true }, "Taxonomy", "Taxonomy");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.Region) { Name = "Region" }, "Taxonomy", "Taxonomy");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _articleTagsPicker, PropertyAliases.Tags) { Name = "Tags" }, "Taxonomy", "Taxonomy");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.Author) { Name = "Author", Mandatory = true }, "Taxonomy", "Taxonomy");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _dateTime, PropertyAliases.PublishDate) { Name = "Publish Date", Mandatory = true }, "Settings", "Settings");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _trueFalse, PropertyAliases.IsBreakingNews) { Name = "Breaking News" }, "Settings", "Settings");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.RelatedArticlesOverride) { Name = "Related Articles Override" }, "Settings", "Settings");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _trueFalse, PropertyAliases.IsSponsored) { Name = "Sponsored Content" }, "Sponsored", "Sponsored");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.SponsorName) { Name = "Sponsor Name" }, "Sponsored", "Sponsored");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created article type");
        return ct;
    }

    private async Task<IContentType> EnsureHomePageTypeAsync(IContentType seoComposition)
    {
        var existing = _contentTypeService.Get(DocumentTypes.HomePage);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.HomePage,
            Name = "Home Page",
            Icon = "icon-home",
            AllowedAsRoot = true,
        };

        ct.AddContentType(seoComposition);

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.FeaturedArticles) { Name = "Featured Articles" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _contentPicker, PropertyAliases.NationalHeadlinesOverride) { Name = "National Headlines Override" }, "Content", "Content");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created home page type");
        return ct;
    }

    private async Task<IContentType> EnsureSiteSettingsTypeAsync()
    {
        var existing = _contentTypeService.Get(DocumentTypes.SiteSettings);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.SiteSettings,
            Name = "Site Settings",
            Icon = "icon-settings",
        };

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.SiteName) { Name = "Site Name", Mandatory = true }, "General", "General");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _mediaPicker, PropertyAliases.SiteLogoLight) { Name = "Site Logo (Light)" }, "General", "General");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _emailAddress, PropertyAliases.ContactEmail) { Name = "Contact Email" }, "General", "General");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _emailAddress, PropertyAliases.ContactRecipientEmail) { Name = "Contact Form Recipient Email" }, "General", "General");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.FooterCopyrightText) { Name = "Footer Copyright Text" }, "General", "General");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.FacebookUrl) { Name = "Facebook URL" }, "Social", "Social");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.InstagramUrl) { Name = "Instagram URL" }, "Social", "Social");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.AdsensePublisherId) { Name = "AdSense Publisher ID" }, "Analytics & Ads", "Analytics & Ads");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textarea, PropertyAliases.AdsenseScriptTag) { Name = "AdSense Script Tag" }, "Analytics & Ads", "Analytics & Ads");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.Ga4MeasurementId) { Name = "GA4 Measurement ID" }, "Analytics & Ads", "Analytics & Ads");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textarea, PropertyAliases.DefaultSeoDescription) { Name = "Default SEO Description" }, "SEO", "SEO");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _mediaPicker, PropertyAliases.DefaultOgImage) { Name = "Default OG Image" }, "SEO", "SEO");

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textarea, PropertyAliases.BannedWordsList) { Name = "Banned Words List" }, "Moderation", "Moderation");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _trueFalse, PropertyAliases.MaintenanceMode) { Name = "Maintenance Mode" }, "Moderation", "Moderation");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created site settings type");
        return ct;
    }

    private async Task<IContentType> EnsureStaticPageTypeAsync(IContentType seoComposition)
    {
        var existing = _contentTypeService.Get(DocumentTypes.StaticPage);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.StaticPage,
            Name = "Static Page",
            Icon = "icon-document",
        };

        ct.AddContentType(seoComposition);

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.PageTitle) { Name = "Page Title", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _richtext, PropertyAliases.Body) { Name = "Body" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _mediaPicker, PropertyAliases.MediaKitPdf) { Name = "Media Kit PDF" }, "Content", "Content");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created static page type");
        return ct;
    }

    private async Task<IContentType> EnsureContactPageTypeAsync(IContentType seoComposition)
    {
        var existing = _contentTypeService.Get(DocumentTypes.ContactPage);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.ContactPage,
            Name = "Contact Page",
            Icon = "icon-message",
        };

        ct.AddContentType(seoComposition);

        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.PageTitle) { Name = "Page Title", Mandatory = true }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textarea, PropertyAliases.IntroText) { Name = "Intro Text" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _textstring, PropertyAliases.PhoneNumber) { Name = "Phone Number" }, "Content", "Content");
        ct.AddPropertyType(new PropertyType(_shortStringHelper, _emailAddress, PropertyAliases.DisplayEmail) { Name = "Display Email" }, "Content", "Content");

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created contact page type");
        return ct;
    }

    private async Task<IContentType> EnsureAllNewsPageTypeAsync(IContentType seoComposition)
    {
        var existing = _contentTypeService.Get(DocumentTypes.AllNewsPage);
        if (existing != null) return existing;

        var ct = new ContentType(_shortStringHelper, -1)
        {
            Alias = DocumentTypes.AllNewsPage,
            Name = "All News Page",
            Icon = "icon-newspaper-alt",
        };

        ct.AddContentType(seoComposition);

        await _contentTypeService.SaveAsync(ct, SuperUserKey);
        _logger.LogInformation("Created all news page type");
        return ct;
    }

    private async Task SetAllowedChildrenAsync(IContentType parent, IContentType[] children)
    {
        var sorts = children.Select((c, i) => new ContentTypeSort(c.Key, i, c.Alias)).ToList();

        var currentAliases = parent.AllowedContentTypes?.Select(a => a.Alias).OrderBy(a => a).ToList() ?? [];
        var newAliases = sorts.Select(s => s.Alias).OrderBy(a => a).ToList();

        if (currentAliases.SequenceEqual(newAliases))
            return;

        parent.AllowedContentTypes = sorts;
        await _contentTypeService.SaveAsync(parent, SuperUserKey);
        _logger.LogInformation("Set allowed children for {Alias}: {Children}", parent.Alias, string.Join(", ", newAliases));
    }
}
