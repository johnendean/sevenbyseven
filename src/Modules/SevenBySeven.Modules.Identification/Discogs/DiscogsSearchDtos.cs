using System.Text.Json.Serialization;

namespace SevenBySeven.Modules.Identification.Discogs;

internal sealed record DiscogsSearchResponse
{
    [JsonPropertyName("results")]
    public IReadOnlyList<DiscogsSearchResult>? Results { get; init; }
}

internal sealed record DiscogsSearchResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    /// <summary>Discogs returns artist and title as one string: "Miles Davis - Kind Of Blue".</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A string, not a number: it can be empty or partial.</summary>
    [JsonPropertyName("year")]
    public string? Year { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; init; }

    [JsonPropertyName("catno")]
    public string? CatalogueNumber { get; init; }

    [JsonPropertyName("label")]
    public IReadOnlyList<string>? Labels { get; init; }

    [JsonPropertyName("format")]
    public IReadOnlyList<string>? Formats { get; init; }

    [JsonPropertyName("master_id")]
    public int? MasterId { get; init; }
}
