namespace AWGBassumTelegramBot.Interfaces
{
    public interface ICalendarJobService
    {
        Task ExecuteCalendarScrapeJobAsync(string calendarUrl);
        void ScheduleRecurringCalendarScrape(string calendarUrl, string cronExpression);
    }
}
