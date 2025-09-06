namespace AWGBassumTelegramBot.Interfaces
{
    public interface ICalendarJobService
    {
        Task ExecuteCalendarScrapeJobAsync();
        void ScheduleRecurringCalendarScrape(string cronExpression);
    }
}
