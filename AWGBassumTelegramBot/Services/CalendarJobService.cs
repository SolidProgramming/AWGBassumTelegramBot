using Hangfire;
using Microsoft.Extensions.Logging;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarJobService : ICalendarJobService
    {
        private readonly ICalendarScrapingService _calendarScrapingService;
        private readonly ILogger<CalendarJobService> _logger;

        public CalendarJobService(ICalendarScrapingService calendarScrapingService, ILogger<CalendarJobService> logger)
        {
            _calendarScrapingService = calendarScrapingService;
            _logger = logger;
        }

        public async Task ExecuteCalendarScrapeJobAsync(string calendarUrl)
        {
            try
            {
                _logger.LogInformation("Executing calendar scrape job for URL: {CalendarUrl}", calendarUrl);

                var calendarData = await _calendarScrapingService.ScrapeCalendarAsync(calendarUrl);
                await _calendarScrapingService.ProcessCalendarDataAsync(calendarData);

                _logger.LogInformation("Calendar scrape job completed successfully");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Calendar scrape job failed for URL: {CalendarUrl}", calendarUrl);
                throw;
            }
        }

        public void ScheduleRecurringCalendarScrape(string calendarUrl, string cronExpression)
        {
            RecurringJob.AddOrUpdate(
                "calendar-scrape-job",
                () => ExecuteCalendarScrapeJobAsync(calendarUrl),
                cronExpression);

            _logger.LogInformation("Scheduled recurring calendar scrape job with cron: {CronExpression}", cronExpression);
        }
    }
}
