namespace PredelNews.Core.ViewModels;

public class StaticPageViewModel : BasePageViewModel
{
    public string Body { get; set; } = string.Empty;
    public string? MediaKitPdfUrl { get; set; }
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
}
