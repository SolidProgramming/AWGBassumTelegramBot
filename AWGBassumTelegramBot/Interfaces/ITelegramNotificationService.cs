namespace AWGBassumTelegramBot.Interfaces
{
    public interface ITelegramNotificationService
    {
        Task SendMessageAsync(string message);
    }
}
