using Ical.Net;
using Ical.Net.CalendarComponents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarScrapingService(HttpClient httpClient, ILogger<CalendarScrapingService> logger, IOptions<AppSettings> calendarConfig) : ICalendarScrapingService
    {
        public async Task<string> ScrapeCalendarAsync(string calendarUrl)
        {
            try
            {
                logger.LogInformation("Starting to scrape calendar from: {CalendarUrl}", calendarUrl);

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
                logger.LogInformation("Processing calendar data...");

                Calendar? calendar = Calendar.Load(calendarData);

                if(calendar == null)
                {
                    logger.LogWarning("No calendar data found to process.");
                    return default;
                }

                List<CalendarEvent> futureEvents = [];

                DateTime now = DateTime.UtcNow;
                System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo(calendarConfig.Value.CalendarLocale);

                foreach(CalendarEvent calendarEvent in calendar.Events)
                {
                    // calendarEvent.Start is of type CalDateTime, which has a Value property of type DateTime
                    if(calendarEvent.Start != null && calendarEvent.Start.AsUtc > now)
                    {
                        futureEvents.Add(calendarEvent);

                        string formattedTime = calendarEvent.Start.Value.ToString(culture);

                        logger.LogInformation("Future event: {EventSummary} at {EventStart}", calendarEvent.Summary, formattedTime);
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
