namespace AWGBassumTelegramBot.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task<bool> SendMessageAsync(string message);
        Task<bool> TestConnectionAsync();
    }
}
