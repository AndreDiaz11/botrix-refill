using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BotrixRefill.Models;
using BotrixRefill.Services;

namespace BotrixRefill.ViewModels;

public partial class ShopViewModel : ViewModelBase, IDisposable
{
    private static readonly (string Label, Func<string, bool> Match)[] GroupDefs =
    {
        ("Yape / Plin", k => k.Contains("yape") || k.Contains("plin")),
        ("Suscripciones", k => k.Contains("sub")),
        ("Recargas", k => k.Contains("bet") || k.Contains("recarga")),
        ("Otros", _ => true),
    };

    private readonly AppConfig _config;
    private readonly PollerService _poller;
    private readonly System.Timers.Timer _pointsTimer;
    private System.Timers.Timer? _cooldownTimer;
    private int _toastId;

    public event Action? StopRequested;
    public event Action<ShopItem>? RefillDetected;

    public string StreamerDisplay { get; }

    [ObservableProperty]
    private ObservableCollection<RewardGroup> _groups = new();

    [ObservableProperty]
    private BotrixUser? _user;

    [ObservableProperty]
    private DateTime? _lastUpdate;

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isPolling;

    [ObservableProperty]
    private int _ptsCooldown;

    [ObservableProperty]
    private int _availableCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private ObservableCollection<ToastItem> _toasts = new();

    [ObservableProperty]
    private int _redeemedTodayCount;

    public string OpenShopLabel => $"🔴 {StreamerDisplay}";
    public bool PtsCooldownActive => PtsCooldown > 0;

    partial void OnPtsCooldownChanged(int value) => OnPropertyChanged(nameof(PtsCooldownActive));
    partial void OnIsPollingChanged(bool value) => OnPropertyChanged(nameof(StatusText));

    public string StatusText => IsPolling
        ? $"Monitoreando · tienda cada 10-14s, puntos cada 60s · última revisión {(LastUpdate.HasValue ? LastUpdate.Value.ToString("HH:mm:ss") : "—")}"
        : $"Detenido · última revisión {(LastUpdate.HasValue ? LastUpdate.Value.ToString("HH:mm:ss") : "—")}";

    public ShopViewModel(AppConfig config, PollerService poller)
    {
        _config = config;
        _poller = poller;
        StreamerDisplay = BotrixApiService.ExtractStreamer(config.Streamer);

        _poller.ShopUpdated += OnShopUpdated;
        _poller.ItemRefilled += OnItemRefilled;

        _pointsTimer = new System.Timers.Timer(60000) { AutoReset = true };
        _pointsTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(() => _ = RefreshUserAsync());
        _pointsTimer.Start();

        RedeemedTodayCount = RedemptionTracker.GetTodayCount();

        _ = LoadAsync();
        _poller.Start(config);
        IsPolling = true;
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var shopTask = BotrixApiService.FetchShopItemsAsync(BotrixApiService.ExtractStreamer(_config.Streamer));
            var userTask = BotrixApiService.FetchUserAsync(_config.Streamer, _config.SessionKid);
            await Task.WhenAll(shopTask, userTask);
            ApplyItems(await shopTask, DateTime.Now);
            User = await userTask;
        }
        catch (Exception e)
        {
            ErrorMessage = e.Message;
            ErrorLogger.Log("shop-load", e);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshUserAsync()
    {
        try
        {
            var user = await BotrixApiService.FetchUserAsync(_config.Streamer, _config.SessionKid);
            if (user != null) User = user;
        }
        catch (Exception e)
        {
            // no se muestra en UI — se reintenta solo en el próximo ciclo de 60s
            ErrorLogger.Log("refresh-points", e);
        }
    }

    [RelayCommand]
    private void RefreshPoints()
    {
        if (PtsCooldown > 0) return;
        _ = RefreshUserAsync();
        PtsCooldown = 10;
        _cooldownTimer?.Stop();
        _cooldownTimer = new System.Timers.Timer(1000) { AutoReset = true };
        _cooldownTimer.Elapsed += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            PtsCooldown = Math.Max(0, PtsCooldown - 1);
            if (PtsCooldown == 0) _cooldownTimer?.Stop();
        });
        _cooldownTimer.Start();
    }

    [RelayCommand]
    private void OpenShop()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"https://botrix.live/k/{StreamerDisplay}/shop",
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignorar si no se pudo abrir el navegador
        }
    }

    [RelayCommand]
    private void Stop()
    {
        _poller.Stop();
        IsPolling = false;
        StopRequested?.Invoke();
    }

    private void OnShopUpdated(List<ShopItem> items, DateTime timestamp)
    {
        ApplyItems(items, timestamp);
    }

    private void ApplyItems(List<ShopItem> items, DateTime timestamp)
    {
        var buckets = GroupDefs.Select(g => new RewardGroup(g.Label)).ToList();
        foreach (var item in items)
        {
            var key = $"{item.Code} {item.Name}".ToLowerInvariant();
            var idx = Array.FindIndex(GroupDefs, g => g.Match(key));
            var card = new RewardCardViewModel(item);
            card.Redeemed += () => RedeemedTodayCount = RedemptionTracker.IncrementToday();
            buckets[idx].Items.Add(card);
        }

        Groups = new ObservableCollection<RewardGroup>(buckets.Where(b => b.Items.Count > 0));
        LastUpdate = timestamp;
        AvailableCount = items.Count(i => i.Stock != 0);
        TotalCount = items.Count;
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnItemRefilled(ShopItem item)
    {
        RefillDetected?.Invoke(item);
        var id = ++_toastId;
        var toast = new ToastItem
        {
            Id = id,
            ItemName = item.Name,
            PriceText = $"{item.Price.ToString("N0", CultureInfo.GetCultureInfo("es-PE"))} pts",
        };
        Toasts.Add(toast);
        _ = RemoveToastAfterDelay(id);
    }

    private async Task RemoveToastAfterDelay(int id)
    {
        await Task.Delay(5000);
        var toast = Toasts.FirstOrDefault(t => t.Id == id);
        if (toast != null) Toasts.Remove(toast);
    }

    public void Dispose()
    {
        _poller.ShopUpdated -= OnShopUpdated;
        _poller.ItemRefilled -= OnItemRefilled;
        _pointsTimer.Stop();
        _pointsTimer.Dispose();
        _cooldownTimer?.Stop();
        _cooldownTimer?.Dispose();
    }
}
