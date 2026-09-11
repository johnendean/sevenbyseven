using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Catalogue;

/// <summary>
/// Where a Release comes from when the Catalogue does not already hold it. The
/// Catalogue owns this contract and whichever module holds the connection to the
/// external source implements it — today that is Identification's Discogs client.
/// Declaring the port here keeps the Catalogue ignorant of how Discogs is reached.
/// </summary>
public interface ICatalogueSource
{
    /// <summary>
    /// Fetches one pressing in full, tracklist included, as a detached Release ready to
    /// be stored. Null when the source has no such release or could not be reached —
    /// an unfetchable release is a reason to say so, not to throw.
    /// </summary>
    Task<Release?> FetchAsync(int discogsReleaseId, CancellationToken cancellationToken = default);
}
