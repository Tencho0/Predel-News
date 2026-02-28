using PredelNews.Core.Constants;
using PredelNews.Core.Services;
using PredelNews.Core.ViewModels;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;

namespace PredelNews.Web.Services;

public class SiteSettingsService : ISiteSettingsService
{
    private readonly UmbracoHelper _umbracoHelper;

    public SiteSettingsService(UmbracoHelper umbracoHelper)
    {
        _umbracoHelper = umbracoHelper;
    }

    public SiteSettingsViewModel GetSiteSettings()
    {
        var rootContent = _umbracoHelper.ContentAtRoot().FirstOrDefault();

        if (rootContent == null)
            return new SiteSettingsViewModel { SiteName = "PredelNews" };

        var settingsNode = rootContent.Children?
            .FirstOrDefault(c => c.ContentType.Alias == DocumentTypes.SiteSettings);

        if (settingsNode == null)
            return new SiteSettingsViewModel { SiteName = "PredelNews" };

        return new SiteSettingsViewModel
        {
            SiteName = settingsNode.Value<string>(PropertyAliases.SiteName) ?? "PredelNews",
            SiteLogoLightUrl = settingsNode.Value<IPublishedContent>(PropertyAliases.SiteLogoLight)?.Url(),
            ContactEmail = settingsNode.Value<string>(PropertyAliases.ContactEmail),
            ContactRecipientEmail = settingsNode.Value<string>(PropertyAliases.ContactRecipientEmail),
            AdsensePublisherId = settingsNode.Value<string>(PropertyAliases.AdsensePublisherId),
            AdsenseScriptTag = settingsNode.Value<string>(PropertyAliases.AdsenseScriptTag),
            Ga4MeasurementId = settingsNode.Value<string>(PropertyAliases.Ga4MeasurementId),
            FacebookUrl = settingsNode.Value<string>(PropertyAliases.FacebookUrl),
            InstagramUrl = settingsNode.Value<string>(PropertyAliases.InstagramUrl),
            DefaultSeoDescription = settingsNode.Value<string>(PropertyAliases.DefaultSeoDescription),
            DefaultOgImageUrl = settingsNode.Value<IPublishedContent>(PropertyAliases.DefaultOgImage)?.Url(),
            FooterCopyrightText = settingsNode.Value<string>(PropertyAliases.FooterCopyrightText),
            BannedWordsList = settingsNode.Value<string>(PropertyAliases.BannedWordsList),
            MaintenanceMode = settingsNode.Value<bool>(PropertyAliases.MaintenanceMode),
        };
    }
}
