namespace PredelNews.Core.ViewModels;

public class BasePageViewModel
{
    public string PageTitle { get; set; } = string.Empty;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public string? CanonicalUrl { get; set; }
    public string SiteName { get; set; } = string.Empty;
    public string? AnalyticsTrackingId { get; set; }
}
