using PredelNews.Core.Extensions;
using PredelNews.Infrastructure.Extensions;
using PredelNews.BackofficeExtensions.Extensions;

namespace PredelNews.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.CreateUmbracoBuilder()
            .AddBackOffice()
            .AddWebsite()
            .AddDeliveryApi()
            .AddComposers()
            .Build();

        builder.Services.AddPredelNewsCore();
        builder.Services.AddPredelNewsInfrastructure(builder.Configuration);
        builder.Services.AddPredelNewsBackofficeExtensions();

        builder.Services.AddResponseCaching();
        builder.Services.AddOutputCache(options =>
        {
            options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
            options.AddPolicy("StaticAssets", builder => builder.Expire(TimeSpan.FromHours(24)));
        });

        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        WebApplication app = builder.Build();

        await app.BootUmbracoAsync();

        app.UseHttpsRedirection();

        app.UseSecurityHeaders();

        app.UseStaticFiles();

        app.UseOutputCache();
        app.UseResponseCaching();

        app.UseUmbraco()
            .WithMiddleware(u =>
            {
                u.UseBackOffice();
                u.UseWebsite();
            })
            .WithEndpoints(u =>
            {
                u.UseBackOfficeEndpoints();
                u.UseWebsiteEndpoints();
            });

        await app.RunAsync();
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "SAMEORIGIN";
            headers["X-XSS-Protection"] = "1; mode=block";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

            // CSP scaffold - customize based on requirements
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: blob:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'self'; " +
                "base-uri 'self'; " +
                "form-action 'self';";

            await next();
        });
    }
}
