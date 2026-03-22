namespace PredelNews.Core.Interfaces;

public interface ICacheInvalidationService
{
    Task EvictByTagAsync(string tag, CancellationToken cancellationToken = default);
}
