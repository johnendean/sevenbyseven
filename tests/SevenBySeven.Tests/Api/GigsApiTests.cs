using System.Net;
using System.Net.Http.Json;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Gigs.Contracts;

namespace SevenBySeven.Tests.Api;

public class GigsApiTests
{
    private static readonly DateOnly September26 = new(2026, 9, 26);

    [Fact]
    public async Task A_new_gig_starts_with_one_empty_selection()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/gigs", new GigDetails { PlayedOn = September26, Venue = "The Social" }, ApiHost.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var gig = await response.ReadAsync<GigSummary>();
        Assert.Equal($"/gigs/{gig.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(new GigSummary(gig.Id, September26, "The Social", 1, 0), gig);
        Assert.Equal([gig], await (await client.GetAsync("/gigs")).ReadAsync<GigSummary[]>());
    }

    [Fact]
    public async Task A_gig_with_no_date_is_refused()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsync("/gigs", JsonContent.Create(new { venue = "The Social" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Records_are_played_moved_and_taken_out_again()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var first = await CollectionApiTests.AddAsync(client, 249504);
        var second = await CollectionApiTests.AddAsync(client, 1000);
        var gig = await CreateAsync(client);
        var selection = (await SheetAsync(client, gig.Id)).Selections[0].Id;

        Assert.Equal(HttpStatusCode.NoContent, (await PlayAsync(client, gig.Id, selection, first.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await PlayAsync(client, gig.Id, selection, second.Id)).StatusCode);

        var played = (await SheetAsync(client, gig.Id)).Selections[0].Plays;
        Assert.Equal([first.Id, second.Id], played.Select(play => play.CopyId));

        var moved = await client.PostAsJsonAsync($"/gigs/{gig.Id}/plays/{played[1].Id}/move", new MovePlayRequest(Earlier: true), ApiHost.Json);
        Assert.Equal(HttpStatusCode.NoContent, moved.StatusCode);
        Assert.Equal([second.Id, first.Id], (await SheetAsync(client, gig.Id)).Selections[0].Plays.Select(play => play.CopyId));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/gigs/{gig.Id}/plays/{played[0].Id}")).StatusCode);
        Assert.Equal([second.Id], (await SheetAsync(client, gig.Id)).Selections[0].Plays.Select(play => play.CopyId));
    }

    [Fact]
    public async Task A_play_reads_as_the_pressing_that_was_played()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var copy = await CollectionApiTests.AddAsync(client, 249504);
        var gig = await CreateAsync(client);
        await PlayAsync(client, gig.Id, (await SheetAsync(client, gig.Id)).Selections[0].Id, copy.Id);

        var play = (await SheetAsync(client, gig.Id)).Selections[0].Plays.Single();

        Assert.Equal(1, play.Number);
        Assert.Equal("Kind Of Blue", play.Release.Title);
        Assert.Equal(copy.Release.Pressing, play.Release.Pressing);
        Assert.False(play.IsFormer);
    }

    [Fact]
    public async Task A_record_i_do_not_own_cannot_be_played()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var gig = await CreateAsync(client);
        var selection = (await SheetAsync(client, gig.Id)).Selections[0].Id;

        var response = await PlayAsync(client, gig.Id, selection, Guid.CreateVersion7());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Candidates_say_what_playing_each_would_amount_to()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var copy = await CollectionApiTests.AddAsync(client, 249504);
        var gig = await CreateAsync(client);
        await PlayAsync(client, gig.Id, (await SheetAsync(client, gig.Id)).Selections[0].Id, copy.Id);

        var candidate = (await (await client.GetAsync($"/gigs/{gig.Id}/candidates")).ReadAsync<Candidate[]>()).Single();

        Assert.Equal(copy.Id, candidate.CopyId);
        Assert.Equal("Kind Of Blue", candidate.Release.Title);
        Assert.True(candidate.PlayedThisGig);
        Assert.Equal(Repeat.None, candidate.Repeat);
    }

    [Fact]
    public async Task Selections_are_added_and_removed()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var gig = await CreateAsync(client);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/gigs/{gig.Id}/selections", null)).StatusCode);

        var selections = (await SheetAsync(client, gig.Id)).Selections;
        Assert.Equal([1, 2], selections.Select(selection => selection.Number));

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/gigs/{gig.Id}/selections/{selections[0].Id}")).StatusCode);
        Assert.Equal([selections[1].Id], (await SheetAsync(client, gig.Id)).Selections.Select(selection => selection.Id));
    }

    [Fact]
    public async Task A_gig_is_changed_and_then_deleted()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var gig = await CreateAsync(client);

        var changed = await client.PutAsJsonAsync($"/gigs/{gig.Id}", new GigDetails { PlayedOn = September26, Notes = "Rammed" }, ApiHost.Json);
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal("Rammed", (await SheetAsync(client, gig.Id)).Notes);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/gigs/{gig.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/gigs/{gig.Id}")).StatusCode);
    }

    [Fact]
    public async Task Changes_to_a_gig_that_has_gone_are_not_found()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var gone = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/gigs/{gone}", new GigDetails { PlayedOn = September26 }, ApiHost.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/gigs/{gone}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync($"/gigs/{gone}/selections", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/gigs/{gone}/selections/{other}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/gigs/{gone}/plays/{other}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync($"/gigs/{gone}/plays/{other}/move", new MovePlayRequest(true), ApiHost.Json)).StatusCode);
    }

    private static async Task<GigSummary> CreateAsync(HttpClient client) =>
        await (await client.PostAsJsonAsync("/gigs", new GigDetails { PlayedOn = September26 }, ApiHost.Json)).ReadAsync<GigSummary>();

    private static async Task<GigSheet> SheetAsync(HttpClient client, Guid gigId) =>
        await (await client.GetAsync($"/gigs/{gigId}")).ReadAsync<GigSheet>();

    private static Task<HttpResponseMessage> PlayAsync(HttpClient client, Guid gigId, Guid selectionId, Guid copyId) =>
        client.PostAsJsonAsync($"/gigs/{gigId}/selections/{selectionId}/plays", new AddPlayRequest(copyId), ApiHost.Json);
}
