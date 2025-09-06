using Hangfire;
using Hangfire.Storage;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarJobService(ICalendarScrapingService calendarScrapingService, ITelegramNotificationService telegramNotificationService, IOptions<AppSettings> settings, ILogger<CalendarJobService> logger) : ICalendarJobService
    {
        private readonly System.Globalization.CultureInfo Culture = new(settings.Value.CalendarLocale);

        public async Task ExecuteCalendarScrapeJobAsync()
        {
            try
            {
                logger.LogInformation("Executing calendar scrape job for URL: {CalendarUrl}", settings.Value.CalendarUrl);

                string calendarData = await calendarScrapingService.ScrapeCalendarAsync(settings.Value.CalendarUrl);
                List<CalendarEvent>? futureEvents = calendarScrapingService.GetFutureCalendarEvents(calendarData);

                if(futureEvents == null || futureEvents.Count == 0)
                {
                    logger.LogInformation("No upcoming events found in the calendar.");
                    return;
                }

                DateTime nextDay = DateTime.UtcNow.Date.AddDays(1);

                List<CalendarEvent>? eventsOnNextDay = [.. futureEvents.Where(e =>
                        e.Start != null && e.Start.AsUtc.Date == nextDay)];

                if(eventsOnNextDay.Count > 0)
                {
                    logger.LogInformation("There is at least one calendar event on the next day: {NextDay}", nextDay);

                    string? message = BuildEventMessage(eventsOnNextDay);

                    if(!string.IsNullOrEmpty(message))
                    {
                        await telegramNotificationService.SendMessageAsync(message);
                        logger.LogInformation("Sent notification for {EventCount} events on {NextDay}", eventsOnNextDay.Count, nextDay);
                    }
                    else
                    {
                        logger.LogWarning("No message was built for the events on {NextDay}", nextDay);
                    }
                }
                else
                {
                    CalendarEvent nearestEvent = futureEvents.OrderBy(e => e.Start?.AsUtc).First();
                    string? message = BuildNextEventMessage(nearestEvent, nextDay);

                    await telegramNotificationService.SendMessageAsync(message);
                }

                logger.LogInformation("Calendar scrape job completed successfully");
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Calendar scrape job failed for URL: {CalendarUrl}", settings.Value.CalendarUrl);
                throw;
            }
        }

        public async Task ScheduleRecurringCalendarScrape(string cronExpression)
        {
            RecurringJob.AddOrUpdate(
                "calendar-scrape-job",
                () => ExecuteCalendarScrapeJobAsync(),
                cronExpression);

            logger.LogDebug("Scheduled recurring calendar scrape job with cron: {CronExpression}", cronExpression);

            string message = NextRunInfoMessage();

            await telegramNotificationService.SendMessageAsync(message);
        }

        private string BuildEventMessage(List<CalendarEvent> events)
        {
            System.Text.StringBuilder messageBuilder = new();

            foreach(CalendarEvent calendarEvent in events)
            {
                string eventTime = calendarEvent.Start?.AsUtc.ToString("d", Culture) ?? "Unknown time";
                string eventSummary = calendarEvent.Summary ?? "No title";

                messageBuilder.AppendLine($"⏰ <b>ACHTUNG:</b>{Environment.NewLine}");
                messageBuilder.AppendLine($"am <b>{eventTime}</b> wird die <b>{eventSummary}</b> abgeholt❗");
            }

            return messageBuilder.ToString();
        }

        private string BuildNextEventMessage(CalendarEvent nextEvent, DateTime nextDay)
        {
            System.Text.StringBuilder messageBuilder = new();

            string eventTime = nextEvent.Start?.AsUtc.ToString("d", Culture) ?? "Unknown time";
            string eventSummary = nextEvent.Summary ?? "No title";

            messageBuilder.AppendLine($"✅ Es sind keine Abholtermine für den <b>{nextDay.ToString("d", Culture)}</b> eingetragen.{Environment.NewLine}");
            messageBuilder.AppendLine($"➡ der nächste Abholtermin ist der <b>{eventTime}</b>, da wird die <b>{eventSummary}</b> abgeholt❗");

            return messageBuilder.ToString();
        }

        private string NextRunInfoMessage()
        {
            try
            {
                using IStorageConnection connection = JobStorage.Current.GetConnection();

                // Get information about the recurring job
                List<RecurringJobDto> recurringJobs = connection.GetRecurringJobs();
                RecurringJobDto? calendarJob = recurringJobs.FirstOrDefault(x => x.Id == "calendar-scrape-job");

                if (calendarJob == null)
                {
                    return "❌ No calendar scrape job is currently scheduled.";
                }

                // Get the next execution time
                DateTime? nextExecution = calendarJob.NextExecution;

                if (nextExecution.HasValue)
                {
                    string nextRunTime = nextExecution.Value.ToString("dd.MM.yyyy HH:mm:ss", Culture);
                    return $"⏰ Next calendar scrape job is scheduled for: <b>{nextRunTime}</b>";
                }
                else
                {
                    return "⚠️ Calendar scrape job is scheduled but next execution time is unknown.";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve next job execution time");
                return "❌ Error retrieving job schedule information.";
            }
        }

        public string GetNextJobScheduleInfo()
        {
            return NextRunInfoMessage();
        }
    }
}
