global using AWGBassumTelegramBot.Interfaces;
global using AWGBassumTelegramBot.Models;
global using AWGBassumTelegramBot.Services;
global using AWGBassumTelegramBot.Misc;
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

ITelegramNotificationService telegramNotificationService = app.Services.GetRequiredService<ITelegramNotificationService>();
bool botTokenValid = await telegramNotificationService.TestConnectionAsync();

if(!botTokenValid)
{
    Console.ReadKey();
    return;
}

IHostApplicationLifetime lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStarted.Register(async() =>
{
    ICalendarJobService jobService = app.Services.GetRequiredService<ICalendarJobService>();
    IOptions<AppSettings> settings = app.Services.GetRequiredService<IOptions<AppSettings>>();

    HttpClient httpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();

    bool urlIsValidAndReachable = await Helper.IsValidAndReachableUrlAsync(settings.Value.CalendarUrl, httpClient);

    if (urlIsValidAndReachable)
    {
        jobService.ScheduleRecurringCalendarScrape(Cron.Daily());
        BackgroundJob.Enqueue(() => jobService.ExecuteCalendarScrapeJobAsync());
    }
});

await app.RunAsync();