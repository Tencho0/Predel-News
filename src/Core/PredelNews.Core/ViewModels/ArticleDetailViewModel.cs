namespace PredelNews.Core.ViewModels;

public class ArticleDetailViewModel : BasePageViewModel
{
    public string Headline { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? CoverImageAlt { get; set; }
    public string? CoverImageSrcSet { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryUrl { get; set; }
    public string? RegionName { get; set; }
    public string? RegionUrl { get; set; }
    public List<TagViewModel> Tags { get; set; } = [];
    public string? AuthorName { get; set; }
    public string? AuthorUrl { get; set; }
    public string? AuthorPhotoUrl { get; set; }
    public string? AuthorBio { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool IsSponsored { get; set; }
    public string? SponsorName { get; set; }
    public string ShareUrl { get; set; } = string.Empty;
    public int CommentCount { get; set; }
    public List<ArticleSummaryViewModel> RelatedArticles { get; set; } = [];
    public List<BreadcrumbItem> Breadcrumbs { get; set; } = [];
}

public class TagViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
