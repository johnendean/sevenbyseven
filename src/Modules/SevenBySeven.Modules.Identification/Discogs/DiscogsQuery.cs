namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// A search against the Discogs release database. Fields left null are omitted, so the
/// same type expresses a precise barcode lookup and a vague artist-and-title guess.
/// </summary>
public sealed record DiscogsQuery
{
    public string? Barcode { get; init; }
    public string? CatalogueNumber { get; init; }
    public string? LabelName { get; init; }
    public string? ArtistName { get; init; }
    public string? Title { get; init; }
    public int? Year { get; init; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Barcode)
        && string.IsNullOrWhiteSpace(CatalogueNumber)
        && string.IsNullOrWhiteSpace(ArtistName)
        && string.IsNullOrWhiteSpace(Title);
}
