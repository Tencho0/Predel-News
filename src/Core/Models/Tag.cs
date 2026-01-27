namespace PredelNews.Core.Models;

public class Tag
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public int ArticleCount { get; set; }

    public string GetUrl()
    {
        return $"/tag/{Slug}";
    }
}
