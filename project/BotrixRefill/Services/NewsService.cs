using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using BotrixRefill.Models;

namespace BotrixRefill.Services;

public class NewsResult
{
    public bool ShouldShow { get; init; }
    public string Version { get; init; } = "";
    public string Notes { get; init; } = "";
}

public static class NewsService
{
    private const string ReleaseApiBase = "https://api.github.com/repos/AndreDiaz11/botrix-refill/releases/tags/v";
    private static readonly HttpClient Http = new();

    public static string CurrentVersion =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public static async Task<NewsResult> CheckAsync(AppConfig config)
    {
        var current = CurrentVersion;
        if (config.LastSeenVersion == current)
        {
            return new NewsResult { ShouldShow = false, Version = current };
        }

        var notes = "";
        try
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("BotrixRefill");
            var res = await Http.GetAsync(ReleaseApiBase + current);
            if (res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("body", out var bodyProp))
                {
                    notes = bodyProp.GetString() ?? "";
                }
            }
        }
        catch
        {
            // sin conexión o sin release todavía — se muestra el popup igual, sin changelog
        }

        return new NewsResult { ShouldShow = true, Version = current, Notes = notes };
    }

    public static void MarkSeen(AppConfig config, string version)
    {
        config.LastSeenVersion = version;
        ConfigStore.Save(config);
    }
}
