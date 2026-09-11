using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// Searches the Discogs release database. The single catalogue source — see
/// docs/adr/0001 — kept behind an interface so a second source stays possible.
/// </summary>
public interface IDiscogsCatalogue
{
    /// <summary>
    /// Returns candidates best-match-first, or an empty list when nothing matched.
    /// An unconfigured token yields an empty list rather than an exception.
    /// </summary>
    Task<IReadOnlyList<MatchCandidate>> SearchAsync(DiscogsQuery query, CancellationToken cancellationToken = default);
}
