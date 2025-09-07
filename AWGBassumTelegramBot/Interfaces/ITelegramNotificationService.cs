namespace AWGBassumTelegramBot.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task<bool> SendMessageAsync(string message, bool sentSilentTestMessage = true);
        Task<bool> TestConnectionAsync();
    }
}
