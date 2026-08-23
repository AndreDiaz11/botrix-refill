using Avalonia.Controls;
using Avalonia.Interactivity;

namespace BotrixRefill.Views;

public partial class NewsWindow : Window
{
    public NewsWindow()
    {
        InitializeComponent();
    }

    public NewsWindow(string version, string notes) : this()
    {
        TitleText.Text = $"🎉 Botrix Refill {version}";
        NotesText.Text = string.IsNullOrWhiteSpace(notes)
            ? "Se actualizó a la última versión."
            : notes;
    }

    private void CloseClick(object? sender, RoutedEventArgs e) => Close();
}
