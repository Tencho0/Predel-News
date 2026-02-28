namespace PredelNews.Core.Constants;

public static class CacheKeys
{
    private const string Prefix = "PredelNews:";

    public const string SiteSettings = Prefix + "SiteSettings";
    public const string Categories = Prefix + "Categories";
    public const string Regions = Prefix + "Regions";
    public const string AdSlots = Prefix + "AdSlots";

    public static string ArticleCommentCount(int articleId) => $"{Prefix}ArticleCommentCount:{articleId}";
    public static string ActivePoll() => $"{Prefix}ActivePoll";
}
