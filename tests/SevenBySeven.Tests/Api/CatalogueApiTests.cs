using System.Net;
using SevenBySeven.Modules.Catalogue.Contracts;

namespace SevenBySeven.Tests.Api;

public class CatalogueApiTests
{
    [Fact]
    public async Task A_release_is_fetched_once_and_served_from_the_catalogue_after()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var first = await client.GetAsync("/catalogue/releases/249504");
        var second = await client.GetAsync("/catalogue/releases/249504");

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var release = await second.ReadAsync<ReleaseSheet>();
        Assert.Equal(249504, release.DiscogsReleaseId);
        Assert.Equal("Columbia · CS 8163 · US · 1959 · Vinyl, LP, Album", release.Pressing);
        Assert.Equal(["So What", "Freddie Freeloader"], release.Tracks.Select(track => track.Title));
        Assert.Equal([249504], host.Source.Fetched);
    }

    [Fact]
    public async Task A_release_that_cannot_be_fetched_is_not_found()
    {
        await using var host = await ApiHost.StartAsync();
        host.Source.Unfetchable.Add(1);
        using var client = host.CreateClient();

        var response = await client.GetAsync("/catalogue/releases/1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task A_release_id_that_cannot_exist_is_never_asked_for()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.GetAsync("/catalogue/releases/0");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(host.Source.Fetched);
    }
}
