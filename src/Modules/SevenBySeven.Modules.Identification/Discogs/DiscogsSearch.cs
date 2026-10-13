using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// What a search came back with. A search that could not be made — no token, a network
/// failure, a refusal — has no candidates either, but it says nothing about what Discogs
/// holds, so it is kept apart from a search that ran and matched nothing.
/// </summary>
public sealed record DiscogsSearch(IReadOnlyList<MatchCandidate> Candidates, bool Failed)
{
    public static DiscogsSearch Found(IReadOnlyList<MatchCandidate> candidates) => new(candidates, false);

    public static DiscogsSearch Failure { get; } = new([], true);

    /// <summary>Discogs was asked and answered with nothing.</summary>
    public bool MatchedNothing => !Failed && Candidates.Count == 0;
}
