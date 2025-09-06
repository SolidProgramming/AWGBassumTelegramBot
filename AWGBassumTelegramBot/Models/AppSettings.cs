using Microsoft.Extensions.Options;

namespace AWGBassumTelegramBot.Models
{
    public class AppSettings
    {
        public const string SectionName = "AppSettings";

        public string CalendarUrl { get; set; } = string.Empty;
        public string CalendarLocale { get; set; } = "de-DE";
        public int TimeoutSeconds { get; set; } = 30;
        public string TelegramBotToken { get; set; } = string.Empty;
        public long TelegramChatId { get; set; }
    }
}
