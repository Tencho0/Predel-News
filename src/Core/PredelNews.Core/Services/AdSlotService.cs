using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Models;

namespace PredelNews.Core.Services;

public class AdSlotService : IAdSlotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "AdSlots:All";

    public AdSlotService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
    }

    public async Task<IReadOnlyList<AdSlot>> GetAllAsync()
    {
        return await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IAdSlotRepository>();
            return await repo.GetAllAsync();
        }) ?? [];
    }

    public async Task<AdSlot?> GetBySlotIdAsync(string slotId)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(s => s.SlotId == slotId);
    }

    public async Task UpdateSlotAsync(AdSlot slot)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAdSlotRepository>();
        await repo.UpdateAsync(slot);
        _cache.Remove(CacheKey);
    }
}
