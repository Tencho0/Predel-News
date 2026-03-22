using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PredelNews.Core.Services;

namespace PredelNews.Web.Controllers.Api;

[Route("api/email-signup")]
[ApiController]
public class EmailSignupApiController : ControllerBase
{
    private readonly IEmailSignupService _emailSignupService;

    public EmailSignupApiController(IEmailSignupService emailSignupService)
    {
        _emailSignupService = emailSignupService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("EmailSignupRateLimit")]
    public async Task<IActionResult> Signup()
    {
        var email = Request.Form["email"].ToString();
        var consent = Request.Form["consent"].ToString().Equals("true", StringComparison.OrdinalIgnoreCase)
                   || Request.Form["consent"].ToString() == "on";

        var (success, message) = await _emailSignupService.SignupAsync(email, consent);

        if (!success)
            return BadRequest(new { status = "error", message });

        return Ok(new { status = "success", message });
    }
}
