namespace PredelNews.Core.ViewModels;

public class ArticleSummaryViewModel
{
    public int Id { get; set; }
    public string Headline { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? CoverImageAlt { get; set; }
    public string? CategoryName { get; set; }
    public string? CategorySlug { get; set; }
    public string? RegionName { get; set; }
    public string? AuthorName { get; set; }
    public DateTime PublishDate { get; set; }
    public bool IsBreakingNews { get; set; }
    public bool IsSponsored { get; set; }
    public string? SponsorName { get; set; }
    public int CommentCount { get; set; }
}
