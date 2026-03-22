using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Constants;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace PredelNews.BackofficeExtensions.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("umbraco/management/api/v{version:apiVersion}/predelnews/editorial")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class EditorialDashboardApiController : ManagementApiControllerBase
{
    private readonly IContentService _contentService;
    private readonly IContentTypeService _contentTypeService;
    private readonly Umbraco.Cms.Infrastructure.Scoping.IScopeProvider _scopeProvider;
    private readonly ILogger<EditorialDashboardApiController> _logger;

    public EditorialDashboardApiController(
        IContentService contentService,
        IContentTypeService contentTypeService,
        Umbraco.Cms.Infrastructure.Scoping.IScopeProvider scopeProvider,
        ILogger<EditorialDashboardApiController> logger)
    {
        _contentService = contentService;
        _contentTypeService = contentTypeService;
        _scopeProvider = scopeProvider;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(EditorialDashboardResponse), StatusCodes.Status200OK)]
    public IActionResult GetDashboard()
    {
        var articleType = _contentTypeService.Get(DocumentTypes.Article);
        if (articleType == null)
            return Ok(new EditorialDashboardResponse());

        // Page through articles in batches to avoid loading all into memory
        const int batchSize = 500;
        var inReviewArticles = new List<InReviewArticleDto>();
        int publishedTodayCount = 0;
        int publishedThisWeekCount = 0;
        var today = DateTime.UtcNow.Date;
        // Monday-based week start (Bulgarian convention)
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);

        int pageIndex = 0;
        long totalRecords;
        do
        {
            var batch = _contentService.GetPagedOfType(
                articleType.Id, pageIndex, batchSize, out totalRecords, null!);

            foreach (var article in batch)
            {
                // In Review
                if (string.Equals(
                    article.GetValue<string>(PropertyAliases.ArticleStatus),
                    "In Review",
                    StringComparison.OrdinalIgnoreCase))
                {
                    inReviewArticles.Add(new InReviewArticleDto
                    {
                        Id = article.Id,
                        Key = article.Key,
                        Headline = article.GetValue<string>(PropertyAliases.Headline) ?? article.Name ?? "",
                        AuthorName = GetAuthorName(article),
                        ModifiedAt = article.UpdateDate,
                    });
                }

                // Published counts
                if (article.Published && article.PublishDate.HasValue)
                {
                    var pubDate = article.PublishDate.Value.Date;
                    if (pubDate >= today) publishedTodayCount++;
                    if (pubDate >= weekStart) publishedThisWeekCount++;
                }
            }

            pageIndex++;
        } while (pageIndex * batchSize < totalRecords);

        // Sort in-review articles by last modified
        inReviewArticles = inReviewArticles.OrderBy(a => a.ModifiedAt).ToList();

        // Database counts with error handling for tables that may not exist yet
        int heldCommentsCount = 0;
        int emailSignupsCount = 0;

        using (var scope = _scopeProvider.CreateScope())
        {
            try
            {
                heldCommentsCount = scope.Database.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM [pn_comments] WHERE [is_held] = 1 AND [is_deleted] = 0");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not query pn_comments — table may not exist yet");
            }

            try
            {
                emailSignupsCount = scope.Database.ExecuteScalar<int>(
                    "SELECT COUNT(*) FROM [pn_email_subscribers]");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not query pn_email_subscribers — table may not exist yet");
            }

            scope.Complete();
        }

        return Ok(new EditorialDashboardResponse
        {
            InReviewCount = inReviewArticles.Count,
            InReviewArticles = inReviewArticles,
            PublishedTodayCount = publishedTodayCount,
            PublishedThisWeekCount = publishedThisWeekCount,
            HeldCommentsCount = heldCommentsCount,
            EmailSignupsCount = emailSignupsCount,
        });
    }

    private string GetAuthorName(IContent article)
    {
        var authorValue = article.GetValue<string>(PropertyAliases.Author);
        if (string.IsNullOrEmpty(authorValue))
            return "";

        // MNTP may store as JSON array of UDIs, a single UDI, or comma-separated UDIs
        var udiString = authorValue.Trim();
        if (udiString.StartsWith('['))
        {
            // JSON array format — extract the first UDI string
            udiString = udiString.Trim('[', ']', '"', ' ');
            if (udiString.Contains(','))
                udiString = udiString.Split(',')[0].Trim('"', ' ');
        }

        if (Umbraco.Cms.Core.UdiParser.TryParse(udiString, out var parsedUdi)
            && parsedUdi is Umbraco.Cms.Core.GuidUdi guidUdi)
        {
            var author = _contentService.GetById(guidUdi.Guid);
            return author?.GetValue<string>(PropertyAliases.FullName) ?? author?.Name ?? "";
        }

        return "";
    }
}

public class EditorialDashboardResponse
{
    public int InReviewCount { get; set; }
    public List<InReviewArticleDto> InReviewArticles { get; set; } = [];
    public int PublishedTodayCount { get; set; }
    public int PublishedThisWeekCount { get; set; }
    public int HeldCommentsCount { get; set; }
    public int EmailSignupsCount { get; set; }
}

public class InReviewArticleDto
{
    public int Id { get; set; }
    public Guid Key { get; set; }
    public string Headline { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public DateTime ModifiedAt { get; set; }
}
