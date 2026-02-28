namespace PredelNews.Core.ViewModels;

public class ArchivePageViewModel : BasePageViewModel
{
    public string ArchiveTitle { get; set; } = string.Empty;
    public string? ArchiveDescription { get; set; }
    public int TotalArticleCount { get; set; }
    public List<ArticleSummaryViewModel> Articles { get; set; } = [];
    public PaginationViewModel Pagination { get; set; } = new();
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
    public string? AuthorName { get; set; }
    public string? AuthorPhotoUrl { get; set; }
    public string? AuthorBio { get; set; }
}
