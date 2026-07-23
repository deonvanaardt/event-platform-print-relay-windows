using System.Text.Json;
using Json.Schema;
using Xunit.Abstractions;

namespace EventPlatform.PrintRelay.Core.Tests.Contracts;

public sealed class PlatformSchemaContractTests
{
    private static readonly string SchemaRoot = ResolveSchemaRoot();
    private static readonly EvaluationOptions EvaluationOptions = new()
    {
        OutputFormat = OutputFormat.Hierarchical,
    };

    static PlatformSchemaContractTests()
    {
        RegisterPinnedSchemas();
    }

    private readonly ITestOutputHelper _output;

    public PlatformSchemaContractTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("desk-setup-code.v1.json", "fixtures/desk-setup-code.v1.valid.json")]
    [InlineData("pending-job.response.json", "fixtures/pending-response.valid.json")]
    [InlineData("pending-job.response.json", "fixtures/pending-response.empty.json")]
    [InlineData("pair-exchange.response.json", "fixtures/pair-exchange.response.valid.json")]
    public void Fixture_validates_against_pinned_schema(string schemaFile, string fixtureFile)
    {
        var schema = LoadSchema(schemaFile);
        var instance = LoadJson(fixtureFile);

        var result = schema.Evaluate(instance, EvaluationOptions);

        if (!result.IsValid)
        {
            _output.WriteLine(result.ToString());
        }

        Assert.True(result.IsValid, $"{fixtureFile} must validate against {schemaFile}");
    }

    [Fact]
    public void Desk_setup_code_fixture_rejects_missing_secret()
    {
        var schema = LoadSchema("desk-setup-code.v1.json");
        var invalid = JsonDocument.Parse(
            """
            {
              "v": 1,
              "api_url": "https://app.example.com",
              "desk_name": "Main entrance"
            }
            """).RootElement;

        var result = schema.Evaluate(invalid, EvaluationOptions);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Pending_response_rejects_missing_jobs_property()
    {
        var schema = LoadSchema("pending-job.response.json");
        var invalid = JsonDocument.Parse("{}").RootElement;

        var result = schema.Evaluate(invalid, EvaluationOptions);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Pending_job_rejects_missing_badge_html()
    {
        var schema = LoadSchema("pending-job.response.json");
        var invalid = JsonDocument.Parse(
            """
            {
              "jobs": [
                {
                  "id": "77777777-7777-4777-8777-777777777777",
                  "status": "queued",
                  "desk_id": "44444444-4444-4444-8444-444444444444",
                  "event_id": "33333333-3333-4333-8333-333333333333",
                  "registration_id": "55555555-5555-4555-8555-555555555555",
                  "idempotency_key": "66666666-6666-4666-8666-666666666666",
                  "is_reprint": false,
                  "created_at": "2026-06-27T12:00:00.000Z",
                  "badge_document": {
                    "template": {
                      "canvas_config": {
                        "format": {
                          "id": "cr80",
                          "name": "CR80",
                          "physicalWidth": 85.6,
                          "physicalHeight": 54,
                          "safeZone": 2,
                          "sides": [
                            {
                              "id": "front",
                              "label": "Front",
                              "widthMm": 85.6,
                              "heightMm": 54,
                              "offsetX": 0,
                              "offsetY": 0
                            }
                          ]
                        },
                        "elements": [],
                        "guides": [],
                        "background": { "front": { "color": "#ffffff" } }
                      }
                    },
                    "data": {
                      "full_name": "Test"
                    }
                  }
                }
              ]
            }
            """).RootElement;

        var result = schema.Evaluate(invalid, EvaluationOptions);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Platform_pin_records_commit_sha()
    {
        var pinPath = Path.Combine(SchemaRoot, "platform-pin.json");
        Assert.True(File.Exists(pinPath), "schemas/platform-pin.json must exist");

        using var pin = JsonDocument.Parse(File.ReadAllText(pinPath));
        var sha = pin.RootElement.GetProperty("commit_sha").GetString();

        Assert.False(string.IsNullOrWhiteSpace(sha));
        Assert.Equal(40, sha!.Length);
    }

    [Fact]
    public void Platform_pin_lists_existing_schema_files()
    {
        using var pin = JsonDocument.Parse(File.ReadAllText(Path.Combine(SchemaRoot, "platform-pin.json")));
        var repoRoot = Directory.GetParent(SchemaRoot)!.FullName;

        foreach (var path in pin.RootElement.GetProperty("schema_paths").EnumerateArray())
        {
            var relativePath = path.GetString();
            Assert.False(string.IsNullOrWhiteSpace(relativePath));

            var fullPath = Path.Combine(repoRoot, relativePath!);
            Assert.True(File.Exists(fullPath), $"Pinned schema file must exist: {relativePath}");
        }
    }

    private static JsonSchema LoadSchema(string fileName)
    {
        var path = Path.Combine(SchemaRoot, fileName);
        return JsonSchema.FromFile(path);
    }

    private static JsonElement LoadJson(string relativePath)
    {
        var path = Path.Combine(SchemaRoot, relativePath);
        return JsonDocument.Parse(File.ReadAllText(path)).RootElement;
    }

    private static void RegisterPinnedSchemas()
    {
        foreach (var schemaPath in Directory.EnumerateFiles(SchemaRoot, "*.json"))
        {
            if (Path.GetFileName(schemaPath) is "platform-pin.json")
            {
                continue;
            }

            var schema = JsonSchema.FromFile(schemaPath);
            SchemaRegistry.Global.Register(schema);
        }
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

        throw new InvalidOperationException("Could not locate schemas/ directory from test output.");
    }
}
