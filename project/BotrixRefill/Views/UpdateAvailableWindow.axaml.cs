using Avalonia.Controls;
using Avalonia.Interactivity;
using BotrixRefill.Services;

namespace BotrixRefill.Views;

public partial class UpdateAvailableWindow : Window
{
    public UpdateAvailableWindow()
    {
        InitializeComponent();
    }

    public UpdateAvailableWindow(string version) : this()
    {
        VersionText.Text = $"Versión {version} lista para instalar. La app se reinicia sola al terminar.";
    }

    private void LaterClick(object? sender, RoutedEventArgs e) => Close();

    private async void UpdateClick(object? sender, RoutedEventArgs e)
    {
        await UpdateService.DownloadAndApplyAsync();
    }
}
