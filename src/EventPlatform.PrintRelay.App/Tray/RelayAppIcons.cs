using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace EventPlatform.PrintRelay.App.Tray;

internal static class RelayAppIcons
{
    private const string AppIconResource = "EventPlatform.PrintRelay.App.Assets.brand.app.ico";
    private const string TrayBaseResource = "EventPlatform.PrintRelay.App.Assets.brand.tray.base-32.png";

    private static readonly Color ConnectedColor = Color.FromArgb(0x16, 0xA3, 0x4A);
    private static readonly Color ReconnectingColor = Color.FromArgb(0xD9, 0x77, 0x06);
    private static readonly Color ErrorColor = Color.FromArgb(0xDC, 0x26, 0x26);

    private static Icon? _appIcon;
    private static Bitmap? _trayBaseBitmap;
    private static readonly object Sync = new();
    private static readonly Dictionary<RelayTrayIconState, Icon> TrayIcons = new();

    public static Icon LoadAppIcon()
    {
        lock (Sync)
        {
            _appIcon ??= LoadIconResource(AppIconResource);
            return (Icon)_appIcon.Clone();
        }
    }

    public static Icon CreateTrayIcon(RelayTrayIconState state)
    {
        lock (Sync)
        {
            if (TrayIcons.TryGetValue(state, out var cached))
            {
                return (Icon)cached.Clone();
            }

            var trayIcon = BuildTrayIcon(state);
            TrayIcons[state] = trayIcon;
            return (Icon)trayIcon.Clone();
        }
    }

    private static Icon BuildTrayIcon(RelayTrayIconState state)
    {
        var baseBitmap = GetTrayBaseBitmap();
        using var composed = new Bitmap(baseBitmap.Width, baseBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(composed))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(baseBitmap, 0, 0, baseBitmap.Width, baseBitmap.Height);

            var dotColor = state switch
            {
                RelayTrayIconState.Reconnecting => ReconnectingColor,
                RelayTrayIconState.Error => ErrorColor,
                _ => ConnectedColor,
            };

            // Monochrome tray base (brand pack §3) — slightly larger dot for 16–32px readability.
            var dotSize = Math.Max(8, (baseBitmap.Width * 10) / 32);
            var dotX = baseBitmap.Width - dotSize;
            var dotY = baseBitmap.Height - dotSize;

            using var brush = new SolidBrush(dotColor);
            graphics.FillEllipse(brush, dotX, dotY, dotSize, dotSize);
            using var outline = new Pen(Color.White, 1f);
            graphics.DrawEllipse(outline, dotX, dotY, dotSize, dotSize);
        }

        return BitmapToIcon(composed);
    }

    private static Bitmap GetTrayBaseBitmap()
    {
        _trayBaseBitmap ??= LoadBitmapResource(TrayBaseResource);
        return _trayBaseBitmap;
    }

    private static Icon LoadIconResource(string resourceName)
    {
        using var stream = OpenResourceStream(resourceName);
        return new Icon(stream);
    }

    private static Bitmap LoadBitmapResource(string resourceName)
    {
        using var stream = OpenResourceStream(resourceName);
        return new Bitmap(stream);
    }

    private static Stream OpenResourceStream(string resourceName)
    {
        var assembly = typeof(RelayAppIcons).Assembly;
        var stream = assembly.GetManifestResourceStream(resourceName);

        if (stream is null)
        {
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        }

        return stream;
    }

    private static Icon BitmapToIcon(Bitmap bitmap)
    {
        var handle = bitmap.GetHicon();

        try
        {
            using var icon = Icon.FromHandle(handle);
            return (Icon)icon.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
