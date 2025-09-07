using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarScrapingService(HttpClient httpClient, ILogger<CalendarScrapingService> logger) : ICalendarScrapingService
    {
        private static readonly AppSettings Settings = Helper.ReadSettings<AppSettings>() ?? new AppSettings();
        private readonly System.Globalization.CultureInfo Culture = new(Settings.CalendarLocale);

        public async Task<string> ScrapeCalendarAsync(string calendarUrl)
        {
            try
            {
                logger.LogDebug("Starting to scrape calendar from: {CalendarUrl}", calendarUrl);

                HttpResponseMessage response = await httpClient.GetAsync(calendarUrl);
                response.EnsureSuccessStatusCode();

                string? calendarData = await response.Content.ReadAsStringAsync();

                logger.LogInformation("Successfully scraped calendar data. Size: {DataSize} characters", calendarData.Length);

                return calendarData;
            }
            catch(HttpRequestException ex)
            {
                logger.LogError(ex, $"HTTP error occurred while scraping calendar from {calendarUrl}", calendarUrl);
                throw;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, $"Unexpected error occurred while scraping calendar from {calendarUrl}", calendarUrl);
                throw;
            }
        }

        public List<CalendarEvent>? GetFutureCalendarEvents(string calendarData)
        {
            try
            {
                logger.LogDebug("Processing calendar data...");

                Calendar? calendar = Calendar.Load(calendarData);

                if(calendar == null)
                {
                    logger.LogWarning("No calendar data found to process.");
                    return default;
                }

                List<CalendarEvent> futureEvents = [];

                DateTime now = DateTime.UtcNow;

                foreach(CalendarEvent calendarEvent in calendar.Events)
                {
                    if(calendarEvent.Start != null && calendarEvent.Start.AsUtc > now)
                    {
                        futureEvents.Add(calendarEvent);

                        string formattedTime = calendarEvent.Start.AsUtc.ToString("dd.MM", Culture);

                        logger.LogDebug("Future event: {EventSummary} at {EventStart}", calendarEvent.Summary, formattedTime);
                    }
                }

                logger.LogInformation("Found {FutureEventCount} future events out of {TotalEventCount} total events",
                    futureEvents.Count, calendar.Events.Count);

                return futureEvents;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing calendar data");
                throw;
            }
        }
    }
}
