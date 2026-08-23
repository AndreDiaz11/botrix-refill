using System;
using System.IO;

namespace BotrixRefill.Services;

public static class ErrorLogger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "botrix-refill",
        "error-log.txt"
    );

    private const int MaxLines = 500;

    public static void Log(string source, Exception ex)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(dir);

            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {source}: {ex.Message}";
            File.AppendAllLines(LogPath, new[] { line });

            Trim();
        }
        catch
        {
            // si ni siquiera se puede escribir el log, no hay nada más que hacer
        }
    }

    private static void Trim()
    {
        var lines = File.ReadAllLines(LogPath);
        if (lines.Length <= MaxLines) return;
        var kept = lines[^MaxLines..];
        File.WriteAllLines(LogPath, kept);
    }
}
