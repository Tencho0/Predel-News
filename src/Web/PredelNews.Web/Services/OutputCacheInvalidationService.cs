using Microsoft.AspNetCore.OutputCaching;
using PredelNews.Core.Interfaces;

namespace PredelNews.Web.Services;

public class OutputCacheInvalidationService : ICacheInvalidationService
{
    private readonly IOutputCacheStore _outputCacheStore;

    public OutputCacheInvalidationService(IOutputCacheStore outputCacheStore)
    {
        _outputCacheStore = outputCacheStore;
    }

    public async Task EvictByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await _outputCacheStore.EvictByTagAsync(tag, cancellationToken);
    }
}
