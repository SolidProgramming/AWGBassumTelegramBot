global using AWGBassumTelegramBot.Interfaces;
global using AWGBassumTelegramBot.Models;
global using AWGBassumTelegramBot.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Configure settings
builder.Services.Configure<AppSettings>(
    builder.Configuration.GetSection(AppSettings.SectionName));

// Register HTTP client
builder.Services.AddHttpClient<CalendarScrapingService>((serviceProvider, client) =>
{
    AppSettings settings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// Register services
builder.Services.AddScoped<ICalendarScrapingService, CalendarScrapingService>();
builder.Services.AddScoped<ICalendarJobService, CalendarJobService>();
builder.Services.AddScoped<ITelegramNotificationService, TelegramNotificationService>();

// Configure Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage());

builder.Services.AddHangfireServer();

IHost app = builder.Build();

IHostApplicationLifetime lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(() =>
{
    ICalendarJobService jobService = app.Services.GetRequiredService<ICalendarJobService>();
    IOptions<AppSettings> settings = app.Services.GetRequiredService<IOptions<AppSettings>>();

    if (IsValidUrl(settings.Value.CalendarUrl))
    {
        jobService.ScheduleRecurringCalendarScrape(Cron.Daily());
        BackgroundJob.Enqueue(() => jobService.ExecuteCalendarScrapeJobAsync());
    }
});

await app.RunAsync();

static bool IsValidUrl(string url)
{
    if (string.IsNullOrWhiteSpace(url))
        return false;

    return Uri.TryCreate(url, UriKind.Absolute, out Uri? result)
           && !string.IsNullOrEmpty(result.Host)
           && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
}