using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs;
using SevenBySeven.Shared.Persistence;

namespace SevenBySeven.Tests.Gigs;

/// <summary>Records on the shelf to play at Gigs, and the gig log to play them in.</summary>
internal static class Shelf
{
    /// <summary>
    /// A new Copy of a pressing, holding the pressing first if it is not held already.
    /// Two pressings sharing a master are the same record to the gig log.
    /// </summary>
    public static async Task<Guid> CopyOfAsync(
        TestDatabase database,
        int discogsReleaseId,
        int? discogsMasterId = null,
        string artist = "Miles Davis",
        string title = "Kind Of Blue")
    {
        await using var context = database.NewContext();

        var release = await context.Set<Release>()
            .FirstOrDefaultAsync(held => held.DiscogsReleaseId == discogsReleaseId);

        if (release is null)
        {
            release = new Release
            {
                DiscogsReleaseId = discogsReleaseId,
                DiscogsMasterId = discogsMasterId,
                ArtistName = artist,
                Title = title,
            };
            context.Add(release);
        }

        var copy = new Copy { ReleaseId = release.Id };
        context.Add(copy);
        await context.SaveChangesAsync();

        return copy.Id;
    }

    public static GigLog Log(SevenBySevenDbContext context, int repeatWindow = GigsOptions.DefaultRepeatWindow) =>
        new(context, Options.Create(new GigsOptions { RepeatWindow = repeatWindow }));
}
