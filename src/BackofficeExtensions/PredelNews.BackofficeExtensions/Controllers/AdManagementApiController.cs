using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Models;
using PredelNews.Core.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace PredelNews.BackofficeExtensions.Controllers;

public record UpdateAdSlotRequest(
    string Mode,
    string? AdsenseCode,
    string? BannerImageUrl,
    string? BannerDestUrl,
    string? BannerAltText,
    DateTime? StartDate,
    DateTime? EndDate
);

[VersionedApiBackOfficeRoute("ads")]
[ApiExplorerSettings(GroupName = "Ads")]
[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class AdManagementApiController : ManagementApiControllerBase
{
    private readonly IAdSlotService _adSlotService;
    private readonly IUserService _userService;

    public AdManagementApiController(IAdSlotService adSlotService, IUserService userService)
    {
        _adSlotService = adSlotService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSlots()
    {
        var slots = await _adSlotService.GetAllAsync();
        return Ok(slots);
    }

    [HttpGet("{slotId}")]
    public async Task<IActionResult> GetSlot(string slotId)
    {
        var slot = await _adSlotService.GetBySlotIdAsync(slotId);
        if (slot == null) return NotFound();
        return Ok(slot);
    }

    [HttpPut("{slotId}")]
    public async Task<IActionResult> UpdateSlot(string slotId, [FromBody] UpdateAdSlotRequest request)
    {
        if (!await IsAdminAsync()) return Forbid();

        if (request.Mode is not "adsense" and not "direct")
            return BadRequest(new { status = "error", message = "Невалиден режим. Позволени стойности: adsense, direct." });

        if (request.Mode == "direct")
        {
            if (string.IsNullOrWhiteSpace(request.BannerImageUrl))
                return BadRequest(new { status = "error", message = "Изображението на банера е задължително в директен режим." });
            if (string.IsNullOrWhiteSpace(request.BannerDestUrl))
                return BadRequest(new { status = "error", message = "URL адресът на банера е задължителен в директен режим." });
        }
        if (request.StartDate.HasValue && request.EndDate.HasValue && request.StartDate >= request.EndDate)
            return BadRequest(new { status = "error", message = "Началната дата трябва да е преди крайната." });

        var existing = await _adSlotService.GetBySlotIdAsync(slotId);
        if (existing == null) return NotFound();

        var updated = new AdSlot
        {
            Id = existing.Id,
            SlotId = existing.SlotId,
            SlotName = existing.SlotName,
            Mode = request.Mode,
            AdsenseCode = request.AdsenseCode,
            BannerImageUrl = request.BannerImageUrl,
            BannerDestUrl = request.BannerDestUrl,
            BannerAltText = request.BannerAltText,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
        };

        await _adSlotService.UpdateSlotAsync(updated);
        return Ok(new { status = "updated" });
    }

    [HttpPost("{slotId}/reset")]
    public async Task<IActionResult> ResetToAdSense(string slotId)
    {
        if (!await IsAdminAsync()) return Forbid();

        var existing = await _adSlotService.GetBySlotIdAsync(slotId);
        if (existing == null) return NotFound();

        var slot = new AdSlot
        {
            Id = existing.Id,
            SlotId = existing.SlotId,
            SlotName = existing.SlotName,
            Mode = "adsense",
            AdsenseCode = existing.AdsenseCode,
        };

        await _adSlotService.UpdateSlotAsync(slot);
        return Ok(new { status = "reset" });
    }

    private async Task<bool> IsAdminAsync()
    {
        var userKeyStr = User.FindFirst(Umbraco.Cms.Core.Constants.Security.OpenIdDictSubClaimType)?.Value
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userKeyStr == null || !Guid.TryParse(userKeyStr, out var userKey))
            return false;

        var user = await _userService.GetAsync(userKey);
        var groups = user?.Groups.Select(g => g.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        return groups.Contains("admin", StringComparer.OrdinalIgnoreCase);
    }
}
