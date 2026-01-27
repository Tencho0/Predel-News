using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PredelNews.Core.Interfaces;

namespace PredelNews.Infrastructure.Caching;

public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _cacheKeys;
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
        _cacheKeys = new ConcurrentDictionary<string, byte>();
    }

    public Task<T?> GetAsync<T>(string key) where T : class
    {
        var value = _cache.Get<T>(key);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? _defaultExpiration,
            Priority = CacheItemPriority.Normal
        };

        cacheOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
        {
            _cacheKeys.TryRemove(evictedKey.ToString() ?? string.Empty, out _);
        });

        _cache.Set(key, value, cacheOptions);
        _cacheKeys.TryAdd(key, 0);

        _logger.LogDebug("Cache set for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        _cacheKeys.TryRemove(key, out _);
        _logger.LogDebug("Cache removed for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix)
    {
        var keysToRemove = _cacheKeys.Keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
        }

        _logger.LogDebug("Cache removed for prefix: {Prefix}, Count: {Count}", prefix, keysToRemove.Count);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory();
        await SetAsync(key, value, expiration);
        return value;
    }
}
