using PredelNews.Core.Models;

namespace PredelNews.Core.Interfaces;

public interface IAdSlotRepository
{
    Task<IReadOnlyList<AdSlot>> GetAllAsync();
    Task UpdateAsync(AdSlot slot);
}
