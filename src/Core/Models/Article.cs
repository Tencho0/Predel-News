using Umbraco.Cms.Core.Models.PublishedContent;

namespace PredelNews.Core.Models;

public class Article
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public string? Subtitle { get; set; }
    public required string Content { get; set; }
    public string? Excerpt { get; set; }
    public DateTime PublishDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public required Author Author { get; set; }
    public required Category Category { get; set; }
    public IReadOnlyList<Tag> Tags { get; set; } = Array.Empty<Tag>();
    public MediaImage? FeaturedImage { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBreakingNews { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? CanonicalUrl { get; set; }

    public string GetUrl()
    {
        return $"/{Category.Slug}/{Slug}";
    }
}

public class ArticleSummary
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public string? Excerpt { get; set; }
    public DateTime PublishDate { get; set; }
    public required string AuthorName { get; set; }
    public required string CategoryName { get; set; }
    public required string CategorySlug { get; set; }
    public MediaImage? FeaturedImage { get; set; }
    public int ViewCount { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBreakingNews { get; set; }

    public string GetUrl()
    {
        return $"/{CategorySlug}/{Slug}";
    }
}
