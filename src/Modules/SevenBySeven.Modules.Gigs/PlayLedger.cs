using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs;

/// <summary>
/// Every Play I have recorded, arranged to answer the two questions asked of it: how does
/// a record stand now, and would it have been a Repeat at a given Gig. Gigs are ordered by
/// the date they were played rather than when they were entered, because a Gig may be
/// entered long after the fact and still has to fall in its proper place.
/// </summary>
internal sealed class PlayLedger
{
    private readonly IReadOnlyList<GigFact> _gigs;
    private readonly ILookup<SameRecord, PlayFact> _playsByRecord;
    private readonly int _window;

    public PlayLedger(IEnumerable<GigFact> gigs, IEnumerable<PlayFact> plays, int window)
    {
        _gigs = [.. gigs.OrderBy(gig => gig.PlayedOn).ThenBy(gig => gig.Id)];
        _playsByRecord = plays.ToLookup(play => play.Record);
        _window = Math.Max(1, window);
    }

    /// <summary>
    /// Reads the whole history. A personal record of Gigs runs to thousands of Plays at
    /// most, so holding it in memory is cheaper than asking the database the same question
    /// once per record on a page.
    /// </summary>
    public static async Task<PlayLedger> ReadAsync(
        SevenBySevenDbContext database,
        int window,
        CancellationToken cancellationToken)
    {
        var gigs = await database.Set<Gig>()
            .AsNoTracking()
            .Select(gig => new GigFact(gig.Id, gig.PlayedOn, gig.Venue))
            .ToListAsync(cancellationToken);

        var plays = await (
                from play in database.Set<Play>().AsNoTracking()
                join selection in database.Set<Selection>() on play.SelectionId equals selection.Id
                select new
                {
                    selection.GigId,
                    SelectionSequence = selection.Sequence,
                    PlaySequence = play.Sequence,
                    play.CopyId,
                    play.Copy!.ReleaseId,
                    play.Copy.Release!.DiscogsMasterId,
                })
            .ToListAsync(cancellationToken);

        return new PlayLedger(
            gigs,
            plays.Select(play => new PlayFact(
                play.GigId,
                play.SelectionSequence,
                play.PlaySequence,
                play.CopyId,
                SameRecord.Of(play.DiscogsMasterId, play.ReleaseId))),
            window);
    }

    /// <summary>How a record stands as things are now, judged against my most recent Gigs.</summary>
    public PlayStanding StandingOf(SameRecord record)
    {
        var plays = _playsByRecord[record];
        var played = plays.Select(play => play.GigId).ToHashSet();

        if (played.Count == 0)
        {
            return PlayStanding.NeverPlayed;
        }

        var last = _gigs.Last(gig => played.Contains(gig.Id));

        return new PlayStanding(
            plays.Count(),
            new LastPlayed(last.Id, last.PlayedOn, last.Venue),
            Judge(record, _gigs));
    }

    /// <summary>
    /// Whether playing a record at this Gig was a Repeat of the Gigs before it. Only the
    /// Gigs before it count, so going back to enter an old Gig judges it as it stood then.
    /// A Gig the ledger does not know is judged as the next one to be played.
    /// </summary>
    public Repeat RepeatAt(SameRecord record, Guid gigId)
    {
        var position = IndexOf(gigId);

        return position < 0 ? Judge(record, _gigs) : Judge(record, _gigs.Take(position).ToList());
    }

    /// <summary>
    /// Whether this record was already played earlier in the same Gig, before the given
    /// point in its running order. Allowed, but more likely a slip than a choice.
    /// </summary>
    public bool PlayedEarlierAt(SameRecord record, Guid gigId, int selectionSequence, int playSequence) =>
        _playsByRecord[record].Any(play => play.GigId == gigId
            && (play.SelectionSequence, play.PlaySequence).CompareTo((selectionSequence, playSequence)) < 0);

    /// <summary>Whether this record has been played anywhere in this Gig.</summary>
    public bool PlayedAt(SameRecord record, Guid gigId) =>
        _playsByRecord[record].Any(play => play.GigId == gigId);

    private Repeat Judge(SameRecord record, IReadOnlyList<GigFact> before)
    {
        var recent = before.TakeLast(_window).ToList();

        if (recent.Count == 0)
        {
            return Repeat.None;
        }

        var played = _playsByRecord[record].Select(play => play.GigId).ToHashSet();

        if (played.Contains(recent[^1].Id))
        {
            return Repeat.LastGig;
        }

        return recent.Any(gig => played.Contains(gig.Id)) ? Repeat.Recent : Repeat.None;
    }

    private int IndexOf(Guid gigId)
    {
        for (var index = 0; index < _gigs.Count; index++)
        {
            if (_gigs[index].Id == gigId)
            {
                return index;
            }
        }

        return -1;
    }
}

internal sealed record GigFact(Guid Id, DateOnly PlayedOn, string? Venue);

internal sealed record PlayFact(
    Guid GigId,
    int SelectionSequence,
    int PlaySequence,
    Guid CopyId,
    SameRecord Record);
