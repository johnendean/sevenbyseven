using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Modules.Collection;

/// <summary>
/// What the Collection needs to know about Plays, which it does not hold. The Gigs module
/// records them and supplies this; it is declared here rather than referenced from there
/// because Gigs already depends on the Collection — every Play is of a Copy.
/// </summary>
public interface IPlayHistory
{
    /// <summary>
    /// Whether this Copy has been played at any Gig, which is what decides whether removing
    /// it deletes it or keeps it as a Former Copy (docs/adr/0006).
    /// </summary>
    Task<bool> HasBeenPlayedAsync(Guid copyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// How each of these Copies stands as things are now: how often the record has been
    /// played, where last, and whether it is a Repeat. Judged across every Copy of the same
    /// Master, not just the one asked about. Every id asked about is answered.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PlayStanding>> StandingsAsync(
        IReadOnlyCollection<Guid> copyIds,
        CancellationToken cancellationToken = default);
}
