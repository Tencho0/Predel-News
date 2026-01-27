using PredelNews.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace PredelNews.Core.ViewModels;

public class BasePageViewModel
{
    public IPublishedContent? CurrentPage { get; set; }
    public string PageTitle { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImage { get; set; }
    public string? OgType { get; set; } = "website";
    public string? TwitterCard { get; set; } = "summary_large_image";
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImage { get; set; }
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; set; } = Array.Empty<BreadcrumbItem>();
    public IReadOnlyList<Category> NavigationCategories { get; set; } = Array.Empty<Category>();
    public IReadOnlyList<ArticleSummary> SidebarMostRead { get; set; } = Array.Empty<ArticleSummary>();
}

public class BreadcrumbItem
{
    public required string Title { get; set; }
    public string? Url { get; set; }
    public bool IsCurrentPage { get; set; }
}

public class ErrorViewModel : BasePageViewModel
{
    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
