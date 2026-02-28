namespace PredelNews.Core.ViewModels;

public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; } = 20;
    public string BaseUrl { get; set; } = string.Empty;
}
