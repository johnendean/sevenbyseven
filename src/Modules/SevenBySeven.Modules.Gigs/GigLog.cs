using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs;

internal sealed class GigLog(SevenBySevenDbContext database, IOptions<GigsOptions> options) : IGigLog
{
    public async Task<IReadOnlyList<GigSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        await database.Set<Gig>()
            .AsNoTracking()
            .OrderByDescending(gig => gig.PlayedOn)
            .ThenByDescending(gig => gig.Id)
            .Select(gig => new GigSummary(
                gig.Id,
                gig.PlayedOn,
                gig.Venue,
                gig.Selections.Count,
                gig.Selections.Sum(selection => selection.Plays.Count)))
            .ToListAsync(cancellationToken);

    public async Task<GigSheet?> FindAsync(Guid gigId, CancellationToken cancellationToken = default)
    {
        var gig = await database.Set<Gig>()
            .AsNoTracking()
            .Include(candidate => candidate.Selections)
                .ThenInclude(selection => selection.Plays)
                    .ThenInclude(play => play.Copy!.Release)
            .AsSplitQuery()
            .FirstOrDefaultAsync(candidate => candidate.Id == gigId, cancellationToken);

        if (gig is null)
        {
            return null;
        }

        var ledger = await Ledger(cancellationToken);

        return new GigSheet(
            gig.Id,
            gig.PlayedOn,
            gig.Venue,
            gig.Notes,
            [.. gig.Selections.Select(selection => new SelectionSheet(
                selection.Id,
                selection.Sequence + 1,
                [.. selection.Plays.Select(play =>
                {
                    var copy = play.Copy!;
                    var record = SameRecord.Of(copy.Release!.DiscogsMasterId, copy.ReleaseId);

                    return new PlayLine(
                        play.Id,
                        play.Sequence + 1,
                        copy.Id,
                        copy.Release,
                        copy.IsFormer,
                        ledger.RepeatAt(record, gig.Id),
                        ledger.PlayedEarlierAt(record, gig.Id, selection.Sequence, play.Sequence));
                })]))]);
    }

    public async Task<IReadOnlyList<Candidate>> CandidatesAsync(
        Guid gigId,
        CancellationToken cancellationToken = default)
    {
        var copies = await database.Set<Copy>()
            .AsNoTracking()
            .Include(copy => copy.Release)
            .Where(copy => copy.PartedWithOn == null)
            .ToListAsync(cancellationToken);

        var ledger = await Ledger(cancellationToken);

        return
        [
            .. copies
                .OrderBy(copy => copy.Release!.ArtistName)
                .ThenBy(copy => copy.Release!.Title)
                .Select(copy =>
                {
                    var record = SameRecord.Of(copy.Release!.DiscogsMasterId, copy.ReleaseId);

                    return new Candidate(copy, ledger.RepeatAt(record, gigId), ledger.PlayedAt(record, gigId));
                }),
        ];
    }

    public async Task<Gig> CreateAsync(GigDetails details, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(details);

        var gig = new Gig
        {
            PlayedOn = details.PlayedOn,
            Venue = Trimmed(details.Venue),
            Notes = Trimmed(details.Notes),
        };

        // A Gig is for playing records at, so it starts with somewhere to put them.
        gig.AddSelection();

        database.Add(gig);
        await SaveAsync(cancellationToken);

        return gig;
    }

    public Task<bool> UpdateAsync(Guid gigId, GigDetails details, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(details);

        return Changing(gigId, gig =>
        {
            gig.PlayedOn = details.PlayedOn;
            gig.Venue = Trimmed(details.Venue);
            gig.Notes = Trimmed(details.Notes);

            return true;
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid gigId, CancellationToken cancellationToken = default)
    {
        var gig = await Tracked(gigId, cancellationToken);

        if (gig is null)
        {
            return false;
        }

        database.Remove(gig);
        await SaveAsync(cancellationToken);

        return true;
    }

    public Task<bool> AddSelectionAsync(Guid gigId, CancellationToken cancellationToken = default) =>
        Changing(gigId, gig =>
        {
            gig.AddSelection();

            return true;
        }, cancellationToken);

    public Task<bool> RemoveSelectionAsync(
        Guid gigId,
        Guid selectionId,
        CancellationToken cancellationToken = default) =>
        Changing(gigId, gig => gig.RemoveSelection(selectionId), cancellationToken);

    public async Task<bool> AddPlayAsync(
        Guid gigId,
        Guid selectionId,
        Guid copyId,
        CancellationToken cancellationToken = default)
    {
        var owned = await database.Set<Copy>()
            .AnyAsync(copy => copy.Id == copyId && copy.PartedWithOn == null, cancellationToken);

        return owned && await Changing(gigId, gig => gig.AddPlay(selectionId, copyId) is not null, cancellationToken);
    }

    public Task<bool> RemovePlayAsync(Guid gigId, Guid playId, CancellationToken cancellationToken = default) =>
        Changing(gigId, gig => gig.RemovePlay(playId), cancellationToken);

    public Task<bool> MovePlayAsync(
        Guid gigId,
        Guid playId,
        bool earlier,
        CancellationToken cancellationToken = default) =>
        Changing(gigId, gig => gig.MovePlay(playId, earlier), cancellationToken);

    /// <summary>
    /// Loads a Gig to change it, applies the change, and saves only if the change took.
    /// </summary>
    private async Task<bool> Changing(Guid gigId, Func<Gig, bool> change, CancellationToken cancellationToken)
    {
        var gig = await Tracked(gigId, cancellationToken);

        if (gig is null || !change(gig))
        {
            LetGo();

            return false;
        }

        await SaveAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Saves, then lets go of every Gig, Selection and Play whether or not the save took.
    /// The context lives as long as the circuit: a failed write left tracked would be
    /// written by the next unrelated save, and a Gig left tracked would be handed back
    /// stale the next time it is loaded to change, whatever another tab has done since.
    /// </summary>
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            LetGo();
        }
    }

    private void LetGo()
    {
        var mine = database.ChangeTracker.Entries()
            .Where(entry => entry.Entity is Gig or Selection or Play)
            .ToList();

        foreach (var entry in mine)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>Tracked, unlike every read above: this one is going to be written back.</summary>
    private Task<Gig?> Tracked(Guid gigId, CancellationToken cancellationToken) =>
        database.Set<Gig>()
            .Include(gig => gig.Selections)
                .ThenInclude(selection => selection.Plays)
            .AsSplitQuery()
            .FirstOrDefaultAsync(gig => gig.Id == gigId, cancellationToken);

    private Task<PlayLedger> Ledger(CancellationToken cancellationToken) =>
        PlayLedger.ReadAsync(database, options.Value.RepeatWindow, cancellationToken);

    private static string? Trimmed(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
