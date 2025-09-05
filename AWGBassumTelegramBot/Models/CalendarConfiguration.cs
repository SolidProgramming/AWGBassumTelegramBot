namespace AWGBassumTelegramBot.Models
{
    public class CalendarConfiguration
    {
        public string CalendarUrl { get; set; } = string.Empty;
        public string CronExpression { get; set; } = "0 * * * *"; // Every hour by default
        public int TimeoutSeconds { get; set; } = 30;
    }
}
