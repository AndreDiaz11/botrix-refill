using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BotrixRefill.Models;

namespace BotrixRefill.Services;

public static class BotrixApiService
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public static string ExtractStreamer(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var match = Regex.Match(input, @"botrix\.live/k/([^/]+)");
        return match.Success ? match.Groups[1].Value : input.Trim();
    }

    public static async Task<List<ShopItem>> FetchShopItemsAsync(string streamer)
    {
        var url = $"https://botrix.live/api/public/shop/items?u={Uri.EscapeDataString(streamer)}&platform=kick";
        var res = await Http.GetAsync(url);
        if (!res.IsSuccessStatusCode) throw new Exception("Canal no encontrado");
        var body = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ShopItem>>(body, JsonOpts) ?? new List<ShopItem>();
    }

    public static async Task<BotrixUser?> FetchUserAsync(string streamer, string sessionKid)
    {
        var name = ExtractStreamer(streamer);
        var url = $"https://botrix.live/api/public/leaderboard/whoamiKick?user={Uri.EscapeDataString(name)}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Session-kid", sessionKid);
        var res = await Http.SendAsync(req);
        if (!res.IsSuccessStatusCode) throw new Exception("Error al obtener puntos");
        var body = await res.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<WhoamiResponse>(body, JsonOpts);
        return data?.User;
    }
}
