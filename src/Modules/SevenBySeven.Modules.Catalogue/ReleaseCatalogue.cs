using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Modules.Catalogue;

internal sealed class ReleaseCatalogue(
    SevenBySevenDbContext database,
    ICatalogueSource source,
    ILogger<ReleaseCatalogue> logger) : IReleaseCatalogue
{
    public async Task<Release?> EnsureAsync(
        int discogsReleaseId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(discogsReleaseId);

        if (await FindAsync(discogsReleaseId, cancellationToken) is { } held)
        {
            return held;
        }

        var fetched = await source.FetchAsync(discogsReleaseId, cancellationToken);

        if (fetched is null)
        {
            logger.LogWarning(
                "Release {DiscogsReleaseId} could not be fetched, so nothing was cached.",
                discogsReleaseId);

            return null;
        }

        database.Add(fetched);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Two confirmations of the same pressing at once — an impatient double-click
            // is enough. The unique index on DiscogsReleaseId is the arbiter, and the
            // loser simply reads the row the winner wrote.
            Detach(fetched);

            var raced = await FindAsync(discogsReleaseId, cancellationToken);

            if (raced is null)
            {
                throw;
            }

            logger.LogInformation(
                ex, "Release {DiscogsReleaseId} was cached by another request first.", discogsReleaseId);

            return raced;
        }

        logger.LogInformation(
            "Cached release {DiscogsReleaseId} with {TrackCount} tracks.",
            discogsReleaseId,
            fetched.Tracks.Count);

        return fetched;
    }

    private Task<Release?> FindAsync(int discogsReleaseId, CancellationToken cancellationToken) =>
        database.Set<Release>()
            .Include(release => release.Tracks.OrderBy(track => track.Sequence))
            .FirstOrDefaultAsync(release => release.DiscogsReleaseId == discogsReleaseId, cancellationToken);

    /// <summary>
    /// Lets go of a Release that failed to save, so the circuit's context does not keep
    /// trying to insert it on the next unrelated save.
    /// </summary>
    private void Detach(Release release)
    {
        foreach (var track in release.Tracks)
        {
            database.Entry(track).State = EntityState.Detached;
        }

        database.Entry(release).State = EntityState.Detached;
    }
}
