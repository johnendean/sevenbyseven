using Microsoft.EntityFrameworkCore;
using SevenBySeven.Modules.Catalogue.Domain;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Tests.Collection;

public class VinylCollectionTests
{
    [Fact]
    public async Task Adding_a_copy_records_it_against_the_pressing()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var copy = await new VinylCollection(context).AddAsync(
            release,
            new CopyDetails
            {
                MediaCondition = ConditionGrade.VeryGoodPlus,
                SleeveCondition = ConditionGrade.VeryGood,
                PricePaid = 24.50m,
                PricePaidCurrency = "gbp",
                PurchasedFrom = "Sounds of the Universe",
                Location = "Front room, left shelf",
                Notes = "Slight warp, plays clean.",
            });

        await using var later = database.NewContext();
        var held = await later.Set<Copy>().SingleAsync();

        Assert.Equal(copy.Id, held.Id);
        Assert.Equal(release, held.ReleaseId);
        Assert.Equal(ConditionGrade.VeryGoodPlus, held.MediaCondition);
        Assert.Equal(ConditionGrade.VeryGood, held.SleeveCondition);
        Assert.Equal(24.50m, held.PricePaid);
        Assert.Equal("GBP", held.PricePaidCurrency);
        Assert.Equal("Sounds of the Universe", held.PurchasedFrom);
    }

    [Fact]
    public async Task What_I_know_about_a_copy_can_be_filled_in_afterwards()
    {
        // A Stack adds records without stopping to ask. Without this, condition and
        // price — the facts no lookup can ever supply — would be lost for good.
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);
        var copy = await collection.AddAsync(release, CopyDetails.Unknown);

        var updated = await collection.UpdateAsync(copy.Id, new CopyDetails
        {
            MediaCondition = ConditionGrade.NearMint,
            PricePaid = 12m,
            PricePaidCurrency = "gbp",
            Location = "Shelf A",
        });

        await using var later = database.NewContext();
        var held = await later.Set<Copy>().SingleAsync();

        Assert.True(updated);
        Assert.Equal(ConditionGrade.NearMint, held.MediaCondition);
        Assert.Equal(12m, held.PricePaid);
        Assert.Equal("GBP", held.PricePaidCurrency);
        Assert.Equal("Shelf A", held.Location);
    }

    [Fact]
    public async Task Clearing_the_price_takes_the_currency_with_it()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);
        var copy = await collection.AddAsync(
            release, new CopyDetails { PricePaid = 30m, PricePaidCurrency = "GBP" });

        await collection.UpdateAsync(copy.Id, new CopyDetails { Location = "Loft" });

        await using var later = database.NewContext();
        var held = await later.Set<Copy>().SingleAsync();

        Assert.Null(held.PricePaid);
        Assert.Null(held.PricePaidCurrency);
        Assert.Equal("Loft", held.Location);
    }

    [Fact]
    public async Task Updating_a_copy_that_is_not_mine_changes_nothing()
    {
        await using var database = await TestDatabase.CreateAsync();

        await using var context = database.NewContext();

        Assert.False(await new VinylCollection(context)
            .UpdateAsync(Guid.CreateVersion7(), CopyDetails.Unknown));
    }

    [Fact]
    public async Task An_update_needs_details_to_apply()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new VinylCollection(context).UpdateAsync(Guid.CreateVersion7(), null!));
    }

    [Fact]
    public async Task Two_copies_of_one_pressing_are_two_records()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);

        var first = await collection.AddAsync(release, CopyDetails.Unknown);
        var second = await collection.AddAsync(release, CopyDetails.Unknown);

        Assert.NotEqual(first.Id, second.Id);

        await using var later = database.NewContext();
        Assert.Equal(2, await later.Set<Copy>().CountAsync());
    }

    [Fact]
    public async Task A_currency_with_no_price_against_it_is_not_kept()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var copy = await new VinylCollection(context).AddAsync(
            release, new CopyDetails { PricePaidCurrency = "GBP" });

        Assert.Null(copy.PricePaid);
        Assert.Null(copy.PricePaidCurrency);
    }

    [Theory]
    [InlineData("gbp", "GBP")]
    [InlineData(" eur ", "EUR")]
    [InlineData("pounds", null)]
    [InlineData("£", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public async Task A_price_is_kept_in_a_currency_or_in_none(string? given, string? expected)
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var copy = await new VinylCollection(context).AddAsync(
            release, new CopyDetails { PricePaid = 12m, PricePaidCurrency = given });

        Assert.Equal(expected, copy.PricePaidCurrency);
    }

    [Fact]
    public async Task Details_left_blank_are_held_as_nothing_rather_than_as_whitespace()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var copy = await new VinylCollection(context).AddAsync(
            release, new CopyDetails { PurchasedFrom = "   ", Location = "", Notes = " \t " });

        Assert.Null(copy.PurchasedFrom);
        Assert.Null(copy.Location);
        Assert.Null(copy.Notes);
    }

    [Fact]
    public async Task The_collection_lists_the_most_recently_added_first()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);

        var first = await collection.AddAsync(release, new CopyDetails { Notes = "first" });

        // AddedOn is stamped from the clock, so the two have to land in different instants
        // for "most recent first" to mean anything at all.
        await Task.Delay(20);

        var second = await collection.AddAsync(release, new CopyDetails { Notes = "second" });

        var listed = await collection.ListAsync();

        Assert.Equal([second.Id, first.Id], listed.Select(copy => copy.Id));
        Assert.All(listed, copy => Assert.NotNull(copy.Release));
    }

    [Fact]
    public async Task A_copy_is_found_with_its_pressing_and_the_tracks_in_running_order()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database, "So What", "Freddie Freeloader", "Blue In Green");

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);
        var added = await collection.AddAsync(release, CopyDetails.Unknown);

        var found = await collection.FindAsync(added.Id);

        Assert.NotNull(found);
        Assert.Equal("Miles Davis", found.Release?.ArtistName);
        Assert.Equal(
            ["So What", "Freddie Freeloader", "Blue In Green"],
            found.Release!.Tracks.Select(track => track.Title));
    }

    [Fact]
    public async Task Removing_a_copy_leaves_the_pressing_in_the_catalogue()
    {
        await using var database = await TestDatabase.CreateAsync();
        var release = await HeldRelease(database);

        await using var context = database.NewContext();
        var collection = new VinylCollection(context);
        var added = await collection.AddAsync(release, CopyDetails.Unknown);

        Assert.True(await collection.RemoveAsync(added.Id));

        await using var later = database.NewContext();
        Assert.Equal(0, await later.Set<Copy>().CountAsync());
        Assert.Equal(1, await later.Set<Release>().CountAsync());
    }

    [Fact]
    public async Task Removing_a_copy_that_is_not_mine_says_so()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        Assert.False(await new VinylCollection(context).RemoveAsync(Guid.CreateVersion7()));
    }

    [Fact]
    public async Task A_copy_of_a_pressing_that_is_not_held_is_refused()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.NewContext();

        // A Copy without its Release is meaningless, and the database is what says so.
        await Assert.ThrowsAsync<DbUpdateException>(
            () => new VinylCollection(context).AddAsync(Guid.CreateVersion7(), CopyDetails.Unknown));
    }

    private static async Task<Guid> HeldRelease(TestDatabase database, params string[] trackTitles)
    {
        await using var context = database.NewContext();

        var release = TestDatabase.AnyRelease(trackTitles: trackTitles);
        context.Add(release);
        await context.SaveChangesAsync();

        return release.Id;
    }
}
