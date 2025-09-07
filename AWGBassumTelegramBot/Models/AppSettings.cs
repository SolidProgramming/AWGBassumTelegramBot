using Hangfire;
using System.Text.Json.Serialization;

namespace AWGBassumTelegramBot.Models
{
    public class AppSettings
    {
        [JsonPropertyName("CalendarUrl")]
        public string CalendarUrl { get; set; } = string.Empty;

        [JsonPropertyName("CalendarLocale")]
        public string CalendarLocale { get; set; } = "de-DE";

        [JsonPropertyName("TimeoutSeconds")]
        public int TimeoutSeconds { get; set; } = 30;

        [JsonPropertyName("TelegramBotToken")]
        public string TelegramBotToken { get; set; } = string.Empty;

        [JsonPropertyName("TelegramChatId")]
        public long TelegramChatId { get; set; }

        [JsonPropertyName("JobSchedule")]
        public string JobSchedule { get; set; } = Cron.Daily();

        [JsonPropertyName("TelegramSilentNotifications")]
        public bool TelegramSilentNotifications { get; set; }
    }
}
