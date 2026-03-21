using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using PredelNews.BackofficeExtensions.Extensions;
using PredelNews.Core.Extensions;
using PredelNews.Core.Services;
using PredelNews.Infrastructure.Extensions;
using PredelNews.Web.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Umbraco", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/predelnews-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.File(
            path: "logs/predelnews-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddPredelNewsCore();
    builder.Services.AddPredelNewsInfrastructure(builder.Configuration);
    builder.Services.AddPredelNewsBackofficeExtensions();
    builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
    builder.Services.AddScoped<ContentMapperService>();

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-CSRF-TOKEN";
        options.FormFieldName = "__RequestVerificationToken";
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("CommentRateLimit", context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(5),
                    SegmentsPerWindow = 5,
                    PermitLimit = 3
                }));
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                status = "rate_limited",
                message = "Моля, изчакайте няколко минути преди да публикувате нов коментар."
            });
            await context.HttpContext.Response.WriteAsync(body, cancellationToken);
        };
    });

    builder.CreateUmbracoBuilder()
        .AddBackOffice()
        .AddWebsite()
        .AddComposers()
        .Build();

    WebApplication app = builder.Build();

    await app.BootUmbracoAsync();

    app.UseSerilogRequestLogging();
    app.UseStatusCodePagesWithReExecute("/error/{0}");
    app.UseHttpsRedirection();
    app.UseRateLimiter();

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
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
