namespace SevenBySeven.Modules.Identification.Domain;

/// <summary>
/// What could be read off a photographed sleeve or centre label. Every field is
/// optional: a barcode may be absent, and extraction may simply fail to read a field.
/// </summary>
public sealed record SleeveDetails
{
    public string? Barcode { get; init; }
    public string? ArtistName { get; init; }
    public string? Title { get; init; }
    public string? LabelName { get; init; }
    public string? CatalogueNumber { get; init; }
    public int? Year { get; init; }

    /// <summary>True when nothing usable was read and there is nothing to search on.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Barcode)
        && string.IsNullOrWhiteSpace(ArtistName)
        && string.IsNullOrWhiteSpace(Title)
        && string.IsNullOrWhiteSpace(CatalogueNumber);
}
