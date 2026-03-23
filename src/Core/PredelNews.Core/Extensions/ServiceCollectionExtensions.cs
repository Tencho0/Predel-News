using Microsoft.Extensions.DependencyInjection;
using PredelNews.Core.Services;

namespace PredelNews.Core.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPredelNewsCore(this IServiceCollection services)
    {
        services.AddSingleton<ISlugGenerator, SlugGenerator>();
        services.AddSingleton<IAdSlotService, AdSlotService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IContactFormService, ContactFormService>();
        services.AddScoped<IEmailSignupService, EmailSignupService>();
        services.AddScoped<IPollService, PollService>();
        return services;
    }
}
