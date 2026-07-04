using System.Runtime.InteropServices;
using PdfiumPrinter.LibraryLoader;

namespace EventPlatform.PrintRelay.App.Printing;

/// <summary>
/// PdfiumPrinter expects runtimes/win-{arch}/native/pdfium.dll; bblanchon.PDFium.Win32
/// places copies under {arch}/ at the app root. Register fallbacks before first print.
/// </summary>
internal static class PdfiumNativeBootstrap
{
    private static int _initialized;

    public static void EnsureLoaded()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        NativeLibraryLoader.SetLibraryLoader(new RelayPdfiumLibraryLoader());
    }
}

internal sealed class RelayPdfiumLibraryLoader : ILibraryLoader
{
    public LoadResult OpenLibrary(string path)
    {
        foreach (var candidate in EnumerateCandidates(path))
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            if (NativeLibrary.TryLoad(candidate, out _))
            {
                return LoadResult.Success;
            }
        }

        return LoadResult.Failure($"pdfium.dll not found in fallback paths (default: {path}).");
    }

    private static IEnumerable<string> EnumerateCandidates(string defaultPath)
    {
        var baseDir = AppContext.BaseDirectory;

        // Self-contained win-x64 publish often hoists pdfium.dll to the app root.
        yield return Path.Combine(baseDir, "pdfium.dll");

        var archFolder = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => null,
        };

        if (archFolder is not null)
        {
            yield return Path.Combine(baseDir, archFolder, "pdfium.dll");
        }

        yield return defaultPath;
    }
}
