namespace PredelNews.Infrastructure.Caching;

public static class CacheKeys
{
    public const string AllCategories = "categories:all";
    public const string NavigationCategories = "categories:navigation";
    public const string AllTags = "tags:all";
    public const string PopularTags = "tags:popular";
    public const string AllAuthors = "authors:all";
    public const string FeaturedArticles = "articles:featured";
    public const string BreakingNews = "articles:breaking";
    public const string MostRead = "articles:mostread";
    public const string LatestArticles = "articles:latest";

    public static string Category(string slug) => $"category:{slug}";
    public static string CategoryArticles(string slug, int page) => $"category:{slug}:articles:{page}";
    public static string Tag(string slug) => $"tag:{slug}";
    public static string TagArticles(string slug, int page) => $"tag:{slug}:articles:{page}";
    public static string Author(string slug) => $"author:{slug}";
    public static string AuthorArticles(string slug, int page) => $"author:{slug}:articles:{page}";
    public static string Article(string categorySlug, string articleSlug) => $"article:{categorySlug}:{articleSlug}";
    public static string ArticleById(int id) => $"article:id:{id}";
    public static string Search(string query, int page) => $"search:{query.ToLowerInvariant()}:{page}";
    public static string RelatedArticles(int articleId) => $"articles:related:{articleId}";
}
