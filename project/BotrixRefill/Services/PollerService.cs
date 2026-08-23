using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BotrixRefill.Models;

namespace BotrixRefill.Services;

public class PollerService
{
    private const int MinDelayMs = 10000;
    private const int MaxDelayMs = 14000;
    private const int MaxBackoffMs = 60000;

    private readonly Random _random = new();
    private CancellationTokenSource? _cts;
    private Dictionary<string, int> _previousStock = new();
    private int _consecutiveErrors;
    private AppConfig _config = new();

    public event Action<List<ShopItem>, DateTime>? ShopUpdated;
    public event Action<ShopItem>? ItemRefilled;

    public bool IsRunning => _cts != null;

    private int NextDelay()
    {
        var jitter = MinDelayMs + _random.NextDouble() * (MaxDelayMs - MinDelayMs);
        if (_consecutiveErrors == 0) return (int)jitter;
        return (int)Math.Min(jitter * Math.Pow(2, _consecutiveErrors), MaxBackoffMs);
    }

    public void Start(AppConfig config)
    {
        Stop();
        _config = new AppConfig
        {
            Streamer = BotrixApiService.ExtractStreamer(config.Streamer),
            SessionKid = config.SessionKid,
            TelegramEnabled = config.TelegramEnabled,
            TelegramToken = config.TelegramToken,
            TelegramChatId = config.TelegramChatId,
        };
        _cts = new CancellationTokenSource();
        _ = PollLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        _previousStock = new Dictionary<string, int>();
        _consecutiveErrors = 0;
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var items = await BotrixApiService.FetchShopItemsAsync(_config.Streamer);
                _consecutiveErrors = 0;

                var newStock = new Dictionary<string, int>();
                var refilled = new List<ShopItem>();
                var hasPrevData = _previousStock.Count > 0;

                foreach (var item in items)
                {
                    newStock[item.Code] = item.Stock;
                    var hadNoStock = _previousStock.TryGetValue(item.Code, out var prev) && prev == 0;
                    var nowHasStock = item.Stock != 0;
                    if (hasPrevData && hadNoStock && nowHasStock) refilled.Add(item);
                }

                _previousStock = newStock;
                ShopUpdated?.Invoke(items, DateTime.Now);

                foreach (var item in refilled)
                {
                    ItemRefilled?.Invoke(item);
                    if (_config.TelegramEnabled && !string.IsNullOrWhiteSpace(_config.TelegramToken) && !string.IsNullOrWhiteSpace(_config.TelegramChatId))
                    {
                        var msg = $"🟢 <b>{item.Name}</b> ya está disponible!\n" +
                                  $"💰 <b>{item.Price:N0}</b> puntos\n" +
                                  $"📺 Tienda de <b>{_config.Streamer}</b>\n" +
                                  $"🔗 https://botrix.live/k/{_config.Streamer}/shop";
                        _ = TelegramService.SendMessageAsync(_config.TelegramToken, _config.TelegramChatId, msg)
                            .ContinueWith(t => { }, TaskContinuationOptions.OnlyOnFaulted);
                    }
                }
            }
            catch
            {
                _consecutiveErrors++;
            }

            try
            {
                await Task.Delay(NextDelay(), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
