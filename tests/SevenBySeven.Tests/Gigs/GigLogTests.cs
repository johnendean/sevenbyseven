using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Tests.Gigs;

public class GigLogTests
{
    private static readonly DateOnly September26 = new(2026, 9, 26);

    [Fact]
    public async Task A_new_gig_starts_with_a_selection_to_fill()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        var gig = await Shelf.Log(context).CreateAsync(
            new GigDetails { PlayedOn = September26, Venue = "  The Social ", Notes = "   " });

        await using var later = database.NewContext();
        var sheet = await Shelf.Log(later).FindAsync(gig.Id);

        Assert.NotNull(sheet);
        Assert.Equal(September26, sheet.PlayedOn);
        Assert.Equal("The Social", sheet.Venue);
        Assert.Null(sheet.Notes);
        Assert.Equal(1, Assert.Single(sheet.Selections).Number);
    }

    [Fact]
    public async Task Gigs_are_listed_most_recently_played_first_with_what_was_played()
    {
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var older = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 8, 1) });
        var newer = await log.CreateAsync(new GigDetails { PlayedOn = September26, Venue = "The Social" });
        await log.AddSelectionAsync(newer.Id);
        await log.AddPlayAsync(newer.Id, newer.Selections[0].Id, copy);

        var listed = await Shelf.Log(database.NewContext()).ListAsync();

        Assert.Equal([newer.Id, older.Id], listed.Select(gig => gig.Id));
        Assert.Equal(new GigSummary(newer.Id, September26, "The Social", 2, 1), listed[0]);
    }

    [Fact]
    public async Task A_gig_reads_back_in_running_order()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = await Shelf.CopyOfAsync(database, 1, title: "First");
        var second = await Shelf.CopyOfAsync(database, 2, title: "Second");
        var third = await Shelf.CopyOfAsync(database, 3, title: "Third");

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        var opening = gig.Selections[0].Id;
        await log.AddPlayAsync(gig.Id, opening, first);
        await log.AddPlayAsync(gig.Id, opening, second);
        await log.AddSelectionAsync(gig.Id);
        var closing = (await log.FindAsync(gig.Id))!.Selections[1].Id;
        await log.AddPlayAsync(gig.Id, closing, third);

        await using var later = database.NewContext();
        var sheet = (await Shelf.Log(later).FindAsync(gig.Id))!;

        Assert.Equal([1, 2], sheet.Selections.Select(selection => selection.Number));
        Assert.Equal(["First", "Second"], sheet.Selections[0].Plays.Select(play => play.Release.Title));
        Assert.Equal([1, 2], sheet.Selections[0].Plays.Select(play => play.Number));
        Assert.Equal(["Third"], sheet.Selections[1].Plays.Select(play => play.Release.Title));
        Assert.Equal(3, sheet.Plays);
    }

    [Fact]
    public async Task Moving_and_removing_plays_is_kept()
    {
        await using var database = await TestDatabase.CreateAsync();
        var first = await Shelf.CopyOfAsync(database, 1, title: "First");
        var second = await Shelf.CopyOfAsync(database, 2, title: "Second");
        var third = await Shelf.CopyOfAsync(database, 3, title: "Third");

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        var selection = gig.Selections[0].Id;
        await log.AddPlayAsync(gig.Id, selection, first);
        await log.AddPlayAsync(gig.Id, selection, second);
        await log.AddPlayAsync(gig.Id, selection, third);
        var plays = (await log.FindAsync(gig.Id))!.Selections[0].Plays;

        Assert.True(await log.MovePlayAsync(gig.Id, plays[2].Id, earlier: true));
        Assert.True(await log.RemovePlayAsync(gig.Id, plays[0].Id));
        Assert.False(await log.MovePlayAsync(gig.Id, plays[1].Id, earlier: false));

        await using var later = database.NewContext();
        var sheet = (await Shelf.Log(later).FindAsync(gig.Id))!;

        Assert.Equal(["Third", "Second"], sheet.Selections[0].Plays.Select(play => play.Release.Title));
    }

    [Fact]
    public async Task Removing_a_selection_takes_its_plays_and_closes_up_the_gig()
    {
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, copy);
        await log.AddSelectionAsync(gig.Id);
        var kept = (await log.FindAsync(gig.Id))!.Selections[1].Id;

        Assert.True(await log.RemoveSelectionAsync(gig.Id, gig.Selections[0].Id));

        await using var later = database.NewContext();
        var sheet = (await Shelf.Log(later).FindAsync(gig.Id))!;

        Assert.Equal(kept, Assert.Single(sheet.Selections).Id);
        Assert.Equal(1, sheet.Selections[0].Number);
        Assert.Equal(0, sheet.Plays);
    }

    [Fact]
    public async Task A_gig_is_flagged_against_the_gigs_before_it_across_pressings()
    {
        await using var database = await TestDatabase.CreateAsync();
        var original = await Shelf.CopyOfAsync(database, 1, discogsMasterId: 5460);
        var reissue = await Shelf.CopyOfAsync(database, 2, discogsMasterId: 5460);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var lastWeek = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 9, 19) });
        var tonight = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        await log.AddPlayAsync(lastWeek.Id, lastWeek.Selections[0].Id, original);
        await log.AddPlayAsync(tonight.Id, tonight.Selections[0].Id, reissue);

        var then = (await log.FindAsync(lastWeek.Id))!.Selections[0].Plays.Single();
        var now = (await log.FindAsync(tonight.Id))!.Selections[0].Plays.Single();

        Assert.Equal(Repeat.None, then.Repeat);
        Assert.Equal(Repeat.LastGig, now.Repeat);
    }

    [Fact]
    public async Task The_same_record_twice_in_a_gig_is_flagged_the_second_time()
    {
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);
        var other = await Shelf.CopyOfAsync(database, 2, title: "Other");

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        var selection = gig.Selections[0].Id;
        await log.AddPlayAsync(gig.Id, selection, copy);
        await log.AddPlayAsync(gig.Id, selection, other);
        await log.AddPlayAsync(gig.Id, selection, copy);

        var plays = (await log.FindAsync(gig.Id))!.Selections[0].Plays;

        Assert.Equal([false, false, true], plays.Select(play => play.PlayedEarlierThisGig));
    }

    [Fact]
    public async Task Candidates_are_my_records_flagged_for_this_gig()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repeated = await Shelf.CopyOfAsync(database, 1, artist: "B", title: "Repeated");
        var fresh = await Shelf.CopyOfAsync(database, 2, artist: "A", title: "Fresh");
        var alreadyTonight = await Shelf.CopyOfAsync(database, 3, artist: "C", title: "Tonight");

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var lastWeek = await log.CreateAsync(new GigDetails { PlayedOn = new DateOnly(2026, 9, 19) });
        var tonight = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        await log.AddPlayAsync(lastWeek.Id, lastWeek.Selections[0].Id, repeated);
        await log.AddPlayAsync(tonight.Id, tonight.Selections[0].Id, alreadyTonight);

        var candidates = await log.CandidatesAsync(tonight.Id);

        Assert.Equal([fresh, repeated, alreadyTonight], candidates.Select(candidate => candidate.Copy.Id));
        Assert.Equal([Repeat.None, Repeat.LastGig, Repeat.None], candidates.Select(candidate => candidate.Repeat));
        Assert.Equal([false, false, true], candidates.Select(candidate => candidate.PlayedThisGig));
    }

    [Fact]
    public async Task Only_a_record_in_my_collection_can_be_played()
    {
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        var selection = gig.Selections[0].Id;

        Assert.False(await log.AddPlayAsync(gig.Id, selection, Guid.CreateVersion7()));
        Assert.False(await log.AddPlayAsync(gig.Id, Guid.CreateVersion7(), copy));
        Assert.False(await log.AddPlayAsync(Guid.CreateVersion7(), selection, copy));
        Assert.Equal(0, (await log.FindAsync(gig.Id))!.Plays);
    }

    [Fact]
    public async Task A_gigs_details_can_be_corrected()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });

        Assert.True(await log.UpdateAsync(
            gig.Id,
            new GigDetails { PlayedOn = new DateOnly(2026, 9, 27), Venue = " Band on the Wall ", Notes = "Ran over" }));
        Assert.False(await log.UpdateAsync(Guid.CreateVersion7(), new GigDetails { PlayedOn = September26 }));

        await using var later = database.NewContext();
        var sheet = (await Shelf.Log(later).FindAsync(gig.Id))!;

        Assert.Equal(new DateOnly(2026, 9, 27), sheet.PlayedOn);
        Assert.Equal("Band on the Wall", sheet.Venue);
        Assert.Equal("Ran over", sheet.Notes);
    }

    [Fact]
    public async Task Deleting_a_gig_takes_what_was_played_at_it()
    {
        await using var database = await TestDatabase.CreateAsync();
        var copy = await Shelf.CopyOfAsync(database, 1);

        await using var context = database.NewContext();
        var log = Shelf.Log(context);
        var gig = await log.CreateAsync(new GigDetails { PlayedOn = September26 });
        await log.AddPlayAsync(gig.Id, gig.Selections[0].Id, copy);

        Assert.True(await log.DeleteAsync(gig.Id));
        Assert.False(await log.DeleteAsync(gig.Id));

        await using var later = database.NewContext();
        Assert.Null(await Shelf.Log(later).FindAsync(gig.Id));
        Assert.Equal(0, await later.Set<Play>().CountAsync());
        Assert.Equal(1, await later.Set<Copy>().CountAsync());
    }
}
