using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Catalogue;

/// <summary>
/// The locally held Releases. Reading one is how the rest of the application gets at
/// a pressing: the Catalogue fetches and stores it the first time it is asked for,
/// and serves it from the database forever after (docs/adr/0001).
/// </summary>
public interface IReleaseCatalogue
{
    /// <summary>
    /// Returns the held Release with its Tracks, fetching and storing it if this is the
    /// first time it has been asked for. Null when the source cannot supply it, which
    /// is the caller's cue that there is nothing to confirm against.
    /// </summary>
    Task<Release?> EnsureAsync(int discogsReleaseId, CancellationToken cancellationToken = default);
}
