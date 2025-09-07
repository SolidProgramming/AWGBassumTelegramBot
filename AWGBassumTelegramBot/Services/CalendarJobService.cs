using Hangfire;
using Hangfire.Storage;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Logging;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarJobService(ICalendarScrapingService calendarScrapingService, ITelegramNotificationService telegramNotificationService, ILogger<CalendarJobService> logger) : ICalendarJobService
    {
        private static readonly AppSettings Settings = Helper.ReadSettings<AppSettings>() ?? new AppSettings();
        private readonly System.Globalization.CultureInfo Culture = new(Settings.CalendarLocale);

        public async Task ExecuteCalendarScrapeJobAsync()
        {
            try
            {
                logger.LogDebug("Executing calendar scrape job for URL: {CalendarUrl}", Settings.CalendarUrl);

                string calendarData = await calendarScrapingService.ScrapeCalendarAsync(Settings.CalendarUrl);
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
                    logger.LogDebug("There is at least one calendar event on the next day: {NextDay}", nextDay);

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

                logger.LogDebug("Calendar scrape job completed successfully");
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Calendar scrape job failed for URL: {CalendarUrl}", Settings.CalendarUrl);
                throw;
            }
        }

        public async Task ScheduleRecurringCalendarScrape(string cronExpression)
        {
            RecurringJob.AddOrUpdate(
                "calendar-scrape-job",
                () => ExecuteCalendarScrapeJobAsync(),
                cronExpression);

            logger.LogInformation("Scheduled recurring calendar scrape job with cron: {CronExpression}", cronExpression);

            string message = NextRunInfoMessage();

            await telegramNotificationService.SendMessageAsync(message);
        }

        private string BuildEventMessage(List<CalendarEvent> events)
        {
            System.Text.StringBuilder messageBuilder = new();

            messageBuilder.AppendLine($"⏰ <b>ACHTUNG:</b>{Environment.NewLine}");

            foreach(CalendarEvent calendarEvent in events)
            {
                string eventTime = calendarEvent.Start?.AsUtc.ToString("dd.MM", Culture) ?? "Unknown time";
                string eventSummary = calendarEvent.Summary ?? "No title";

                messageBuilder.AppendLine($"am <b>{eventTime}</b> wird die <b>{eventSummary}</b> abgeholt❗");
            }

            return messageBuilder.ToString();
        }

        private string BuildNextEventMessage(CalendarEvent nextEvent, DateTime nextDay)
        {
            System.Text.StringBuilder messageBuilder = new();

            string eventTime = nextEvent.Start?.AsUtc.ToString("dd.MM", Culture) ?? "Unknown time";
            string eventSummary = nextEvent.Summary ?? "No title";

            messageBuilder.AppendLine($"✅ Es sind keine Abholtermine für den <b>{nextDay.ToString("dd.MM", Culture)}</b> eingetragen.{Environment.NewLine}");
            messageBuilder.AppendLine($"➡ der nächste Abholtermin ist der <b>{eventTime}</b>, da wird die <b>{eventSummary}</b> abgeholt❗");

            return messageBuilder.ToString();
        }

        private string NextRunInfoMessage()
        {
            try
            {
                using IStorageConnection connection = JobStorage.Current.GetConnection();

                List<RecurringJobDto> recurringJobs = connection.GetRecurringJobs();
                RecurringJobDto? calendarJob = recurringJobs.FirstOrDefault(x => x.Id == "calendar-scrape-job");

                if (calendarJob == null)
                {
                    return "❌ No calendar scrape job is currently scheduled.";
                }

                DateTime? nextExecution = calendarJob.NextExecution;

                if (nextExecution.HasValue)
                {
                    string nextRunTime = nextExecution.Value.ToString("dd.MM hh:mm", Culture);
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
