using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AWGBassumTelegramBot.Services
{
    public class TelegramNotificationService(HttpClient httpClient, ILogger<TelegramNotificationService> logger, IOptions<AppSettings> settings) : ITelegramNotificationService
    {
        private readonly AppSettings _settings = settings.Value;

        public async Task SendMessageAsync(string message)
        {
            try
            {
                if(string.IsNullOrEmpty(_settings.TelegramBotToken))
                {
                    logger.LogWarning("Telegram bot token is not configured. Cannot send message.");
                    return;
                }

                if(_settings.TelegramChatId == 0)
                {
                    logger.LogWarning("Telegram chat ID is not configured. Cannot send message.");
                    return;
                }

                string telegramApiUrl = $"https://api.telegram.org/bot{_settings.TelegramBotToken}/sendMessage";

                var payload = new
                {
                    chat_id = _settings.TelegramChatId,
                    text = message,
                    parse_mode = "HTML"
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                StringContent content = new(jsonPayload, Encoding.UTF8, "application/json");

                logger.LogInformation("Sending Telegram message to chat ID: {ChatId}", _settings.TelegramChatId);

                HttpResponseMessage response = await httpClient.PostAsync(telegramApiUrl, content);

                if(response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Telegram message sent successfully");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    logger.LogError("Failed to send Telegram message. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, responseContent);
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error occurred while sending Telegram message");
                throw;
            }
        }
    }
}
