using System;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using BotrixRefill.Models;
using BotrixRefill.Services;

namespace BotrixRefill.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PollerService _poller = new();

    public event Action<string, string>? RefillNotification;

    [ObservableProperty]
    private ViewModelBase _currentPage = null!;

    [ObservableProperty]
    private string _titlebarPill = "";

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isInShop;

    public MainWindowViewModel()
    {
        var saved = ConfigStore.Load();
        if (!string.IsNullOrWhiteSpace(saved.Streamer) && !string.IsNullOrWhiteSpace(saved.SessionKid))
        {
            ShowShop(saved);
        }
        else
        {
            CurrentPage = MakeSetup(saved);
        }
    }

    private SetupViewModel MakeSetup(AppConfig? saved)
    {
        var vm = new SetupViewModel(saved);
        vm.SaveCompleted += ShowShop;
        return vm;
    }

    private void ShowShop(AppConfig config)
    {
        if (CurrentPage is ShopViewModel oldShop) oldShop.Dispose();

        var shop = new ShopViewModel(config, _poller);
        shop.StopRequested += ShowPaused;
        shop.RefillDetected += item => RefillNotification?.Invoke(
            "🟢 Tienda actualizada — Botrix Refill",
            $"{item.Name} ya está disponible! ({item.Price.ToString("N0", CultureInfo.GetCultureInfo("es-PE"))} puntos)"
        );
        CurrentPage = shop;
        TitlebarPill = $"@{shop.StreamerDisplay}";
        IsInShop = true;
        IsPaused = false;
    }

    private void ShowPaused()
    {
        IsPaused = true;
        IsInShop = false;
    }

    public void ChangeStreamer()
    {
        IsPaused = false;
        CurrentPage = MakeSetup(ConfigStore.Load());
        TitlebarPill = "";
        IsInShop = false;
    }

    public void Resume()
    {
        var cfg = ConfigStore.Load();
        ShowShop(cfg);
    }

    public PollerService Poller => _poller;
}
