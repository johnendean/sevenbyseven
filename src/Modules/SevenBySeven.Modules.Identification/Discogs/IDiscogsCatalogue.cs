namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// Searches the Discogs release database. The single catalogue source — see
/// docs/adr/0001 — kept behind an interface so a second source stays possible.
/// </summary>
public interface IDiscogsCatalogue
{
    /// <summary>
    /// Returns candidates best-match-first. A search that could not be made — an
    /// unconfigured token, a network failure, an error status — is
    /// <see cref="DiscogsSearch.Failure"/> rather than an exception, so identification
    /// degrades instead of stopping.
    /// </summary>
    Task<DiscogsSearch> SearchAsync(DiscogsQuery query, CancellationToken cancellationToken = default);
}
