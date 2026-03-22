using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PredelNews.Core.Services;

namespace PredelNews.Web.Controllers.Api;

[Route("api/contact")]
[ApiController]
public class ContactApiController : ControllerBase
{
    private readonly IContactFormService _contactFormService;

    public ContactApiController(IContactFormService contactFormService)
    {
        _contactFormService = contactFormService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("ContactRateLimit")]
    public async Task<IActionResult> Submit()
    {
        // Honeypot — silently discard bot submissions
        var honeypot = Request.Form["phone_extra"].ToString();
        if (!string.IsNullOrEmpty(honeypot))
            return Ok(new { status = "success", message = "Съобщението ви беше изпратено успешно." });

        var name = Request.Form["name"].ToString();
        var email = Request.Form["email"].ToString();
        var subject = Request.Form["subject"].ToString();
        var message = Request.Form["message"].ToString();
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var (success, resultMessage) = await _contactFormService.SubmitAsync(name, email, subject, message, ip);

        if (!success)
            return BadRequest(new { status = "error", message = resultMessage });

        return Ok(new { status = "success", message = resultMessage });
    }
}
