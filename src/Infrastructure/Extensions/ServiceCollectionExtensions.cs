using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Interfaces;
using PredelNews.Infrastructure.Caching;
using PredelNews.Infrastructure.Services;

namespace PredelNews.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddScoped<IArticleService, UmbracoArticleService>();
        services.AddScoped<ICategoryService, UmbracoCategoryService>();
        services.AddScoped<ISearchService, UmbracoSearchService>();
        services.AddScoped<ITagService, UmbracoTagService>();
        services.AddScoped<IAuthorService, UmbracoAuthorService>();

        return services;
    }
}
