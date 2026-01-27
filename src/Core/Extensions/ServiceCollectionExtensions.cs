using Microsoft.Extensions.DependencyInjection;

namespace PredelNews.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsCore(this IServiceCollection services)
    {
        return services;
    }
}
