namespace PredelNews.Core.Models;

public class SearchResult
{
    public required string Query { get; set; }
    public PagedResult<ArticleSummary> Articles { get; set; } = PagedResult<ArticleSummary>.Empty();
    public IReadOnlyList<Category> RelatedCategories { get; set; } = Array.Empty<Category>();
    public IReadOnlyList<Tag> RelatedTags { get; set; } = Array.Empty<Tag>();
    public TimeSpan SearchDuration { get; set; }
}
