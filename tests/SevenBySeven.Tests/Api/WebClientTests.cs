extern alias web;

using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Gigs.Contracts;
using web::SevenBySeven.Web.Catalogue;
using web::SevenBySeven.Web.Collection;
using web::SevenBySeven.Web.Gigs;

namespace SevenBySeven.Tests.Api;

/// <summary>
/// The web app's clients against the real API, in memory. What these prove is that the two
/// ends agree: on the routes, on how enums and dates are written, and on what a 404 means.
/// </summary>
public class WebClientTests
{
    private static readonly DateOnly September26 = new(2026, 9, 26);

    [Fact]
    public async Task A_release_is_read_through_the_catalogue()
    {
        await using var host = await ApiHost.StartAsync();
        host.Source.Unfetchable.Add(1);
        var catalogue = new CatalogueClient(host.CreateClient());

        Assert.Equal("Kind Of Blue", (await catalogue.EnsureAsync(249504))?.Title);
        Assert.Null(await catalogue.EnsureAsync(1));
    }

    [Fact]
    public async Task A_copy_is_added_changed_and_removed()
    {
        await using var host = await ApiHost.StartAsync();
        var collection = new CollectionClient(host.CreateClient());

        var added = await collection.AddAsync(
            new AddCopyRequest(249504, new CopyDetails { MediaCondition = ConditionGrade.VeryGoodPlus }));

        Assert.NotNull(added);
        Assert.Equal(ConditionGrade.VeryGoodPlus, added.Details.MediaCondition);
        Assert.Equal([added.Id], (await collection.ListAsync()).Select(copy => copy.Id));

        Assert.True(await collection.UpdateAsync(added.Id, new CopyDetails { Location = "Crate 3" }));
        Assert.Equal("Crate 3", (await collection.FindAsync(added.Id))?.Details.Location);

        Assert.True(await collection.RemoveAsync(added.Id));
        Assert.Null(await collection.FindAsync(added.Id));
        Assert.False(await collection.RemoveAsync(added.Id));
        Assert.False(await collection.UpdateAsync(added.Id, CopyDetails.Unknown));
    }

    [Fact]
    public async Task A_pressing_that_cannot_be_fetched_adds_nothing()
    {
        await using var host = await ApiHost.StartAsync();
        host.Source.Unfetchable.Add(249504);
        var collection = new CollectionClient(host.CreateClient());

        Assert.Null(await collection.AddAsync(new AddCopyRequest(249504, CopyDetails.Unknown)));
    }

    [Fact]
    public async Task A_refusal_the_form_should_have_caught_is_thrown_as_a_bug()
    {
        await using var host = await ApiHost.StartAsync();
        var collection = new CollectionClient(host.CreateClient());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            collection.AddAsync(new AddCopyRequest(249504, new CopyDetails { Location = new string('x', 201) })));
    }

    [Fact]
    public async Task A_gig_is_filled_in_and_taken_apart_again()
    {
        await using var host = await ApiHost.StartAsync();
        var http = host.CreateClient();
        var collection = new CollectionClient(http);
        var gigs = new GigsClient(http);
        var first = (await collection.AddAsync(new AddCopyRequest(249504, CopyDetails.Unknown)))!;
        var second = (await collection.AddAsync(new AddCopyRequest(1000, CopyDetails.Unknown)))!;

        var gig = await gigs.CreateAsync(new GigDetails { PlayedOn = September26, Venue = "The Social" });
        Assert.Equal([gig], await gigs.ListAsync());

        var selection = (await gigs.FindAsync(gig.Id))!.Selections[0].Id;
        Assert.True(await gigs.AddPlayAsync(gig.Id, selection, first.Id));
        Assert.True(await gigs.AddPlayAsync(gig.Id, selection, second.Id));
        Assert.All(await gigs.CandidatesAsync(gig.Id), candidate => Assert.True(candidate.PlayedThisGig));

        var plays = (await gigs.FindAsync(gig.Id))!.Selections[0].Plays;
        Assert.True(await gigs.MovePlayAsync(gig.Id, plays[1].Id, earlier: true));
        Assert.False(await gigs.MovePlayAsync(gig.Id, plays[1].Id, earlier: true));
        Assert.True(await gigs.RemovePlayAsync(gig.Id, plays[0].Id));

        Assert.True(await gigs.AddSelectionAsync(gig.Id));
        Assert.True(await gigs.RemoveSelectionAsync(gig.Id, selection));
        Assert.True(await gigs.UpdateAsync(gig.Id, new GigDetails { PlayedOn = September26, Notes = "Rammed" }));
        Assert.Equal("Rammed", (await gigs.FindAsync(gig.Id))?.Notes);

        Assert.True(await gigs.DeleteAsync(gig.Id));
        Assert.Null(await gigs.FindAsync(gig.Id));
        Assert.False(await gigs.DeleteAsync(gig.Id));
    }
}
