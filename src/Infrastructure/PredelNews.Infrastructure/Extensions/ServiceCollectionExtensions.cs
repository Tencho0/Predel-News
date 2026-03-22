using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Interfaces;
using PredelNews.Infrastructure.Repositories;

namespace PredelNews.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        return services;
    }
}
