namespace PredelNews.Core.ViewModels;

public class SearchResultViewModel
{
    public string Headline { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string ArticleUrl { get; set; } = "";
    public string? CategoryName { get; set; }
    public string? CategoryUrl { get; set; }
    public string? RegionName { get; set; }
    public string? RegionUrl { get; set; }
    public DateTime PublishDate { get; set; }
    public bool IsSponsored { get; set; }
}
