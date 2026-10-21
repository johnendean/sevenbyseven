using SevenBySeven.Modules.Catalogue.Contracts;
using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Catalogue;

/// <summary>
/// A held Release as the API shows it. Every module that puts a Release in front of a
/// client goes through here, so the entity never leaves the process (docs/adr/0009).
/// </summary>
public static class ReleaseContracts
{
    public static ReleaseLine ToLine(this Release release)
    {
        ArgumentNullException.ThrowIfNull(release);

        return new ReleaseLine(
            release.Id,
            release.DiscogsReleaseId,
            release.DiscogsMasterId,
            release.Title,
            release.ArtistName,
            release.LabelName,
            release.CatalogueNumber,
            release.Pressing,
            release.ThumbnailUrl);
    }

    /// <summary>The Release with its Tracks, which must have been loaded with it.</summary>
    public static ReleaseSheet ToSheet(this Release release)
    {
        ArgumentNullException.ThrowIfNull(release);

        return new ReleaseSheet(
            release.Id,
            release.DiscogsReleaseId,
            release.DiscogsMasterId,
            release.Title,
            release.ArtistName,
            release.LabelName,
            release.CatalogueNumber,
            release.Country,
            release.Released?.ToString(),
            release.FormatDescription,
            release.Barcode,
            [.. release.Genres],
            [.. release.Styles],
            release.Pressing,
            release.ThumbnailUrl,
            release.CoverImageUrl,
            [.. release.Tracks.Select(track => new TrackLine(
                track.Id,
                track.Position,
                track.Title,
                track.Duration,
                track.Bpm,
                track.BpmSource))]);
    }
}
