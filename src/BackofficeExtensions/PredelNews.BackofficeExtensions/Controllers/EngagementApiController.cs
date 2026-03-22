using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace PredelNews.BackofficeExtensions.Controllers;

public record CreatePollRequest(string Question, List<string> Options, DateTime? OpensAt, DateTime? ClosesAt);

[VersionedApiBackOfficeRoute("engagement")]
[ApiExplorerSettings(GroupName = "Engagement")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class EngagementApiController : ManagementApiControllerBase
{
    private readonly IPollService _pollService;
    private readonly IEmailSignupRepository _emailSignupRepository;
    private readonly IUserService _userService;

    public EngagementApiController(IPollService pollService, IEmailSignupRepository emailSignupRepository, IUserService userService)
    {
        _pollService = pollService;
        _emailSignupRepository = emailSignupRepository;
        _userService = userService;
    }

    // --- Polls ---

    [HttpGet("polls")]
    public async Task<IActionResult> GetPolls()
    {
        var polls = await _pollService.GetAllPollsAsync();
        return Ok(polls);
    }

    [HttpGet("polls/{id:int}")]
    public async Task<IActionResult> GetPoll(int id)
    {
        var poll = await _pollService.GetPollWithOptionsAsync(id);
        if (poll == null) return NotFound();
        var results = await _pollService.GetResultsAsync(id);
        return Ok(new { poll, results });
    }

    [HttpPost("polls")]
    public async Task<IActionResult> CreatePoll([FromBody] CreatePollRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdClaim, out var uid) ? uid : 0;

        try
        {
            var pollId = await _pollService.CreatePollAsync(request.Question, request.Options, userId, request.OpensAt, request.ClosesAt);
            return Ok(new { id = pollId, status = "created" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { status = "error", message = ex.Message });
        }
    }

    [HttpPost("polls/{id:int}/activate")]
    public async Task<IActionResult> ActivatePoll(int id)
    {
        await _pollService.ActivatePollAsync(id);
        return Ok(new { status = "activated" });
    }

    [HttpPost("polls/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivatePoll(int id)
    {
        await _pollService.DeactivatePollAsync(id);
        return Ok(new { status = "deactivated" });
    }

    [HttpDelete("polls/{id:int}")]
    public async Task<IActionResult> DeletePoll(int id)
    {
        var (success, message) = await _pollService.DeletePollAsync(id);
        if (!success) return BadRequest(new { status = "error", message });
        return Ok(new { status = "deleted" });
    }

    // --- Email Subscribers ---

    [HttpGet("subscribers/count")]
    public async Task<IActionResult> GetSubscriberCount()
    {
        var count = await _emailSignupRepository.GetCountAsync();
        return Ok(new { count });
    }

    [HttpGet("subscribers/recent")]
    public async Task<IActionResult> GetRecentSubscribers()
    {
        var all = await _emailSignupRepository.GetAllAsync();
        var recent = all.Take(20).Select(s => new
        {
            email = MaskEmail(s.Email),
            signedUpAt = s.SignedUpAt.ToString("yyyy-MM-dd HH:mm")
        });
        return Ok(recent);
    }

    [HttpGet("subscribers/export")]
    public async Task<IActionResult> ExportSubscribers()
    {
        // Writer role excluded per US-07.06 — check Umbraco user groups via IUserService
        var userKeyStr = User.FindFirst(Umbraco.Cms.Core.Constants.Security.OpenIdDictSubClaimType)?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userKeyStr == null || !Guid.TryParse(userKeyStr, out var userKey))
            return Forbid();

        var user = await _userService.GetAsync(userKey);
        var userGroups = user?.Groups.Select(g => g.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase)
                         ?? [];

        var allowedGroups = new[] { "admin", "editor" };
        if (!userGroups.Any(g => allowedGroups.Contains(g, StringComparer.OrdinalIgnoreCase)))
            return Forbid();

        var subscribers = await _emailSignupRepository.GetAllAsync();
        var csv = new StringBuilder("email,signed_up_at,consent_flag\n");
        foreach (var s in subscribers)
            csv.AppendLine($"{CsvField(s.Email)},{s.SignedUpAt:yyyy-MM-dd HH:mm},{s.ConsentFlag}");

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "email-subscribers.csv");
    }

    private static string CsvField(string value) =>
        $"\"{value.Replace("\"", "\"\"")}\"";


    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        return parts[0][..1] + "***@" + parts[1];
    }
}
