using Avalonia;
using System;
using System.Threading;
using Velopack;

namespace BotrixRefill;

sealed class Program
{
    private const string SingleInstanceMutexName = "BotrixRefill-SingleInstance-9F3D2C11";
    public const string ShowRequestEventName = "BotrixRefill-ShowRequest-9F3D2C11";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        using var singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            // Ya hay una instancia corriendo: le pedimos que se muestre y salimos sin abrir una ventana nueva.
            try
            {
                using var existingInstanceEvent = EventWaitHandle.OpenExisting(ShowRequestEventName);
                existingInstanceEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Botrix Refill ya está abierto. Revisa el ícono junto al reloj, en la bandeja del sistema.",
                    "Botrix Refill",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
