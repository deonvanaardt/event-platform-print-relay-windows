using System.Text.Json;
using EventPlatform.PrintRelay.Core;
using EventPlatform.PrintRelay.Core.Printing;

namespace EventPlatform.PrintRelay.Core.Tests.Printing;

public sealed class BadgePageDimensionResolverTests
{
    private const string Cr80HtmlSnippet = """
        <style>
        @page {
          size: 85.6mm 54mm;
          margin: 0;
        }
        </style>
        """;

    private const string A6HtmlSnippet = """
        <style>
        @page {
          size: 148mm 105mm;
          margin: 0;
        }
        </style>
        """;

    private const string A5PortraitHtmlSnippet = """
        <style>
        @page {
          size: 148mm 210mm;
          margin: 0;
        }
        </style>
        """;

    private const string A5LandscapeHtmlSnippet = """
        <style>
        @page {
          size: 210mm 148mm;
          margin: 0;
        }
        </style>
        """;

    [Fact]
    public void Resolve_parses_cr80_from_badge_html_atpage()
    {
        var result = BadgePageDimensionResolver.Resolve(Cr80HtmlSnippet, null);

        Assert.Equal(85.6, result.WidthMm);
        Assert.Equal(54.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_parses_a6_landscape_from_badge_html_atpage()
    {
        var result = BadgePageDimensionResolver.Resolve(A6HtmlSnippet, null);

        Assert.Equal(148.0, result.WidthMm);
        Assert.Equal(105.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_parses_a5_portrait_from_badge_html_atpage()
    {
        var result = BadgePageDimensionResolver.Resolve(A5PortraitHtmlSnippet, null);

        Assert.Equal(148.0, result.WidthMm);
        Assert.Equal(210.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_parses_a5_landscape_from_badge_html_atpage()
    {
        var result = BadgePageDimensionResolver.Resolve(A5LandscapeHtmlSnippet, null);

        Assert.Equal(210.0, result.WidthMm);
        Assert.Equal(148.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_parses_platform_fixture_badge_html()
    {
        var fixture = LoadPendingResponseFixture();
        var badgeHtml = fixture.GetProperty("jobs")[0].GetProperty("badge_html").GetString();

        var result = BadgePageDimensionResolver.Resolve(badgeHtml, null);

        Assert.Equal(148.0, result.WidthMm);
        Assert.Equal(105.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_uses_badge_document_when_atpage_missing()
    {
        var badgeDocument = JsonDocument.Parse("""
            {
              "template": {
                "canvas_config": {
                  "format": {
                    "physicalWidth": 148,
                    "physicalHeight": 105
                  }
                }
              }
            }
            """).RootElement;

        var result = BadgePageDimensionResolver.Resolve("<html></html>", badgeDocument);

        Assert.Equal(148.0, result.WidthMm);
        Assert.Equal(105.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Document, result.Source);
    }

    [Fact]
    public void Resolve_returns_cr80_default_for_null_inputs()
    {
        var result = BadgePageDimensionResolver.Resolve(null, null);

        Assert.Equal(RelayConstants.Cr80WidthMm, result.WidthMm);
        Assert.Equal(RelayConstants.Cr80HeightMm, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Default, result.Source);
    }

    [Fact]
    public void Resolve_returns_cr80_default_for_empty_html_and_missing_document()
    {
        var result = BadgePageDimensionResolver.Resolve(string.Empty, null);

        Assert.Equal(RelayConstants.Cr80WidthMm, result.WidthMm);
        Assert.Equal(RelayConstants.Cr80HeightMm, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Default, result.Source);
    }

    [Fact]
    public void Resolve_falls_back_when_atpage_size_is_malformed()
    {
        var badgeDocument = JsonDocument.Parse("""
            {
              "template": {
                "canvas_config": {
                  "format": {
                    "physicalWidth": 85.6,
                    "physicalHeight": 54
                  }
                }
              }
            }
            """).RootElement;

        var result = BadgePageDimensionResolver.Resolve(
            "<style>@page { size: abc mm 54mm; }</style>",
            badgeDocument);

        Assert.Equal(85.6, result.WidthMm);
        Assert.Equal(54.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Document, result.Source);
    }

    [Fact]
    public void Resolve_falls_back_to_default_when_document_format_is_invalid()
    {
        var badgeDocument = JsonDocument.Parse("""
            {
              "template": {
                "canvas_config": {
                  "format": {
                    "physicalWidth": 0,
                    "physicalHeight": 54
                  }
                }
              }
            }
            """).RootElement;

        var result = BadgePageDimensionResolver.Resolve("<html></html>", badgeDocument);

        Assert.Equal(RelayConstants.Cr80WidthMm, result.WidthMm);
        Assert.Equal(RelayConstants.Cr80HeightMm, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Default, result.Source);
    }

    [Fact]
    public void Resolve_prefers_html_over_badge_document()
    {
        var badgeDocument = JsonDocument.Parse("""
            {
              "template": {
                "canvas_config": {
                  "format": {
                    "physicalWidth": 200,
                    "physicalHeight": 100
                  }
                }
              }
            }
            """).RootElement;

        var result = BadgePageDimensionResolver.Resolve(Cr80HtmlSnippet, badgeDocument);

        Assert.Equal(85.6, result.WidthMm);
        Assert.Equal(54.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Html, result.Source);
    }

    [Fact]
    public void Resolve_uses_platform_fixture_badge_document_when_atpage_stripped()
    {
        var fixture = LoadPendingResponseFixture();
        var job = fixture.GetProperty("jobs")[0];
        var badgeDocument = job.GetProperty("badge_document");

        var result = BadgePageDimensionResolver.Resolve("<html><body></body></html>", badgeDocument);

        Assert.Equal(148.0, result.WidthMm);
        Assert.Equal(105.0, result.HeightMm);
        Assert.Equal(BadgePageSizeSource.Document, result.Source);
    }

    [Fact]
    public void BadgePageDimensions_exposes_inches_from_millimetres()
    {
        var dimensions = new BadgePageDimensions(85.6, 54.0, BadgePageSizeSource.Html);

        Assert.Equal(85.6 / 25.4, dimensions.WidthInches, precision: 6);
        Assert.Equal(54.0 / 25.4, dimensions.HeightInches, precision: 6);
    }

    private static JsonElement LoadPendingResponseFixture()
    {
        var schemaRoot = ResolveSchemaRoot();
        var path = Path.Combine(schemaRoot, "fixtures", "pending-response.valid.json");
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    private static string ResolveSchemaRoot()
    {
        var dir = AppContext.BaseDirectory;

        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "schemas", "platform-pin.json");

            if (File.Exists(candidate))
            {
                return Path.Combine(dir, "schemas");
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not locate schemas directory from test output.");
    }
}
