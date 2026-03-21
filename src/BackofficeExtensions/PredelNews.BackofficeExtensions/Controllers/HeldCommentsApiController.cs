using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Web.Common.Authorization;

namespace PredelNews.BackofficeExtensions.Controllers;

[VersionedApiBackOfficeRoute("held-comments")]
[ApiExplorerSettings(GroupName = "Held Comments")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class HeldCommentsApiController : ManagementApiControllerBase
{
    private readonly ICommentService _commentService;

    public HeldCommentsApiController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var comments = await _commentService.GetHeldCommentsAsync(page, pageSize);
        var totalCount = await _commentService.GetHeldCommentsCountAsync();

        return Ok(new
        {
            items = comments.Select(c => new
            {
                c.Id,
                c.ArticleId,
                c.DisplayName,
                c.CommentText,
                c.HeldReason,
                createdAt = c.CreatedAt.ToString("dd.MM.yyyy, HH:mm")
            }),
            total = totalCount,
            page,
            pageSize
        });
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _commentService.ApproveCommentAsync(id);
        return Ok(new { status = "approved" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // TODO: extract actual Umbraco user ID from backoffice auth context
        var username = User.Identity?.Name ?? "backoffice-user";
        await _commentService.DeleteCommentAsync(id, null, username);
        return Ok(new { status = "deleted" });
    }
}
