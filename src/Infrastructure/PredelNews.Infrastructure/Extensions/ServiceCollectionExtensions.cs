using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PredelNews.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
