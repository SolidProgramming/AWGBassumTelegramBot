namespace AWGBassumTelegramBot.Interfaces
{
    public interface ICalendarScrapingService
    {
        Task<string> ScrapeCalendarAsync(string calendarUrl);
        Task ProcessCalendarDataAsync(string calendarData);
    }
}
