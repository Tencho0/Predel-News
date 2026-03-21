namespace PredelNews.Core.Models;

public record CommentDto(int Id, string DisplayName, string CommentText, DateTime CreatedAt);

public record HeldCommentDto(
    int Id,
    int ArticleId,
    string DisplayName,
    string CommentText,
    string? HeldReason,
    DateTime CreatedAt);

public record CommentSubmissionRequest(
    int ArticleId,
    string DisplayName,
    string CommentText,
    string IpAddress,
    string? HoneypotField);

public enum CommentSubmissionStatus
{
    Accepted,
    Held,
    RateLimited,
    HoneypotTripped,
    Invalid
}

public record CommentSubmissionResult(
    CommentSubmissionStatus Status,
    string? UserMessage,
    CommentDto? Comment,
    Dictionary<string, string>? ValidationErrors = null);
