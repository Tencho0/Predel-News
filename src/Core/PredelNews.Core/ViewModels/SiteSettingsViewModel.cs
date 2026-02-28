namespace PredelNews.Core.ViewModels;

public class SiteSettingsViewModel
{
    public string SiteName { get; set; } = string.Empty;
    public string? SiteLogoLightUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactRecipientEmail { get; set; }
    public string? AdsensePublisherId { get; set; }
    public string? AdsenseScriptTag { get; set; }
    public string? Ga4MeasurementId { get; set; }
    public string? FacebookUrl { get; set; }
    public string? DefaultSeoDescription { get; set; }
    public string? DefaultOgImageUrl { get; set; }
    public string? FooterCopyrightText { get; set; }
}
