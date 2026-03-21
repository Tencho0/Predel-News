using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PredelNews.Core.Models;
using PredelNews.Core.Services;

namespace PredelNews.Web.Controllers.Api;

[Route("api/comments")]
[ApiController]
public class CommentsApiController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsApiController> _logger;

    private const string CookieName = "pn_comment_name";
    private const int CookieMaxAgeDays = 30;

    public CommentsApiController(ICommentService commentService, ILogger<CommentsApiController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("CommentRateLimit")]
    public async Task<IActionResult> Submit()
    {
        var request = new CommentSubmissionRequest(
            ArticleId: int.TryParse(Request.Form["articleId"], out var articleId) ? articleId : 0,
            DisplayName: Request.Form["displayName"].ToString(),
            CommentText: Request.Form["commentText"].ToString(),
            IpAddress: HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            HoneypotField: Request.Form["website"].ToString());

        if (request.ArticleId <= 0)
        {
            return BadRequest(new { status = "invalid", errors = new { articleId = "Невалидна статия." } });
        }

        var result = await _commentService.SubmitCommentAsync(request);

        return result.Status switch
        {
            CommentSubmissionStatus.Accepted => HandleAccepted(result),
            CommentSubmissionStatus.Held => Ok(new { status = "held", message = result.UserMessage }),
            CommentSubmissionStatus.HoneypotTripped => Ok(new { status = "accepted", message = "Коментарът е публикуван." }),
            CommentSubmissionStatus.Invalid => BadRequest(new { status = "invalid", errors = result.ValidationErrors }),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        if (!UserCanModerate())
            return Forbid();

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out var userId);
        var username = User.Identity?.Name ?? "unknown";

        await _commentService.DeleteCommentAsync(id, userId, username);

        return Ok(new { status = "deleted" });
    }

    private IActionResult HandleAccepted(CommentSubmissionResult result)
    {
        Response.Cookies.Append(CookieName, result.Comment!.DisplayName, new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(CookieMaxAgeDays),
            SameSite = SameSiteMode.Lax,
            HttpOnly = false,
            IsEssential = true
        });

        return Ok(new
        {
            status = "accepted",
            comment = new
            {
                id = result.Comment.Id,
                displayName = result.Comment.DisplayName,
                commentText = result.Comment.CommentText,
                createdAt = result.Comment.CreatedAt.ToString("dd.MM.yyyy, HH:mm")
            }
        });
    }

    private bool UserCanModerate()
    {
        return User.IsInRole("Writer") || User.IsInRole("Editor") || User.IsInRole("Admin")
            || User.IsInRole("admin");
    }
}
