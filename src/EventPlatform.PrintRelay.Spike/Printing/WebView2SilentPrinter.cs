using System.Drawing.Printing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EventPlatform.PrintRelay.Spike.Printing;

public sealed class WebView2SilentPrinter : IDisposable
{
    private readonly Thread _uiThread;
    private readonly ManualResetEventSlim _loopReady = new(false);
    private readonly ManualResetEventSlim _webviewReady = new(false);
    private Form? _hostForm;
    private WebView2? _webView;
    private Exception? _startupException;
    private bool _disposed;

    public WebView2SilentPrinter()
    {
        _uiThread = new Thread(RunMessageLoop)
        {
            IsBackground = true,
            Name = "WebView2SpikeUi",
        };
        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();

        _loopReady.Wait();
        _webviewReady.Wait();

        if (_startupException is not null)
        {
            throw new InvalidOperationException(
                "Failed to initialize WebView2.",
                _startupException);
        }
    }

    public Task PrintHtmlAsync(
        string html,
        string printerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        return InvokeAsync(() => PrintLoadedContentAsync(
            navigate: webView => webView.CoreWebView2!.NavigateToString(html),
            printerName,
            cancellationToken));
    }

    public Task PrintUriAsync(
        string uri,
        string printerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        return InvokeAsync(() => PrintLoadedContentAsync(
            navigate: webView => webView.CoreWebView2!.Navigate(uri),
            printerName,
            cancellationToken));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_hostForm is { IsDisposed: false })
        {
            try
            {
                _hostForm.Invoke(_hostForm.Close);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        _uiThread.Join(TimeSpan.FromSeconds(10));
        _loopReady.Dispose();
        _webviewReady.Dispose();
    }

    private void RunMessageLoop()
    {
        try
        {
            ApplicationConfiguration.Initialize();

            _hostForm = new Form
            {
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized,
                Opacity = 0,
                Width = 1,
                Height = 1,
            };

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
            };

            _hostForm.Controls.Add(_webView);
            _hostForm.Shown += OnFormShown;
            _loopReady.Set();

            Application.Run(_hostForm);
        }
        catch (Exception ex)
        {
            _startupException = ex;
            _loopReady.Set();
            _webviewReady.Set();
        }
    }

    private async void OnFormShown(object? sender, EventArgs e)
    {
        if (_hostForm is null || _webView is null)
        {
            _startupException = new InvalidOperationException("WebView2 host was not created.");
            _webviewReady.Set();
            return;
        }

        _hostForm.Shown -= OnFormShown;

        try
        {
            var environment = await CoreWebView2Environment
                .CreateAsync()
                .ConfigureAwait(true);

            await _webView
                .EnsureCoreWebView2Async(environment)
                .ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _startupException = ex;
        }
        finally
        {
            _webviewReady.Set();
        }
    }

    private Task InvokeAsync(Func<Task> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _hostForm!.BeginInvoke(new Action(async () =>
        {
            try
            {
                await action().ConfigureAwait(true);
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }));

        return tcs.Task;
    }

    private async Task PrintLoadedContentAsync(
        Action<WebView2> navigate,
        string printerName,
        CancellationToken cancellationToken)
    {
        if (_webView?.CoreWebView2 is null)
        {
            throw new InvalidOperationException("WebView2 is not ready.");
        }

        var navigationCompleted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _webView.CoreWebView2!.NavigationCompleted -= Handler;
            navigationCompleted.TrySetResult(args.IsSuccess);
        }

        _webView.CoreWebView2.NavigationCompleted += Handler;
        navigate(_webView);

        var navigationSucceeded = await navigationCompleted.Task
            .WaitAsync(cancellationToken)
            .ConfigureAwait(true);

        if (!navigationSucceeded)
        {
            throw new InvalidOperationException("Failed to load badge HTML in WebView2.");
        }

        var settings = _webView.CoreWebView2.Environment.CreatePrintSettings();
        settings.ShouldPrintBackgrounds = true;
        settings.ShouldPrintHeaderAndFooter = false;
        settings.MarginTop = 0;
        settings.MarginBottom = 0;
        settings.MarginLeft = 0;
        settings.MarginRight = 0;

        if (IsPrintToPdfDriver(printerName))
        {
            var outputPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                $"spike-print-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

            settings.PrinterName = printerName;

            var pdfResult = await _webView.CoreWebView2
                .PrintToPdfAsync(outputPath, settings)
                .ConfigureAwait(true);

            if (!pdfResult)
            {
                throw new InvalidOperationException(
                    $"WebView2 failed to save PDF via \"{printerName}\".");
            }

            Console.WriteLine($"Saved PDF to: {outputPath}");
            return;
        }

        settings.PrinterName = printerName;

        var status = await _webView.CoreWebView2
            .PrintAsync(settings)
            .ConfigureAwait(true);

        if (status != CoreWebView2PrintStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebView2 print failed with status: {status}.");
        }
    }

    private static bool IsPrintToPdfDriver(string printerName) =>
        printerName.Contains("print to pdf", StringComparison.OrdinalIgnoreCase);
}

public static class InstalledPrinters
{
    public static IReadOnlyList<string> List()
    {
        var printers = new List<string>();

        foreach (string printer in PrinterSettings.InstalledPrinters)
        {
            printers.Add(printer);
        }

        printers.Sort(StringComparer.OrdinalIgnoreCase);
        return printers;
    }

    public static bool Exists(string printerName)
    {
        return List().Contains(printerName, StringComparer.OrdinalIgnoreCase);
    }
}
