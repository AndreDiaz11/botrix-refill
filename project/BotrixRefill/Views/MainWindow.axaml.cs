using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using BotrixRefill.Services;
using BotrixRefill.ViewModels;

namespace BotrixRefill.Views;

public partial class MainWindow : Window
{
    private TrayService? _tray;
    private bool _isQuitting;
    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closing += OnClosing;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        if (Vm != null) Vm.RefillNotification += (title, body) => _tray?.ShowNotification(title, body);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.ico");
        _tray = new TrayService(iconPath);
        _tray.OpenRequested += () => Dispatcher.UIThread.Post(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
        _tray.ExitRequested += () => Dispatcher.UIThread.Post(() =>
        {
            _isQuitting = true;
            Close();
        });

        _ = CheckNewsAsync();
        _ = CheckUpdatesAsync();
    }

    private async Task CheckNewsAsync()
    {
        var config = ConfigStore.Load();
        var news = await NewsService.CheckAsync(config);
        if (!news.ShouldShow) return;

        var win = new NewsWindow(news.Version, news.Notes);
        await win.ShowDialog(this);
        NewsService.MarkSeen(config, news.Version);
    }

    private async Task CheckUpdatesAsync()
    {
        var result = await UpdateService.CheckAsync();
        if (!result.Available || result.Version is null) return;

        var win = new UpdateAvailableWindow(result.Version);
        await win.ShowDialog(this);
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isQuitting)
        {
            _tray?.Dispose();
            return;
        }
        e.Cancel = true;
        Hide();
    }

    private void Titlebar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void Minimize_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Hide();

    private void ChangeStreamer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.ChangeStreamer();

    private void Resume_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Vm?.Resume();
}
