using Ical.Net.CalendarComponents;

namespace AWGBassumTelegramBot.Interfaces
{
    public interface ICalendarScrapingService
    {
        Task<string> ScrapeCalendarAsync(string calendarUrl);
        List<CalendarEvent>? GetFutureCalendarEvents(string calendarData);
    }
}
