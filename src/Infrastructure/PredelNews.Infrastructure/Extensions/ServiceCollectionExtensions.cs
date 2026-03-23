using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Interfaces;
using PredelNews.Core.Repositories;
using PredelNews.Infrastructure.Repositories;
using PredelNews.Infrastructure.Services;

namespace PredelNews.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IContactFormRepository, ContactFormRepository>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IEmailSignupRepository, EmailSignupRepository>();
        services.AddScoped<IPollRepository, PollRepository>();
        services.AddScoped<IAdSlotRepository, AdSlotRepository>();
        return services;
    }
}
