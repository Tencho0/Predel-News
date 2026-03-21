namespace PredelNews.Core.ViewModels;

public class SearchPageViewModel
{
    public string Query { get; set; } = "";
    public List<SearchResultViewModel> Results { get; set; } = [];
    public PaginationViewModel Pagination { get; set; } = new();
    public int TotalResults { get; set; }
    public bool IsEmpty => TotalResults == 0 && !string.IsNullOrEmpty(Query);
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
    public string PageTitle { get; set; } = "Търсене";
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
}
