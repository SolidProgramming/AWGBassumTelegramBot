namespace AWGBassumTelegramBot.Interfaces
{
    public interface ICalendarJobService
    {
        Task ExecuteCalendarScrapeJobAsync();
        Task ScheduleRecurringCalendarScrape(string cronExpression);
        string GetNextJobScheduleInfo();
    }
}
