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
    client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
});

// Register services
builder.Services.AddScoped<ICalendarScrapingService, CalendarScrapingService>();
builder.Services.AddScoped<ICalendarJobService, CalendarJobService>();

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
    IOptions<AppSettings> appSettings = app.Services.GetRequiredService<IOptions<AppSettings>>();

    string calendarUrl = appSettings.Value.CalendarUrl;
    string cronExpression = appSettings.Value.CronExpression;

    if (!string.IsNullOrEmpty(calendarUrl))
    {
        jobService.ScheduleRecurringCalendarScrape(calendarUrl, cronExpression);
        BackgroundJob.Enqueue(() => jobService.ExecuteCalendarScrapeJobAsync(calendarUrl));
    }
});

await app.RunAsync();