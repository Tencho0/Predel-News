using Microsoft.AspNetCore.Mvc;
using PredelNews.Core.Models;
using PredelNews.Core.Services;

namespace PredelNews.Web.ViewComponents;

public class AdSlotViewComponent : ViewComponent
{
    private readonly IAdSlotService _adSlotService;

    public AdSlotViewComponent(IAdSlotService adSlotService)
    {
        _adSlotService = adSlotService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string slotId)
    {
        var slot = await _adSlotService.GetBySlotIdAsync(slotId)
                   ?? new AdSlot { SlotId = slotId, Mode = "adsense" };
        return View("_AdSlot", slot);
    }
}
