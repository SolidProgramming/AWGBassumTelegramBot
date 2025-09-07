namespace AWGBassumTelegramBot.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task<bool> SendMessageAsync(string message, bool isTestMessage = false);
        Task<bool> TestConnectionAsync();
    }
}
