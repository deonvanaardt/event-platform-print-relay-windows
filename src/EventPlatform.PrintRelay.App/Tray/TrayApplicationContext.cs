using EventPlatform.PrintRelay.Core.Settings;

namespace EventPlatform.PrintRelay.App.Tray;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly RelaySettings _settings;
    private readonly string _settingsPath;
    private readonly NotifyIcon _notifyIcon;
    private readonly Form _syncForm;
    private readonly ContextMenuStrip _menu;
    private Icon? _trayIcon;
    private RelayRuntime? _runtime;
    private StatusForm? _statusForm;
    private SettingsForm? _settingsForm;
    private bool _restartRequested;
    private bool _startupFailed;

    public TrayApplicationContext(RelaySettings settings, string settingsPath)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));

        _syncForm = new Form
        {
            ShowInTaskbar = false,
            Opacity = 0,
            Width = 1,
            Height = 1,
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
        };

        MainForm = _syncForm;

        _menu = BuildMenu();

        var startingIcon = CreateIcon(RelayTrayIconState.Reconnecting);
        _trayIcon = startingIcon;
        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Print Relay — Starting…",
            Icon = startingIcon,
            ContextMenuStrip = _menu,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowStatusForm();

        RelayStartupLog.Write("NotifyIcon created and visible=true.");

        _syncForm.Show();
        _syncForm.Hide();

        var initTimer = new System.Windows.Forms.Timer { Interval = 1 };
        initTimer.Tick += (_, _) =>
        {
            initTimer.Stop();
            initTimer.Dispose();
            BeginRuntimeInitialization();
        };
        initTimer.Start();
    }

    public bool RestartRequested => _restartRequested;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_runtime is not null)
            {
                _runtime.SessionState.Changed -= OnSessionChanged;
            }

            _statusForm?.Dispose();
            _settingsForm?.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _syncForm.Dispose();
            _runtime?.Dispose();
            _trayIcon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BeginRuntimeInitialization()
    {
        SetMenuEnabled(false);

        Task.Run(() =>
            {
                try
                {
                    return new RelayRuntime(_settings, _settingsPath);
                }
                catch (InvalidOperationException ex)
                    when (IsWebView2RuntimeMissing(ex))
                {
                    throw new InvalidOperationException(
                        "Print Relay needs the Microsoft Edge WebView2 Runtime.",
                        ex);
                }
            })
            .ContinueWith(
                task =>
                {
                    if (_syncForm.IsDisposed)
                    {
                        if (task.Status == TaskStatus.RanToCompletion)
                        {
                            task.Result?.Dispose();
                        }

                        return;
                    }

                    void ApplyResult()
                    {
                        if (_syncForm.IsDisposed)
                        {
                            if (task.Status == TaskStatus.RanToCompletion)
                            {
                                task.Result?.Dispose();
                            }

                            return;
                        }

                        if (task.IsFaulted)
                        {
                            HandleStartupFailure(task.Exception?.GetBaseException());
                            return;
                        }

                        _runtime = task.Result;
                        _runtime.SessionState.Changed += OnSessionChanged;
                        SetMenuEnabled(true);
                        UpdateTrayIcon();

                        _notifyIcon.ShowBalloonTip(
                            4000,
                            "Print Relay",
                            "Print Relay is running. Right-click the tray icon for Status.",
                            ToolTipIcon.Info);
                    }

                    if (_syncForm.InvokeRequired)
                    {
                        _syncForm.BeginInvoke(ApplyResult);
                    }
                    else
                    {
                        ApplyResult();
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
    }

    private void HandleStartupFailure(Exception? error)
    {
        _startupFailed = true;
        _notifyIcon.Text = "Print Relay — Error";
        AssignTrayIcon(RelayTrayIconState.Error);

        var message = error?.Message ?? "Print Relay could not start.";
        RelayStartupLog.Write($"Startup failed: {error}");

        if (message.Contains("needs the Microsoft Edge WebView2 Runtime", StringComparison.Ordinal))
        {
            var result = MessageBox.Show(
                message + "\n\nOpen the WebView2 download page now?",
                "Print Relay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(
                            "https://go.microsoft.com/fwlink/p/?LinkId=2124703")
                        {
                            UseShellExecute = true,
                        });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Print Relay",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            MessageBox.Show(
                message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        ExitThread();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        menu.Items.Add("Status", null, (_, _) => ShowStatusForm());
        menu.Items.Add("Select printer", null, (_, _) => ShowSettingsForm());
        menu.Items.Add("Print test badge", null, async (_, _) => await RunSafeAsync(PrintTestBadgeAsync));
        menu.Items.Add("Test connection", null, async (_, _) => await RunSafeAsync(TestConnectionAsync));
        menu.Items.Add("Copy diagnostics", null, (_, _) => CopyDiagnostics());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings", null, (_, _) => ShowSettingsForm());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());

        return menu;
    }

    private void SetMenuEnabled(bool enabled)
    {
        foreach (ToolStripItem item in _menu.Items)
        {
            if (item is ToolStripSeparator)
            {
                continue;
            }

            item.Enabled = enabled || string.Equals(item.Text, "Quit", StringComparison.Ordinal);
        }
    }

    private RelayRuntime RequireRuntime()
    {
        if (_runtime is null)
        {
            throw new InvalidOperationException(
                _startupFailed
                    ? "Print Relay failed to start."
                    : "Print Relay is still starting. Try again in a moment.");
        }

        return _runtime;
    }

    private void ShowStatusForm()
    {
        try
        {
            var runtime = RequireRuntime();

            if (_statusForm is null || _statusForm.IsDisposed)
            {
                _statusForm = new StatusForm(runtime);
                _statusForm.FormClosed += (_, _) => _statusForm = null;
            }

            _statusForm.Show();
            _statusForm.BringToFront();
            _statusForm.WindowState = FormWindowState.Normal;
            _statusForm.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void ShowSettingsForm()
    {
        try
        {
            var runtime = RequireRuntime();

            if (_settingsForm is null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(runtime, RequestRestart);
                _settingsForm.FormClosed += (_, _) => _settingsForm = null;
            }

            _settingsForm.Show();
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void RequestRestart()
    {
        _restartRequested = true;
        ExitThread();
    }

    private void CopyDiagnostics()
    {
        try
        {
            Clipboard.SetText(RequireRuntime().BuildDiagnosticsJson());

            _notifyIcon.ShowBalloonTip(
                3000,
                "Print Relay",
                "Diagnostics copied to clipboard.",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task PrintTestBadgeAsync()
    {
        await RequireRuntime().PrintTestBadgeAsync().ConfigureAwait(true);

        _notifyIcon.ShowBalloonTip(
            3000,
            "Print Relay",
            "Test badge sent to printer.",
            ToolTipIcon.Info);
    }

    private Task TestConnectionAsync() => RequireRuntime().TestConnectionAsync();

    private static async Task RunSafeAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Print Relay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnSessionChanged()
    {
        if (_syncForm.IsDisposed || _runtime is null)
        {
            return;
        }

        if (_syncForm.InvokeRequired)
        {
            _syncForm.BeginInvoke(UpdateTrayIcon);
            return;
        }

        UpdateTrayIcon();
    }

    private void UpdateTrayIcon()
    {
        if (_runtime is null)
        {
            return;
        }

        _notifyIcon.Text = TruncateTooltip(_runtime.GetTrayTooltip());
        AssignTrayIcon(_runtime.GetTrayIconState());
    }

    private Icon AssignTrayIcon(RelayTrayIconState state)
    {
        var icon = CreateIcon(state);
        var previous = _trayIcon;
        _notifyIcon.Icon = icon;
        _trayIcon = icon;
        previous?.Dispose();
        return icon;
    }

    private static Icon CreateIcon(RelayTrayIconState state)
    {
        Icon source = state switch
        {
            RelayTrayIconState.Reconnecting => SystemIcons.Warning,
            RelayTrayIconState.Error => SystemIcons.Error,
            _ => SystemIcons.Information,
        };

        return (Icon)source.Clone();
    }

    private static string TruncateTooltip(string text) =>
        text.Length <= 63 ? text : text[..60] + "…";

    private static bool IsWebView2RuntimeMissing(InvalidOperationException ex) =>
        RelayAppInfo.TryGetWebView2Version() is null
        && (ex.Message.Contains("WebView2", StringComparison.OrdinalIgnoreCase)
            || ex.InnerException?.Message.Contains("WebView2", StringComparison.OrdinalIgnoreCase) == true);
}
