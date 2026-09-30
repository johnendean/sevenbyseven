using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Tests.Gigs;

public class PlayHistoryTests
{
    [Fact]
    public async Task A_copy_has_been_played_once_it_is_in_a_gig()
    {
        await using var database = await TestDatabase.CreateAsync();
        var played = await Shelf.CopyOfAsync(database, 1);
        var unplayed = await Shelf.CopyOfAsync(database, 2);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 9, 26) });
        await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, played);

        var history = TestDatabase.PlayHistory(context);

        Assert.True(await history.HasBeenPlayedAsync(played));
        Assert.False(await history.HasBeenPlayedAsync(unplayed));
    }

    [Fact]
    public async Task A_copy_stands_by_every_pressing_of_its_master()
    {
        await using var database = await TestDatabase.CreateAsync();
        var original = await Shelf.CopyOfAsync(database, 1, discogsMasterId: 5460);
        var reissue = await Shelf.CopyOfAsync(database, 2, discogsMasterId: 5460);
        var unrelated = await Shelf.CopyOfAsync(database, 3, discogsMasterId: 99);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 9, 26), Venue = "The Social" });
        await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, original);
        await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, original);

        var unknown = Guid.CreateVersion7();
        var standings = await TestDatabase.PlayHistory(context)
            .StandingsAsync([reissue, unrelated, unknown]);

        Assert.Equal(
            new PlayStanding(2, new LastPlayed(gig.Id, new DateOnly(2026, 9, 26), "The Social"), Repeat.LastGig),
            standings[reissue]);
        Assert.Same(PlayStanding.NeverPlayed, standings[unrelated]);
        Assert.Same(PlayStanding.NeverPlayed, standings[unknown]);
    }

    [Fact]
    public async Task Asking_about_no_copies_is_answered_without_the_database()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.Empty(await TestDatabase.PlayHistory(context).StandingsAsync([]));
    }

    [Fact]
    public async Task The_database_refuses_to_lose_a_played_copy()
    {
        // Removing a Copy through the Collection keeps a played one (docs/adr/0006). This is
        // the foreign key that stops anything that forgets from quietly breaking a Gig.
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);

        await using (var context = database.NewContext())
        {
            var log = Shelf.Log(context);
            var gig = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 9, 26) });
            await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, copy);
        }

        await using var later = database.NewContext();
        later.Remove(await later.Set<Copy>().SingleAsync());

        await Assert.ThrowsAsync<DbUpdateException>(() => later.SaveChangesAsync());
    }
}
