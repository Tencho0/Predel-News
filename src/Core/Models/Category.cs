namespace PredelNews.Core.Models;

public class Category
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public MediaImage? Image { get; set; }
    public int SortOrder { get; set; }
    public bool IsMainNavigation { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int ArticleCount { get; set; }

    public string GetUrl()
    {
        return $"/{Slug}";
    }
}
