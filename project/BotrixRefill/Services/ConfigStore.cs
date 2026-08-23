using System;
using System.IO;
using System.Text.Json;
using BotrixRefill.Models;

namespace BotrixRefill.Services;

public static class ConfigStore
{
    // Carpeta propia (distinta a "botrix-refill" de la version Electron vieja)
    // a proposito: cada instalacion nueva de esta version (Avalonia) arranca
    // 100% en blanco en el Setup, sin heredar sesion/streamer de nadie mas.
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "botrix-refill-app",
        "config.json"
    );

    public static AppConfig Load()
    {
        try
        {
            var raw = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(raw) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }
}
