using System.Text.Json.Serialization;

namespace BotrixRefill.Models;

public class AppConfig
{
    [JsonPropertyName("streamer")]
    public string Streamer { get; set; } = "";

    [JsonPropertyName("sessionKid")]
    public string SessionKid { get; set; } = "";

    [JsonPropertyName("telegramEnabled")]
    public bool TelegramEnabled { get; set; }

    [JsonPropertyName("telegramToken")]
    public string TelegramToken { get; set; } = "";

    [JsonPropertyName("telegramChatId")]
    public string TelegramChatId { get; set; } = "";

    [JsonPropertyName("lastSeenVersion")]
    public string LastSeenVersion { get; set; } = "";
}
