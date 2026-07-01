using EventPlatform.PrintRelay.Core;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EventPlatform.PrintRelay.App.Printing;

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
            Name = "WebView2PrintUi",
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

        ConfigureWebViewForPageLayout();
        await WaitForDocumentReadyAsync(_webView.CoreWebView2).ConfigureAwait(true);

        var settings = CreatePrintSettings(_webView.CoreWebView2.Environment);

        if (IsPrintToPdfDriver(printerName))
        {
            var outputPath = Path.Combine(
                Path.GetTempPath(),
                $"print-relay-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

            settings.PrinterName = printerName;

            var pdfResult = await _webView.CoreWebView2
                .PrintToPdfAsync(outputPath, settings)
                .ConfigureAwait(true);

            if (!pdfResult)
            {
                throw new InvalidOperationException(
                    $"WebView2 failed to save PDF via \"{printerName}\".");
            }

            return;
        }

        var tempPdfPath = Path.Combine(
            Path.GetTempPath(),
            $"print-relay-{Guid.NewGuid():N}.pdf");

        try
        {
            var rendered = await _webView.CoreWebView2
                .PrintToPdfAsync(tempPdfPath, settings)
                .ConfigureAwait(true);

            if (!rendered)
            {
                throw new InvalidOperationException("WebView2 failed to render badge PDF.");
            }

            PdfSpooler.PrintFile(tempPdfPath, printerName);
        }
        finally
        {
            try
            {
                if (File.Exists(tempPdfPath))
                {
                    File.Delete(tempPdfPath);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    private static bool IsPrintToPdfDriver(string printerName) =>
        printerName.Contains("print to pdf", StringComparison.OrdinalIgnoreCase);

    private static CoreWebView2PrintSettings CreatePrintSettings(
        CoreWebView2Environment environment)
    {
        var settings = environment.CreatePrintSettings();
        settings.ShouldPrintBackgrounds = true;
        settings.ShouldPrintHeaderAndFooter = false;
        settings.MarginTop = 0;
        settings.MarginBottom = 0;
        settings.MarginLeft = 0;
        settings.MarginRight = 0;
        settings.MediaSize = CoreWebView2PrintMediaSize.Custom;
        settings.PageWidth = RelayConstants.Cr80WidthInches;
        settings.PageHeight = RelayConstants.Cr80HeightInches;
        settings.ScaleFactor = 1.0;
        return settings;
    }

    private void ConfigureWebViewForPageLayout()
    {
        var widthPx = (int)Math.Round(RelayConstants.Cr80WidthInches * 96);
        var heightPx = (int)Math.Round(RelayConstants.Cr80HeightInches * 96);

        _hostForm!.Size = new Size(widthPx, heightPx);
        _webView!.Size = new Size(widthPx, heightPx);
    }

    private static async Task WaitForDocumentReadyAsync(CoreWebView2 webView)
    {
        await webView.ExecuteScriptAsync(
            """
            (async () => {
                if (document.fonts && document.fonts.ready) {
                    await document.fonts.ready;
                }

                await new Promise(resolve =>
                    requestAnimationFrame(() => requestAnimationFrame(resolve)));
            })();
            """)
            .ConfigureAwait(true);
    }
}
