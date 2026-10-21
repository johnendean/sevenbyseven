using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Gigs.Contracts;

namespace SevenBySeven.Tests.Api;

public class CollectionApiTests
{
    [Fact]
    public async Task A_confirmation_holds_the_release_and_adds_a_copy_of_it()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/collection",
            new AddCopyRequest(249504, new CopyDetails { MediaCondition = ConditionGrade.VeryGoodPlus, Location = "Crate 3" }),
            ApiHost.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var copy = await response.ReadAsync<CopySheet>();
        Assert.Equal($"/collection/{copy.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(249504, copy.Release.DiscogsReleaseId);
        Assert.Equal(2, copy.Release.Tracks.Count);
        Assert.Equal(ConditionGrade.VeryGoodPlus, copy.Details.MediaCondition);
        Assert.Equal("Crate 3", copy.Details.Location);
        Assert.Equal(PlayStanding.NeverPlayed, copy.Standing);
    }

    [Fact]
    public async Task Enums_cross_the_wire_by_name()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/collection",
            new AddCopyRequest(249504, new CopyDetails { SleeveCondition = ConditionGrade.NearMint }),
            ApiHost.Json);

        Assert.Contains("\"sleeveCondition\":\"NearMint\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_pressing_that_cannot_be_fetched_adds_nothing_and_says_why()
    {
        await using var host = await ApiHost.StartAsync();
        host.Source.Unfetchable.Add(249504);
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync("/collection", new AddCopyRequest(249504, CopyDetails.Unknown), ApiHost.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("That pressing could not be fetched.", (await response.ReadAsync<ProblemDetails>()).Title);
        Assert.Empty(await (await client.GetAsync("/collection")).ReadAsync<CopyLine[]>());
    }

    [Fact]
    public async Task A_confirmation_past_the_limits_is_refused_before_anything_is_fetched()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/collection",
            new AddCopyRequest(249504, new CopyDetails { Location = new string('x', 201), PricePaid = -1m }),
            ApiHost.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.ReadAsync<ValidationProblemDetails>();
        Assert.Equal(["details.location", "details.pricePaid"], problem.Errors.Keys.Order());
        Assert.Empty(host.Source.Fetched);
        Assert.Empty(await (await client.GetAsync("/collection")).ReadAsync<CopyLine[]>());
    }

    [Fact]
    public async Task A_confirmation_without_details_is_refused_before_anything_is_fetched()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsync(
            "/collection",
            JsonContent.Create(new { discogsReleaseId = 249504, details = (object?)null }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["details"], (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys);
        Assert.Empty(host.Source.Fetched);
    }

    [Fact]
    public async Task Details_past_the_limits_are_refused_and_nothing_changes()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var added = await AddAsync(client, 249504);

        var response = await client.PutAsJsonAsync(
            $"/collection/{added.Id}/details",
            new CopyDetails { Notes = new string('x', 4001), PurchasedFrom = new string('x', 201) },
            ApiHost.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["notes", "purchasedFrom"], (await response.ReadAsync<ValidationProblemDetails>()).Errors.Keys.Order());

        var found = await (await client.GetAsync($"/collection/{added.Id}")).ReadAsync<CopySheet>();
        Assert.Null(found.Details.Notes);
    }

    [Fact]
    public async Task The_collection_lists_each_copy_with_how_it_stands()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var older = await AddAsync(client, 249504);
        var newer = await AddAsync(client, 1000);

        // Played at the only Gig there is, which makes the record a Repeat of the last one.
        var gig = await (await client.PostAsJsonAsync("/gigs", new GigDetails { PlayedOn = new DateOnly(2026, 9, 26) }, ApiHost.Json))
            .ReadAsync<GigSummary>();
        var sheet = await (await client.GetAsync($"/gigs/{gig.Id}")).ReadAsync<GigSheet>();
        await client.PostAsJsonAsync($"/gigs/{gig.Id}/selections/{sheet.Selections[0].Id}/plays", new AddPlayRequest(older.Id), ApiHost.Json);

        var copies = await (await client.GetAsync("/collection")).ReadAsync<CopyLine[]>();

        Assert.Equal([newer.Id, older.Id], copies.Select(copy => copy.Id));
        Assert.Equal(Repeat.None, copies[0].Standing.Repeat);
        Assert.Equal(1, copies[1].Standing.TimesPlayed);
        Assert.Equal(Repeat.LastGig, copies[1].Standing.Repeat);
    }

    [Fact]
    public async Task A_copy_is_found_by_its_id()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var added = await AddAsync(client, 249504);

        var found = await (await client.GetAsync($"/collection/{added.Id}")).ReadAsync<CopySheet>();

        Assert.Equal(added.Id, found.Id);
        Assert.Equal(added.Release.Tracks.Count, found.Release.Tracks.Count);
    }

    [Fact]
    public async Task What_i_know_about_a_copy_can_be_changed_afterwards()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var added = await AddAsync(client, 249504);

        var response = await client.PutAsJsonAsync(
            $"/collection/{added.Id}/details",
            new CopyDetails { PricePaid = 12m, PricePaidCurrency = "GBP" },
            ApiHost.Json);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var found = await (await client.GetAsync($"/collection/{added.Id}")).ReadAsync<CopySheet>();
        Assert.Equal(12m, found.Details.PricePaid);
        Assert.Equal("GBP", found.Details.PricePaidCurrency);
    }

    [Fact]
    public async Task A_removed_copy_is_gone_from_the_collection()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var added = await AddAsync(client, 249504);

        var removed = await client.DeleteAsync($"/collection/{added.Id}");

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/collection/{added.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_copy_that_is_not_mine_is_not_found()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var nobody = Guid.CreateVersion7();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/collection/{nobody}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync($"/collection/{nobody}/details", CopyDetails.Unknown, ApiHost.Json)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/collection/{nobody}")).StatusCode);
    }

    internal static async Task<CopySheet> AddAsync(HttpClient client, int discogsReleaseId)
    {
        var response = await client.PostAsJsonAsync("/collection", new AddCopyRequest(discogsReleaseId, CopyDetails.Unknown), ApiHost.Json);
        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<CopySheet>();
    }
}
