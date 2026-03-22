using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Services;

namespace PredelNews.Web.Controllers.Api;

[Route("api/poll")]
[ApiController]
public class PollApiController : ControllerBase
{
    private readonly IPollService _pollService;

    public PollApiController(IPollService pollService)
    {
        _pollService = pollService;
    }

    [HttpPost("vote")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Vote()
    {
        if (!int.TryParse(Request.Form["pollId"], out var pollId) ||
            !int.TryParse(Request.Form["optionId"], out var optionId))
            return BadRequest(new { status = "error", message = "\u041d\u0435\u0432\u0430\u043b\u0438\u0434\u043d\u0438 \u0434\u0430\u043d\u043d\u0438." });

        // Cookie dedup check
        var cookieName = $"pn_voted_{pollId}";
        if (Request.Cookies.ContainsKey(cookieName))
        {
            var results = await _pollService.GetResultsAsync(pollId);
            return Ok(new { status = "already_voted", message = "\u0412\u0435\u0447\u0435 \u0441\u0442\u0435 \u0433\u043b\u0430\u0441\u0443\u0432\u0430\u043b\u0438 \u0432 \u0442\u0430\u0437\u0438 \u0430\u043d\u043a\u0435\u0442\u0430.", results });
        }

        var (success, message, voteResults) = await _pollService.VoteAsync(pollId, optionId);

        if (!success)
            return BadRequest(new { status = "error", message });

        // Set cookie — 365 days, not HttpOnly (JS reads it), SameSite Lax
        Response.Cookies.Append(cookieName, optionId.ToString(), new CookieOptions
        {
            MaxAge = TimeSpan.FromDays(365),
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = true
        });

        return Ok(new { status = "success", message, results = voteResults });
    }
}
