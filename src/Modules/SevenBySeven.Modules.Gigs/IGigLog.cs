using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Modules.Gigs;

/// <summary>
/// The Gigs I have played and what I played at each. Every change is made to one Gig at
/// a time, and each answers false when there is no such Gig, Selection or Play to change —
/// the page's cue that what it was showing has gone.
/// </summary>
public interface IGigLog
{
    /// <summary>Every Gig, most recently played first.</summary>
    Task<IReadOnlyList<GigSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>One Gig laid out as it was played, each Play flagged as it stood at the time.</summary>
    Task<GigSheet?> FindAsync(Guid gigId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The Copies in my Collection that could be added to this Gig, each flagged by
    /// whether it would be a Repeat of the Gigs before it or is already played in this one.
    /// </summary>
    Task<IReadOnlyList<Candidate>> CandidatesAsync(Guid gigId, CancellationToken cancellationToken = default);

    Task<Gig> CreateAsync(GigDetails details, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid gigId, GigDetails details, CancellationToken cancellationToken = default);

    /// <summary>Deletes a Gig and everything played at it.</summary>
    Task<bool> DeleteAsync(Guid gigId, CancellationToken cancellationToken = default);

    Task<bool> AddSelectionAsync(Guid gigId, CancellationToken cancellationToken = default);

    Task<bool> RemoveSelectionAsync(Guid gigId, Guid selectionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a Copy to the end of a Selection. False as well when the Copy is not in my
    /// Collection: a record I do not own, or no longer own, cannot be newly played.
    /// </summary>
    Task<bool> AddPlayAsync(Guid gigId, Guid selectionId, Guid copyId, CancellationToken cancellationToken = default);

    Task<bool> RemovePlayAsync(Guid gigId, Guid playId, CancellationToken cancellationToken = default);

    Task<bool> MovePlayAsync(Guid gigId, Guid playId, bool earlier, CancellationToken cancellationToken = default);
}
