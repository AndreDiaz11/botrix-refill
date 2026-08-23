using System;
using System.Drawing;
using System.Windows.Forms;

namespace BotrixRefill.Services;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event Action? OpenRequested;
    public event Action? ExitRequested;

    public TrayService(string iconPath)
    {
        Icon icon;
        try
        {
            icon = new Icon(iconPath);
        }
        catch
        {
            icon = SystemIcons.Application;
        }

        var menu = new ContextMenuStrip();
        menu.Items.Add("Abrir Botrix Refill", null, (_, _) => OpenRequested?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Salir", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Botrix Refill",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _notifyIcon.Click += (_, e) =>
        {
            if (e is MouseEventArgs { Button: MouseButtons.Left }) OpenRequested?.Invoke();
        };
    }

    public void ShowNotification(string title, string body)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = body;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
