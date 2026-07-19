using System.Text.Json;
using System.Text.RegularExpressions;

namespace EventPlatform.PrintRelay.Core.Printing;

/// <summary>
/// Resolves badge page dimensions from server <c>badge_html</c> and <c>badge_document</c> metadata.
/// Does not render layout from <c>badge_document</c> — format fields only.
/// </summary>
public static class BadgePageDimensionResolver
{
    private static readonly Regex PageSizeRegex = new(
        @"@page\s*\{[^}]*\bsize\s*:\s*(\d+(?:\.\d+)?)\s*mm\s+(\d+(?:\.\d+)?)\s*mm",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static BadgePageDimensions Resolve(string? badgeHtml, JsonElement? badgeDocument)
    {
        if (TryParseFromHtml(badgeHtml, out var widthMm, out var heightMm))
        {
            return new BadgePageDimensions(widthMm, heightMm, BadgePageSizeSource.Html);
        }

        if (TryParseFromDocument(badgeDocument, out widthMm, out heightMm))
        {
            return new BadgePageDimensions(widthMm, heightMm, BadgePageSizeSource.Document);
        }

        return new BadgePageDimensions(
            RelayConstants.Cr80WidthMm,
            RelayConstants.Cr80HeightMm,
            BadgePageSizeSource.Default);
    }

    private static bool TryParseFromHtml(string? badgeHtml, out double widthMm, out double heightMm)
    {
        widthMm = 0;
        heightMm = 0;

        if (string.IsNullOrWhiteSpace(badgeHtml))
        {
            return false;
        }

        var match = PageSizeRegex.Match(badgeHtml);
        if (!match.Success)
        {
            return false;
        }

        if (!double.TryParse(
                match.Groups[1].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out widthMm))
        {
            return false;
        }

        if (!double.TryParse(
                match.Groups[2].Value,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out heightMm))
        {
            return false;
        }

        return widthMm > 0 && heightMm > 0;
    }

    private static bool TryParseFromDocument(JsonElement? badgeDocument, out double widthMm, out double heightMm)
    {
        widthMm = 0;
        heightMm = 0;

        if (badgeDocument is not { ValueKind: JsonValueKind.Object })
        {
            return false;
        }

        if (!badgeDocument.Value.TryGetProperty("template", out var template)
            || template.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!template.TryGetProperty("canvas_config", out var canvasConfig)
            || canvasConfig.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!canvasConfig.TryGetProperty("format", out var format)
            || format.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!format.TryGetProperty("physicalWidth", out var widthElement)
            || !format.TryGetProperty("physicalHeight", out var heightElement))
        {
            return false;
        }

        if (!widthElement.TryGetDouble(out widthMm) || !heightElement.TryGetDouble(out heightMm))
        {
            return false;
        }

        return widthMm > 0 && heightMm > 0;
    }
}
