using System.Drawing.Printing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EventPlatform.PrintRelay.Spike.Printing;

public sealed class WebView2SilentPrinter : IDisposable
{
    private readonly Form _hostForm;
    private readonly WebView2 _webView;
    private bool _initialized;

    public WebView2SilentPrinter()
    {
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
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        _hostForm.CreateControl();

        var environment = await CoreWebView2Environment
            .CreateAsync()
            .ConfigureAwait(true);

        await _webView
            .EnsureCoreWebView2Async(environment)
            .ConfigureAwait(true);

        _initialized = true;
    }

    public async Task PrintHtmlAsync(
        string html,
        string printerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        await InitializeAsync(cancellationToken).ConfigureAwait(true);

        var navigationCompleted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _webView.CoreWebView2!.NavigationCompleted -= Handler;
            navigationCompleted.TrySetResult(args.IsSuccess);
        }

        _webView.CoreWebView2!.NavigationCompleted += Handler;
        _webView.CoreWebView2.NavigateToString(html);

        var navigationSucceeded = await navigationCompleted.Task.ConfigureAwait(true);

        if (!navigationSucceeded)
        {
            throw new InvalidOperationException("Failed to load badge HTML in WebView2.");
        }

        var settings = _webView.CoreWebView2.Environment.CreatePrintSettings();
        settings.PrinterName = printerName;
        settings.ShouldPrintBackgrounds = true;
        settings.ShouldPrintHeaderAndFooter = false;
        settings.MarginTop = 0;
        settings.MarginBottom = 0;
        settings.MarginLeft = 0;
        settings.MarginRight = 0;

        var status = await _webView.CoreWebView2
            .PrintAsync(settings)
            .ConfigureAwait(true);

        if (status != CoreWebView2PrintStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebView2 print failed with status: {status}.");
        }
    }

    public async Task PrintUriAsync(
        string uri,
        string printerName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(printerName);

        await InitializeAsync(cancellationToken).ConfigureAwait(true);

        var navigationCompleted = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            _webView.CoreWebView2!.NavigationCompleted -= Handler;
            navigationCompleted.TrySetResult(args.IsSuccess);
        }

        _webView.CoreWebView2!.NavigationCompleted += Handler;
        _webView.CoreWebView2.Navigate(uri);

        var navigationSucceeded = await navigationCompleted.Task.ConfigureAwait(true);

        if (!navigationSucceeded)
        {
            throw new InvalidOperationException($"Failed to load URI in WebView2: {uri}");
        }

        var settings = _webView.CoreWebView2.Environment.CreatePrintSettings();
        settings.PrinterName = printerName;
        settings.ShouldPrintBackgrounds = true;
        settings.ShouldPrintHeaderAndFooter = false;
        settings.MarginTop = 0;
        settings.MarginBottom = 0;
        settings.MarginLeft = 0;
        settings.MarginRight = 0;

        var status = await _webView.CoreWebView2
            .PrintAsync(settings)
            .ConfigureAwait(true);

        if (status != CoreWebView2PrintStatus.Succeeded)
        {
            throw new InvalidOperationException(
                $"WebView2 print failed with status: {status}.");
        }
    }

    public void Dispose()
    {
        _webView.Dispose();
        _hostForm.Dispose();
    }
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
