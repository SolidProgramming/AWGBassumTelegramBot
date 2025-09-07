using System.Text.Json.Serialization;

namespace AWGBassumTelegramBot.Models
{
    public class Settings
    {

        [JsonPropertyName(name: "AppSettings")]
        public AppSettings AppSettings { get; set; }
    }
}
