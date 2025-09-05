namespace AWGBassumTelegramBot.Models
{
    public class CalendarEvent
    {
        public string Summary { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
