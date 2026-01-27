using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface ISiteSettingsService
{
    string SiteName { get; }
    string DefaultLanguage { get; }
    int ArticlesPerPage { get; }
    MediaImage? Logo { get; }
    MediaImage? Favicon { get; }
    string? GoogleAnalyticsId { get; }
    string? FacebookPageUrl { get; }
    string? TwitterHandle { get; }
    string? ContactEmail { get; }
    string? FooterText { get; }
}
