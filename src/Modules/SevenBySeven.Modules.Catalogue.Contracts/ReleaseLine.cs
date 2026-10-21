namespace SevenBySeven.Modules.Catalogue.Contracts;

/// <summary>
/// A Release as a line in a list of records: enough to say which pressing it is and to
/// find it by what is written on it, without its tracklist.
/// </summary>
public sealed record ReleaseLine(
    Guid Id,
    int DiscogsReleaseId,
    int? DiscogsMasterId,
    string Title,
    string ArtistName,
    string? LabelName,
    string? CatalogueNumber,
    string Pressing,
    string? ThumbnailUrl);
