using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Services;

namespace PredelNews.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsCore(this IServiceCollection services)
    {
        services.AddSingleton<ISlugGenerator, SlugGenerator>();
        return services;
    }
}
