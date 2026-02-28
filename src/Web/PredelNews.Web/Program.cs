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
