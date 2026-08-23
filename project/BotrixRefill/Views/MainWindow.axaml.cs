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

        _ = CheckNewsAndUpdatesAsync();
    }

    // Las dos consultas de red (Novedades y Actualización) se disparan a la vez
    // — nunca una después de la otra — para que el popup de Actualización no
    // tarde el doble esperando a que termine el de Novedades primero. El orden
    // en que se MUESTRAN (Novedades antes que Actualización) es independiente
    // de en qué orden terminan de responder.
    private async Task CheckNewsAndUpdatesAsync()
    {
        var config = ConfigStore.Load();
        var newsTask = NewsService.CheckAsync(config);
        var updateTask = UpdateService.CheckAsync();
        await Task.WhenAll(newsTask, updateTask);

        var news = newsTask.Result;
        if (news.ShouldShow)
        {
            var newsWin = new NewsWindow(news.Version, news.Notes);
            await newsWin.ShowDialog(this);
            NewsService.MarkSeen(config, news.Version);
        }

        var update = updateTask.Result;
        if (update.Available && update.Version != null)
        {
            var updateWin = new UpdateAvailableWindow(update.Version);
            await updateWin.ShowDialog(this);
        }
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
