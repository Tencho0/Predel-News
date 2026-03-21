using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Repositories;
using PredelNews.Infrastructure.Repositories;

namespace PredelNews.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommentRepository, CommentRepository>();
        return services;
    }
}
