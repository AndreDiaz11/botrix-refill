using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BotrixRefill.Models;

namespace BotrixRefill.ViewModels;

public partial class RewardCardViewModel : ViewModelBase
{
    public ShopItem Item { get; }

    public event Action? Redeemed;

    public string Name => Item.Name;
    public string Code => Item.Code;
    public string PriceText => Item.Price.ToString("N0", CultureInfo.GetCultureInfo("es-PE"));
    public string BadgeLabel { get; }
    public string BadgeBg { get; }
    public string BadgeFg { get; }
    public string CardAccent { get; }
    public bool IsAvailable { get; }

    [ObservableProperty]
    private bool _copied;

    public string ActionLabel => Copied ? "✓ Copiado" : "🎁 Canjear";

    partial void OnCopiedChanged(bool value) => OnPropertyChanged(nameof(ActionLabel));

    public RewardCardViewModel(ShopItem item)
    {
        Item = item;

        if (item.Stock == -1)
        {
            BadgeLabel = "∞ ilimitado";
            BadgeBg = "#EFF6FF"; BadgeFg = "#2563EB"; CardAccent = "#2563EB";
            IsAvailable = true;
        }
        else if (item.Stock > 0)
        {
            BadgeLabel = item.Stock < 999 ? $"×{item.Stock} stock" : "Disponible";
            BadgeBg = "#ECFDF5"; BadgeFg = "#12B76A"; CardAccent = "#12B76A";
            IsAvailable = true;
        }
        else if (item.Disponibilidad == "Solo para suscriptores")
        {
            BadgeLabel = "👑 Solo subs";
            BadgeBg = "#FFFBEB"; BadgeFg = "#F59E0B"; CardAccent = "#F59E0B";
            IsAvailable = false;
        }
        else
        {
            BadgeLabel = "Sin stock";
            BadgeBg = "#FFF1F0"; BadgeFg = "#F04438"; CardAccent = "#E5E7EB";
            IsAvailable = false;
        }
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
        {
            var clipboard = window.Clipboard;
            if (clipboard != null) await clipboard.SetTextAsync($"!{Code}");
        }
        Redeemed?.Invoke();
        Copied = true;
        await Task.Delay(1200);
        Copied = false;
    }
}
