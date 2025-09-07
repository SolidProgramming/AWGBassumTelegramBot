using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AWGBassumTelegramBot.Services
{
    public class TelegramNotificationService(HttpClient httpClient, ILogger<TelegramNotificationService> logger) : ITelegramNotificationService
    {
        private static readonly AppSettings Settings = Helper.ReadSettings<AppSettings>() ?? new AppSettings();

        public async Task<bool> SendMessageAsync(string message)
        {
            try
            {
                if(string.IsNullOrEmpty(Settings.TelegramBotToken))
                {
                    logger.LogWarning("Telegram bot token is not configured. Cannot send message.");
                    return false;
                }

                if(Settings.TelegramChatId == 0)
                {
                    logger.LogWarning("Telegram chat ID is not configured. Cannot send message.");
                    return false;
                }

                string telegramApiUrl = $"https://api.telegram.org/bot{Settings.TelegramBotToken}/sendMessage";

                var payload = new
                {
                    chat_id = Settings.TelegramChatId,
                    text = message,
                    parse_mode = "HTML"
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                StringContent content = new(jsonPayload, Encoding.UTF8, "application/json");

                logger.LogInformation("Sending Telegram message to chat ID: {ChatId}", Settings.TelegramChatId);

                HttpResponseMessage response = await httpClient.PostAsync(telegramApiUrl, content);

                if(response.IsSuccessStatusCode)
                {
                    logger.LogDebug("Telegram message sent successfully");
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    logger.LogError("Failed to send Telegram message. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error occurred while sending Telegram message");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                if(string.IsNullOrEmpty(Settings.TelegramBotToken))
                {
                    logger.LogWarning("Telegram bot token is not configured. Cannot test connection.");
                    return false;
                }

                string telegramApiUrl = $"https://api.telegram.org/bot{Settings.TelegramBotToken}/getMe";

                HttpResponseMessage response = await httpClient.GetAsync(telegramApiUrl);

                if(response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    logger.LogDebug("Telegram bot connection test successful: {Response}", responseContent);

                    bool canSendMessage = await SendMessageAsync("❕ Test Notification");

                    if(canSendMessage)
                    {
                        logger.LogInformation("Telegram bot is configured correctly and can send messages.");
                    }
                    else
                    {
                        logger.LogError($"Telegram bot connection test succeeded, but sending test message failed. Check your {nameof(AppSettings.TelegramChatId)}");
                    }

                    return canSendMessage;
                }
                else
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    logger.LogError("Telegram bot connection test failed. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);

                    return false;
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error occurred while testing Telegram bot connection");
                return false;
            }
        }
    }
}
