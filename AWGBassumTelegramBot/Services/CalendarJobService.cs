using Hangfire;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Logging;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarJobService(ICalendarScrapingService calendarScrapingService, ILogger<CalendarJobService> logger) : ICalendarJobService
    {
        public async Task ExecuteCalendarScrapeJobAsync(string calendarUrl)
        {
            try
            {
                logger.LogInformation("Executing calendar scrape job for URL: {CalendarUrl}", calendarUrl);

                string calendarData = await calendarScrapingService.ScrapeCalendarAsync(calendarUrl);
                List<CalendarEvent>? futureEvents = calendarScrapingService.GetFutureCalendarEvents(calendarData);



                logger.LogInformation("Calendar scrape job completed successfully");
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Calendar scrape job failed for URL: {CalendarUrl}", calendarUrl);
                throw;
            }
        }

        public void ScheduleRecurringCalendarScrape(string calendarUrl, string cronExpression)
        {
            RecurringJob.AddOrUpdate(
                "calendar-scrape-job",
                () => ExecuteCalendarScrapeJobAsync(calendarUrl),
                cronExpression);

            logger.LogInformation("Scheduled recurring calendar scrape job with cron: {CronExpression}", cronExpression);
        }
    }
}
