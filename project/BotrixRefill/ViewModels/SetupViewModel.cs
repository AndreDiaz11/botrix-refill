using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BotrixRefill.Models;
using BotrixRefill.Services;

namespace BotrixRefill.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    public event Action<AppConfig>? SaveCompleted;

    [ObservableProperty]
    private string _streamer = "";

    [ObservableProperty]
    private string _sessionKid = "";

    [ObservableProperty]
    private bool _telegramEnabled;

    [ObservableProperty]
    private string _telegramToken = "";

    [ObservableProperty]
    private string _telegramChatId = "";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _testLoading;

    [ObservableProperty]
    private string _testMessage = "";

    [ObservableProperty]
    private bool _testMessageIsOk;

    public string SaveButtonLabel => IsLoading ? "Cargando..." : "▶  Iniciar monitoreo";
    public string TestButtonLabel => TestLoading ? "Enviando..." : "Probar conexión";
    public bool SessionKidHasValue => !string.IsNullOrWhiteSpace(SessionKid);

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(SaveButtonLabel));
    partial void OnTestLoadingChanged(bool value) => OnPropertyChanged(nameof(TestButtonLabel));
    partial void OnSessionKidChanged(string value) => OnPropertyChanged(nameof(SessionKidHasValue));

    public SetupViewModel(AppConfig? saved)
    {
        if (saved is null) return;
        Streamer = saved.Streamer;
        SessionKid = saved.SessionKid;
        TelegramEnabled = saved.TelegramEnabled;
        TelegramToken = saved.TelegramToken;
        TelegramChatId = saved.TelegramChatId;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Streamer) || string.IsNullOrWhiteSpace(SessionKid))
        {
            ErrorMessage = "Completa el streamer y Session-kid.";
            return;
        }

        IsLoading = true;
        ErrorMessage = "";
        try
        {
            var cfg = new AppConfig
            {
                Streamer = Streamer.Trim(),
                SessionKid = SessionKid.Trim(),
                TelegramEnabled = TelegramEnabled,
                TelegramToken = TelegramToken.Trim(),
                TelegramChatId = TelegramChatId.Trim(),
            };
            ConfigStore.Save(cfg);
            SaveCompleted?.Invoke(cfg);
        }
        catch (Exception e)
        {
            ErrorMessage = "Error al guardar.";
            ErrorLogger.Log("setup-save", e);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestAsync()
    {
        if (string.IsNullOrWhiteSpace(TelegramToken) || string.IsNullOrWhiteSpace(TelegramChatId))
        {
            TestMessage = "❌ Ingresa token y chat ID.";
            TestMessageIsOk = false;
            return;
        }

        TestLoading = true;
        TestMessage = "";
        try
        {
            await TelegramService.SendMessageAsync(
                TelegramToken.Trim(),
                TelegramChatId.Trim(),
                "✅ <b>Botrix Refill</b> conectado correctamente!\nRecibirás notificaciones aquí cuando la tienda se rellene."
            );
            TestMessage = "✅ Enviado!";
            TestMessageIsOk = true;
        }
        catch (Exception e)
        {
            TestMessage = $"❌ {e.Message}";
            TestMessageIsOk = false;
        }
        finally
        {
            TestLoading = false;
        }
    }
}
