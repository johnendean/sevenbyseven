using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Gigs;

/// <summary>What the Collection is told about Plays, answered from the Gigs I have recorded.</summary>
internal sealed class PlayHistory(SevenBySevenDbContext database, IOptions<GigsOptions> options) : IPlayHistory
{
    public Task<bool> HasBeenPlayedAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        database.Set<Play>().AnyAsync(play => play.CopyId == copyId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, PlayStanding>> StandingsAsync(
        IReadOnlyCollection<Guid> copyIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(copyIds);

        if (copyIds.Count == 0)
        {
            return new Dictionary<Guid, PlayStanding>();
        }

        var records = await database.Set<Copy>()
            .AsNoTracking()
            .Where(copy => copyIds.Contains(copy.Id))
            .Select(copy => new { copy.Id, copy.ReleaseId, copy.Release!.DiscogsMasterId })
            .ToDictionaryAsync(
                copy => copy.Id,
                copy => SameRecord.Of(copy.DiscogsMasterId, copy.ReleaseId),
                cancellationToken);

        var ledger = await PlayLedger.ReadAsync(database, options.Value.RepeatWindow, cancellationToken);

        return copyIds.Distinct().ToDictionary(
            id => id,
            id => records.TryGetValue(id, out var record) ? ledger.StandingOf(record) : PlayStanding.NeverPlayed);
    }
}
