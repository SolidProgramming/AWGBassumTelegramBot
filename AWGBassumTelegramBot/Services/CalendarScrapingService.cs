using Microsoft.Extensions.Logging;

namespace AWGBassumTelegramBot.Services
{
    public class CalendarScrapingService : ICalendarScrapingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CalendarScrapingService> _logger;

        public CalendarScrapingService(HttpClient httpClient, ILogger<CalendarScrapingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string> ScrapeCalendarAsync(string calendarUrl)
        {
            try
            {
                _logger.LogInformation("Starting to scrape calendar from: {CalendarUrl}", calendarUrl);

                var response = await _httpClient.GetAsync(calendarUrl);
                response.EnsureSuccessStatusCode();

                var calendarData = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Successfully scraped calendar data. Size: {DataSize} characters", calendarData.Length);

                return calendarData;
            }
            catch(HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error occurred while scraping calendar from {calendarUrl}", calendarUrl);
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error occurred while scraping calendar from {calendarUrl}", calendarUrl);
                throw;
            }
        }

        public async Task ProcessCalendarDataAsync(string calendarData)
        {
            try
            {
                _logger.LogInformation("Processing calendar data...");

                // Basic iCal parsing - you can enhance this based on your needs
                var events = ParseICalEvents(calendarData);

                _logger.LogInformation("Found {EventCount} events in calendar", events.Count);

                // Process each event (store in database, send notifications, etc.)
                foreach(var eventInfo in events)
                {
                    _logger.LogInformation("Event: {Summary} on {StartDate}", eventInfo.Summary, eventInfo.StartDate);
                    // Add your business logic here
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing calendar data");
                throw;
            }
        }

        private List<CalendarEvent> ParseICalEvents(string calendarData)
        {
            var events = new List<CalendarEvent>();
            var lines = calendarData.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            CalendarEvent? currentEvent = null;

            foreach(var line in lines)
            {
                var trimmedLine = line.Trim();

                if(trimmedLine == "BEGIN:VEVENT")
                {
                    currentEvent = new CalendarEvent();
                }
                else if(trimmedLine == "END:VEVENT" && currentEvent != null)
                {
                    events.Add(currentEvent);
                    currentEvent = null;
                }
                else if(currentEvent != null)
                {
                    if(trimmedLine.StartsWith("SUMMARY:"))
                    {
                        currentEvent.Summary = trimmedLine.Substring(8);
                    }
                    else if(trimmedLine.StartsWith("DTSTART:"))
                    {
                        var dateString = trimmedLine.Substring(8);
                        if(DateTime.TryParseExact(dateString, "yyyyMMddTHHmmssZ", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var startDate))
                        {
                            currentEvent.StartDate = startDate;
                        }
                    }
                    else if(trimmedLine.StartsWith("DTEND:"))
                    {
                        var dateString = trimmedLine.Substring(6);
                        if(DateTime.TryParseExact(dateString, "yyyyMMddTHHmmssZ", null, System.Globalization.DateTimeStyles.AssumeUniversal, out var endDate))
                        {
                            currentEvent.EndDate = endDate;
                        }
                    }
                    else if(trimmedLine.StartsWith("DESCRIPTION:"))
                    {
                        currentEvent.Description = trimmedLine.Substring(12);
                    }
                }
            }

            return events;
        }
    }
}
