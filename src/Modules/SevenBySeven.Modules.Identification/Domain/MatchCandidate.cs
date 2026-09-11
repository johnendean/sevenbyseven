namespace SevenBySeven.Modules.Identification.Domain;

/// <summary>
/// One of the Releases put forward as the possible answer to a Scan. Never persisted:
/// candidates live only until one is confirmed or the Scan is abandoned.
/// </summary>
public sealed record MatchCandidate(
    int DiscogsReleaseId,
    string Title,
    string ArtistName,
    string? LabelName,
    string? CatalogueNumber,
    string? Country,
    int? Year,
    string? FormatDescription,
    string? ThumbnailUrl);
