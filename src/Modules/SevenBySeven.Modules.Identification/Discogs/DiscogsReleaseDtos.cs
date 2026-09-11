using System.Text.Json.Serialization;

namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// One release as the <c>/releases/{id}</c> endpoint returns it. The search endpoint
/// gives a summary only: the tracklist, genres and full images need this second call.
/// </summary>
internal sealed record DiscogsReleaseDetail
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("master_id")]
    public int? MasterId { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A number here, unlike the search endpoint, and 0 when unknown.</summary>
    [JsonPropertyName("year")]
    public int? Year { get; init; }

    /// <summary>"1959", "1959-08" or "1959-08-17" — the precision Discogs actually has.</summary>
    [JsonPropertyName("released")]
    public string? Released { get; init; }

    [JsonPropertyName("country")]
    public string? Country { get; init; }

    [JsonPropertyName("thumb")]
    public string? Thumb { get; init; }

    /// <summary>Discogs' own display form of the credited artists.</summary>
    [JsonPropertyName("artists_sort")]
    public string? ArtistsSort { get; init; }

    [JsonPropertyName("artists")]
    public IReadOnlyList<DiscogsArtist>? Artists { get; init; }

    [JsonPropertyName("labels")]
    public IReadOnlyList<DiscogsLabel>? Labels { get; init; }

    [JsonPropertyName("formats")]
    public IReadOnlyList<DiscogsFormat>? Formats { get; init; }

    [JsonPropertyName("identifiers")]
    public IReadOnlyList<DiscogsIdentifier>? Identifiers { get; init; }

    [JsonPropertyName("images")]
    public IReadOnlyList<DiscogsImage>? Images { get; init; }

    [JsonPropertyName("genres")]
    public IReadOnlyList<string>? Genres { get; init; }

    [JsonPropertyName("styles")]
    public IReadOnlyList<string>? Styles { get; init; }

    [JsonPropertyName("tracklist")]
    public IReadOnlyList<DiscogsTracklistEntry>? Tracklist { get; init; }
}

internal sealed record DiscogsArtist
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

internal sealed record DiscogsLabel
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("catno")]
    public string? CatalogueNumber { get; init; }
}

internal sealed record DiscogsFormat
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("descriptions")]
    public IReadOnlyList<string>? Descriptions { get; init; }
}

internal sealed record DiscogsIdentifier
{
    /// <summary>"Barcode", "Matrix / Runout", "Rights Society" and so on.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }
}

internal sealed record DiscogsImage
{
    /// <summary>"primary" for the front cover, "secondary" for everything else.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    [JsonPropertyName("uri150")]
    public string? Thumbnail { get; init; }
}

internal sealed record DiscogsTracklistEntry
{
    /// <summary>"track", or "heading" and "index" for the structure around them.</summary>
    [JsonPropertyName("type_")]
    public string? Type { get; init; }

    [JsonPropertyName("position")]
    public string? Position { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>"3:22", or "1:02:33" for a side-long piece. Frequently empty.</summary>
    [JsonPropertyName("duration")]
    public string? Duration { get; init; }
}
