using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Models;
using PredelNews.Core.Repositories;

namespace PredelNews.Core.Services;

public partial class CommentService : ICommentService
{
    private readonly ICommentRepository _repository;
    private readonly ISiteSettingsService _siteSettingsService;
    private readonly ILogger<CommentService> _logger;

    private const int MaxDisplayNameLength = 200;
    private const int MaxCommentTextLength = 2000;
    private const int MaxUrlCount = 2;

    public CommentService(
        ICommentRepository repository,
        ISiteSettingsService siteSettingsService,
        ILogger<CommentService> logger)
    {
        _repository = repository;
        _siteSettingsService = siteSettingsService;
        _logger = logger;
    }

    public Task<IReadOnlyList<CommentDto>> GetVisibleCommentsAsync(int articleId)
        => _repository.GetVisibleCommentsAsync(articleId);

    public Task<int> GetCommentCountAsync(int articleId)
        => _repository.GetCommentCountAsync(articleId);

    public Task<IReadOnlyDictionary<int, int>> GetCommentCountsAsync(IEnumerable<int> articleIds)
        => _repository.GetCommentCountsAsync(articleIds);

    public async Task<CommentSubmissionResult> SubmitCommentAsync(CommentSubmissionRequest request)
    {
        // Step 1: Honeypot check
        if (!string.IsNullOrEmpty(request.HoneypotField))
        {
            _logger.LogInformation("Honeypot triggered from IP {Ip}", request.IpAddress);
            return new CommentSubmissionResult(CommentSubmissionStatus.HoneypotTripped, null, null);
        }

        // Step 2: Input validation
        var validationErrors = ValidateInput(request);
        if (validationErrors.Count > 0)
        {
            return new CommentSubmissionResult(
                CommentSubmissionStatus.Invalid,
                "Моля, коригирайте грешките във формуляра.",
                null,
                validationErrors);
        }

        var sanitizedName = StripHtmlTags(request.DisplayName.Trim());
        var commentText = request.CommentText.Trim();

        // Step 3: Link count check
        var urlCount = UrlPattern().Matches(commentText).Count;
        if (urlCount >= MaxUrlCount)
        {
            _logger.LogInformation("Comment held (link_count={Count}) from IP {Ip}", urlCount, request.IpAddress);
            var heldComment = await _repository.InsertCommentAsync(
                request.ArticleId, sanitizedName, commentText, request.IpAddress,
                isHeld: true, heldReason: "link_count");

            return new CommentSubmissionResult(
                CommentSubmissionStatus.Held,
                "Коментарът ви ще бъде прегледан преди публикуване.",
                null);
        }

        // Step 4: Banned word check
        var bannedWord = FindBannedWord(commentText);
        if (bannedWord != null)
        {
            _logger.LogInformation("Comment held (banned_word:{Word}) from IP {Ip}", bannedWord, request.IpAddress);
            var heldComment = await _repository.InsertCommentAsync(
                request.ArticleId, sanitizedName, commentText, request.IpAddress,
                isHeld: true, heldReason: $"banned_word:{bannedWord}");

            return new CommentSubmissionResult(
                CommentSubmissionStatus.Held,
                "Коментарът ви ще бъде прегледан преди публикуване.",
                null);
        }

        // Step 5: Store comment (visible)
        var comment = await _repository.InsertCommentAsync(
            request.ArticleId, sanitizedName, commentText, request.IpAddress,
            isHeld: false, heldReason: null);

        return new CommentSubmissionResult(CommentSubmissionStatus.Accepted, null, comment);
    }

    public async Task DeleteCommentAsync(int commentId, int? deletedByUserId, string? deletedByUsername)
    {
        var existing = await _repository.GetByIdAsync(commentId);
        if (existing == null) return;

        await _repository.SoftDeleteAsync(commentId);
        await _repository.InsertAuditLogAsync(
            commentId,
            "deleted",
            deletedByUserId,
            deletedByUsername,
            existing.CommentText);
    }

    public Task ApproveCommentAsync(int commentId)
        => _repository.ApproveAsync(commentId);

    public Task<IReadOnlyList<HeldCommentDto>> GetHeldCommentsAsync(int page = 1, int pageSize = 20)
        => _repository.GetHeldCommentsAsync(page, pageSize);

    public Task<int> GetHeldCommentsCountAsync()
        => _repository.GetHeldCommentsCountAsync();

    private static Dictionary<string, string> ValidateInput(CommentSubmissionRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            errors["displayName"] = "Полето \u201EИме\u201C е задължително.";
        else if (request.DisplayName.Trim().Length > MaxDisplayNameLength)
            errors["displayName"] = $"Името не може да надвишава {MaxDisplayNameLength} символа.";

        if (string.IsNullOrWhiteSpace(request.CommentText))
            errors["commentText"] = "Полето \u201EКоментар\u201C е задължително.";
        else if (request.CommentText.Trim().Length > MaxCommentTextLength)
            errors["commentText"] = $"Коментарът не може да надвишава {MaxCommentTextLength} символа.";

        return errors;
    }

    private string? FindBannedWord(string text)
    {
        var settings = _siteSettingsService.GetSiteSettings();
        if (string.IsNullOrWhiteSpace(settings.BannedWordsList))
            return null;

        var bannedWords = settings.BannedWordsList
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var lowerText = text.ToLowerInvariant();
        return bannedWords.FirstOrDefault(word =>
            lowerText.Contains(word.ToLowerInvariant()));
    }

    private static string StripHtmlTags(string input)
    {
        return HtmlTagPattern().Replace(input, string.Empty);
    }

    [GeneratedRegex(@"https?://", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();
}
