namespace SevenBySeven.Modules.Catalogue.Contracts;

/// <summary>A Release in full, as the Catalogue holds it, Tracks in running order.</summary>
public sealed record ReleaseSheet(
    Guid Id,
    int DiscogsReleaseId,
    int? DiscogsMasterId,
    string Title,
    string ArtistName,
    string? LabelName,
    string? CatalogueNumber,
    string? Country,
    string? Released,
    string? FormatDescription,
    string? Barcode,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Styles,
    string Pressing,
    string? ThumbnailUrl,
    string? CoverImageUrl,
    IReadOnlyList<TrackLine> Tracks);

/// <summary>One Track of a Release, with its BPM and where that came from when it is known.</summary>
public sealed record TrackLine(
    Guid Id,
    string Position,
    string Title,
    TimeSpan? Duration,
    decimal? Bpm,
    BpmSource? BpmSource);
