using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification.Vision;

internal sealed class ClaudeSleeveReader(
    AnthropicClient client,
    IOptions<IdentificationOptions> options,
    ILogger<ClaudeSleeveReader> logger) : ISleeveReader
{
    private const string Instruction = """
        This is a photograph of a vinyl record's sleeve or centre label.

        Read off only what is actually printed. Do not identify the album from memory,
        and do not infer a value you cannot see — a wrong catalogue number sends the
        search to the wrong pressing, which is worse than no catalogue number at all.

        The catalogue number is the label's own reference, usually printed small on the
        spine, rear sleeve or centre label (for example "BLP 4003", "SHVL 804",
        "CS 8163"). It is not the barcode and not a price.

        Return null for anything you cannot read with confidence.
        """;

    private static readonly Dictionary<string, JsonElement> Schema = BuildSchema();

    private readonly IdentificationOptions _options = options.Value;

    public async Task<SleeveDetails> ReadAsync(
        ReadOnlyMemory<byte> image,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            throw new SleeveReadException(
                "No Anthropic API key is configured, so the sleeve cannot be read.");
        }

        if (image.IsEmpty)
        {
            return new SleeveDetails();
        }

        try
        {
            var response = await client.Messages.Create(
                new MessageCreateParams
                {
                    Model = _options.Model,
                    MaxTokens = 16000,
                    // Reading printed text off a sleeve is not a hard reasoning problem;
                    // low effort keeps the per-scan cost down.
                    Thinking = new ThinkingConfigAdaptive(),
                    OutputConfig = new OutputConfig
                    {
                        Effort = Effort.Low,
                        Format = new JsonOutputFormat { Schema = Schema },
                    },
                    Messages =
                    [
                        new()
                        {
                            Role = Role.User,
                            Content = new List<ContentBlockParam>
                            {
                                new ImageBlockParam
                                {
                                    Source = new Base64ImageSource
                                    {
                                        Data = Convert.ToBase64String(image.Span),
                                        MediaType = contentType,
                                    },
                                },
                                new TextBlockParam { Text = Instruction },
                            },
                        },
                    ],
                },
                cancellationToken);

            if (response.StopReason == "refusal")
            {
                logger.LogWarning(
                    "The sleeve reading was declined: {Category}.",
                    response.StopDetails?.Category);
                return new SleeveDetails();
            }

            var json = string.Concat(
                response.Content.Select(block => block.Value).OfType<TextBlock>().Select(t => t.Text));

            return Parse(json);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading the sleeve failed.");

            // The API's own words: it is the part that says what to change, and a
            // swallowed failure here is indistinguishable from an illegible photograph.
            throw new SleeveReadException($"The sleeve could not be read. {ex.Message}", ex);
        }
    }

    /// <summary>Exposed for testing: the model's JSON is the part most likely to surprise us.</summary>
    internal static SleeveDetails Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SleeveDetails();
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new SleeveDetails
            {
                Barcode = ReadString(root, "barcode"),
                ArtistName = ReadString(root, "artistName"),
                Title = ReadString(root, "title"),
                LabelName = ReadString(root, "labelName"),
                CatalogueNumber = ReadString(root, "catalogueNumber"),
                Year = ReadYear(root, "year"),
            };
        }
        catch (JsonException)
        {
            return new SleeveDetails();
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    private static int? ReadYear(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var year)
        && year is > 1850 and < 2200
            ? year
            : null;

    private static Dictionary<string, JsonElement> BuildSchema()
    {
        static object Nullable(string type, string description) =>
            new { type = new[] { type, "null" }, description };

        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                barcode = Nullable("string", "The barcode digits, if a barcode is visible."),
                artistName = Nullable("string", "The performing artist as printed."),
                title = Nullable("string", "The release title as printed."),
                labelName = Nullable("string", "The record label, for example Blue Note or Harvest."),
                catalogueNumber = Nullable("string", "The label's catalogue number, for example BLP 4003."),
                year = Nullable("integer", "A four digit year, only if one is printed."),
            }),
            ["required"] = JsonSerializer.SerializeToElement(
                new[] { "barcode", "artistName", "title", "labelName", "catalogueNumber", "year" }),
        };
    }
}
