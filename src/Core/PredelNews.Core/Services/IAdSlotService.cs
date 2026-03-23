using PredelNews.Core.Models;

namespace PredelNews.Core.Services;

public interface IAdSlotService
{
    Task<IReadOnlyList<AdSlot>> GetAllAsync();
    Task<AdSlot?> GetBySlotIdAsync(string slotId);
    Task UpdateSlotAsync(AdSlot slot);
}
