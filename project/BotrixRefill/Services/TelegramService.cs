using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotrixRefill.Services;

public static class TelegramService
{
    private static readonly HttpClient Http = new();

    public static async Task SendMessageAsync(string token, string chatId, string text)
    {
        var url = $"https://api.telegram.org/bot{token}/sendMessage";
        var payload = JsonSerializer.Serialize(new { chat_id = chatId, text, parse_mode = "HTML" });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        var res = await Http.PostAsync(url, content);
        var body = await res.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var ok = doc.RootElement.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();
        if (!ok)
        {
            var desc = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() : "Error de Telegram";
            throw new Exception(desc ?? "Error de Telegram");
        }
    }
}
