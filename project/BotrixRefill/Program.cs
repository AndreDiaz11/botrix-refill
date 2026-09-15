using Avalonia;
using System;
using System.Threading;
using BotrixRefill.Services;
using Velopack;

namespace BotrixRefill;

sealed class Program
{
    // Prefijo "Global\" (en vez de un nombre local) para que el candado se vea
    // entre procesos sin importar diferencias de sesión/elevación entre ellos.
    private const string SingleInstanceMutexName = @"Global\BotrixRefill-SingleInstance-9F3D2C11";
    public const string ShowRequestEventName = @"Global\BotrixRefill-ShowRequest-9F3D2C11";

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        bool createdNew;
        Mutex singleInstanceMutex;
        try
        {
            singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
        }
        catch (UnauthorizedAccessException)
        {
            // El mutex ya existe pero con permisos que este proceso no puede abrir
            // (ej. la otra instancia corre con otro nivel de privilegios). Tratamos
            // esto igual que "ya hay una instancia corriendo": avisamos y salimos,
            // nunca dejamos que se abra una segunda ventana en este caso.
            ErrorLogger.LogInfo("single-instance", "Mutex existente sin acceso (UnauthorizedAccessException) — se asume que ya hay una instancia corriendo");
            System.Windows.Forms.MessageBox.Show(
                "Botrix Refill ya está abierto. Revisa el ícono junto al reloj, en la bandeja del sistema.",
                "Botrix Refill",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            return;
        }

        using var _ = singleInstanceMutex;
        if (!createdNew)
        {
            ErrorLogger.LogInfo("single-instance", "Instancia existente detectada — se pide traerla al frente");
            // Ya hay una instancia corriendo: le pedimos que se muestre y salimos sin abrir una ventana nueva.
            try
            {
                using var existingInstanceEvent = EventWaitHandle.OpenExisting(ShowRequestEventName);
                existingInstanceEvent.Set();
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("single-instance-signal", ex);
                System.Windows.Forms.MessageBox.Show(
                    "Botrix Refill ya está abierto. Revisa el ícono junto al reloj, en la bandeja del sistema.",
                    "Botrix Refill",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);
            }
            return;
        }

        ErrorLogger.LogInfo("single-instance", "Instancia nueva — candado creado en este proceso");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
