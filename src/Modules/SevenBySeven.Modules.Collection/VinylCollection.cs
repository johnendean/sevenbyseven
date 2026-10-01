using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Collection;

internal sealed class VinylCollection(SevenBySevenDbContext database, IPlayHistory plays) : IVinylCollection
{
    public async Task<Copy> AddAsync(
        Guid releaseId,
        CopyDetails details,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(details);

        var copy = new Copy
        {
            ReleaseId = releaseId,
            MediaCondition = details.MediaCondition,
            SleeveCondition = details.SleeveCondition,
            PricePaid = details.PricePaid,
            // A currency with no price against it says nothing, so it is not kept.
            PricePaidCurrency = details.PricePaid is null ? null : Currency(details.PricePaidCurrency),
            PurchasedFrom = Trimmed(details.PurchasedFrom),
            Location = Trimmed(details.Location),
            Notes = Trimmed(details.Notes),
        };

        database.Add(copy);
        await database.SaveChangesAsync(cancellationToken);

        return copy;
    }

    public async Task<bool> UpdateAsync(
        Guid copyId,
        CopyDetails details,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(details);

        // Tracked, unlike every read on this class: this one is going to be written back.
        var copy = await Owned(database.Set<Copy>())
            .FirstOrDefaultAsync(candidate => candidate.Id == copyId, cancellationToken);

        if (copy is null)
        {
            return false;
        }

        copy.MediaCondition = details.MediaCondition;
        copy.SleeveCondition = details.SleeveCondition;
        copy.PricePaid = details.PricePaid;
        copy.PricePaidCurrency = details.PricePaid is null ? null : Currency(details.PricePaidCurrency);
        copy.PurchasedFrom = Trimmed(details.PurchasedFrom);
        copy.Location = Trimmed(details.Location);
        copy.Notes = Trimmed(details.Notes);

        await database.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<Copy>> ListAsync(CancellationToken cancellationToken = default) =>
        await Reading()
            .Include(copy => copy.Release)
            .OrderByDescending(copy => copy.AddedOn)
            // Two records catalogued in the same instant still need a settled order, and
            // a version 7 id carries the moment it was made.
            .ThenByDescending(copy => copy.Id)
            .ToListAsync(cancellationToken);

    public Task<Copy?> FindAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        Reading()
            .Include(copy => copy.Release!.Tracks.OrderBy(track => track.Sequence))
            .FirstOrDefaultAsync(copy => copy.Id == copyId, cancellationToken);

    public async Task<bool> RemoveAsync(Guid copyId, CancellationToken cancellationToken = default)
    {
        var copy = await Owned(database.Set<Copy>())
            .FirstOrDefaultAsync(candidate => candidate.Id == copyId, cancellationToken);

        if (copy is null)
        {
            return false;
        }

        // A played Copy is what past Gigs point at, so it is kept as a Former Copy. One
        // nobody ever played is most likely a wrong scan or a duplicate, and simply goes
        // (docs/adr/0006).
        if (await plays.HasBeenPlayedAsync(copyId, cancellationToken))
        {
            copy.PartWith(DateTimeOffset.UtcNow);
        }
        else
        {
            database.Remove(copy);
        }

        await database.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Reads are untracked: the context lives as long as the user's circuit, and nothing
    /// displayed on a page is going to be written back through it.
    /// </summary>
    private IQueryable<Copy> Reading() => Owned(database.Set<Copy>().AsNoTracking());

    /// <summary>The Collection is the Copies I own, so a Former Copy is never part of it.</summary>
    private static IQueryable<Copy> Owned(IQueryable<Copy> copies) =>
        copies.Where(copy => copy.PartedWithOn == null);

    /// <summary>ISO 4217 or nothing — three letters, or it is not a currency.</summary>
    private static string? Currency(string? code)
    {
        var trimmed = code?.Trim().ToUpperInvariant();

        return trimmed is { Length: 3 } && trimmed.All(char.IsAsciiLetterUpper) ? trimmed : null;
    }

    private static string? Trimmed(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
