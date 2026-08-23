using System;

namespace BotrixRefill.Services;

public static class RedemptionTracker
{
    private static string Today => DateTime.Now.ToString("yyyy-MM-dd");

    public static int GetTodayCount()
    {
        var cfg = ConfigStore.Load();
        return cfg.RedeemedTodayDate == Today ? cfg.RedeemedTodayCount : 0;
    }

    public static int IncrementToday()
    {
        var cfg = ConfigStore.Load();
        var today = Today;
        if (cfg.RedeemedTodayDate != today)
        {
            cfg.RedeemedTodayDate = today;
            cfg.RedeemedTodayCount = 0;
        }
        cfg.RedeemedTodayCount++;
        ConfigStore.Save(cfg);
        return cfg.RedeemedTodayCount;
    }
}
