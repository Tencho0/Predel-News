namespace PredelNews.Core.Models;

public class Author
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Bio { get; set; }
    public string? Email { get; set; }
    public MediaImage? Avatar { get; set; }
    public string? TwitterHandle { get; set; }
    public string? FacebookUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public int ArticleCount { get; set; }

    public string GetUrl()
    {
        return $"/author/{Slug}";
    }
}
