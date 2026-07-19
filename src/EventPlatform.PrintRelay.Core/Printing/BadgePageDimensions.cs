namespace EventPlatform.PrintRelay.Core.Printing;

/// <summary>Where resolved badge page dimensions came from.</summary>
public enum BadgePageSizeSource
{
    Html,
    Document,
    Default,
}

/// <summary>Resolved physical page size for a badge print job.</summary>
public sealed record BadgePageDimensions(
    double WidthMm,
    double HeightMm,
    BadgePageSizeSource Source)
{
    public double WidthInches => WidthMm / 25.4;

    public double HeightInches => HeightMm / 25.4;
}
