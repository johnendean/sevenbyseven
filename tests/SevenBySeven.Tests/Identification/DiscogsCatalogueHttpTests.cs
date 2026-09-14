using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Discogs;

namespace SevenBySeven.Tests.Identification;

/// <summary>
/// The half of <see cref="DiscogsCatalogue"/> that talks to Discogs. Every one of these
/// paths ends in a scan that fails silently or shows the wrong pressing, so each failure
/// mode is worth a test even though none of them throws.
/// </summary>
public class DiscogsCatalogueHttpTests
{
    private const string OneResult = """
        {
          "results": [
            {
              "id": 249504,
              "title": "Miles Davis - Kind Of Blue",
              "year": "1959",
              "country": "US",
              "thumb": "https://img.discogs.test/thumb.jpg",
              "catno": "CS 8163",
              "label": ["Columbia", "Columbia"],
              "format": ["Vinyl", "LP", "Album"],
              "master_id": 12345
            }
          ]
        }
        """;

    [Fact]
    public async Task A_search_returns_the_pressings_Discogs_offered()
    {
        var handler = StubHttpMessageHandler.ReturningJson(OneResult);

        var candidates = await Catalogue(handler).SearchAsync(new DiscogsQuery { Barcode = "5099749494220" });

        var candidate = Assert.Single(candidates);
        Assert.Equal(249504, candidate.DiscogsReleaseId);
        Assert.Equal("Miles Davis", candidate.ArtistName);
        Assert.Equal("Kind Of Blue", candidate.Title);
        Assert.Equal("Columbia", candidate.LabelName);
        Assert.Equal("CS 8163", candidate.CatalogueNumber);
        Assert.Equal("US", candidate.Country);
        Assert.Equal(1959, candidate.Year);
        Assert.Equal("Vinyl, LP, Album", candidate.FormatDescription);
        Assert.Equal("https://img.discogs.test/thumb.jpg", candidate.ThumbnailUrl);
        Assert.Equal(12345, candidate.DiscogsMasterId);
    }

    [Fact]
    public async Task A_search_asks_for_the_uri_it_built()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"results": []}""");

        await Catalogue(handler).SearchAsync(new DiscogsQuery { CatalogueNumber = "BLP 4003" });

        var asked = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, asked.Method);

        // AbsoluteUri rather than ToString: the latter unescapes for display, which would
        // hide the very escaping the search depends on.
        Assert.Contains("catno=BLP%204003", asked.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
        Assert.Contains("format=Vinyl", asked.RequestUri.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_empty_query_never_reaches_Discogs()
    {
        var handler = StubHttpMessageHandler.ReturningJson(OneResult);

        Assert.Empty(await Catalogue(handler).SearchAsync(new DiscogsQuery()));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Searching_without_a_token_asks_nothing_rather_than_failing()
    {
        var handler = StubHttpMessageHandler.ReturningJson(OneResult);

        var candidates = await Catalogue(handler, token: null)
            .SearchAsync(new DiscogsQuery { Barcode = "5099749494220" });

        // Identification degrades rather than failing: the app still runs with no token.
        Assert.Empty(candidates);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    // The resilience handler has already retried by the time we see a 429.
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task A_search_Discogs_refuses_reports_nothing_rather_than_blocking_the_user(HttpStatusCode status)
    {
        var handler = StubHttpMessageHandler.Returning(status, """{"results": []}""");

        Assert.Empty(await Catalogue(handler).SearchAsync(new DiscogsQuery { Barcode = "5099749494220" }));
    }

    [Fact]
    public async Task A_search_that_cannot_leave_the_machine_reports_nothing()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("DNS is down."));

        Assert.Empty(await Catalogue(handler).SearchAsync(new DiscogsQuery { Title = "Kind Of Blue" }));
    }

    [Theory]
    [InlineData("""{"results": []}""")]
    [InlineData("""{"results": null}""")]
    [InlineData("{}")]
    public async Task A_search_that_matched_nothing_is_no_candidates(string json)
    {
        var handler = StubHttpMessageHandler.ReturningJson(json);

        Assert.Empty(await Catalogue(handler).SearchAsync(new DiscogsQuery { Title = "Nothing At All" }));
    }

    [Fact]
    public async Task A_confirmed_pressing_is_fetched_in_full()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""
            {
              "id": 249504,
              "master_id": 12345,
              "title": "Kind Of Blue",
              "year": 1959,
              "released": "1959-08-17",
              "country": "US",
              "artists_sort": "Miles Davis",
              "labels": [{"name": "Columbia", "catno": "CS 8163"}],
              "formats": [{"name": "Vinyl", "descriptions": ["LP", "Album"]}],
              "tracklist": [
                {"position": "A1", "title": "So What", "duration": "9:22", "type_": "track"}
              ]
            }
            """);

        var release = await Catalogue(handler).FetchAsync(249504);

        Assert.NotNull(release);
        Assert.Equal(249504, release.DiscogsReleaseId);
        Assert.Equal("Kind Of Blue", release.Title);
        Assert.Equal("Miles Davis", release.ArtistName);
        Assert.Equal("CS 8163", release.CatalogueNumber);
        Assert.Equal("A1", Assert.Single(release.Tracks).Position);

        Assert.Equal("releases/249504", handler.LastRequest.RequestUri!.AbsolutePath.TrimStart('/'));
    }

    [Fact]
    public async Task Fetching_without_a_token_is_nothing_rather_than_a_failure()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{"id": 249504}""");

        Assert.Null(await Catalogue(handler, token: null).FetchAsync(249504));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_pressing_Discogs_has_since_dropped_is_not_a_failure()
    {
        // The candidate came from Discogs moments ago, so a 404 means it was deleted or
        // merged between the search and the confirmation.
        var handler = StubHttpMessageHandler.Returning(HttpStatusCode.NotFound);

        Assert.Null(await Catalogue(handler).FetchAsync(249504));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task A_fetch_Discogs_refuses_is_no_release(HttpStatusCode status)
    {
        Assert.Null(await Catalogue(StubHttpMessageHandler.Returning(status)).FetchAsync(249504));
    }

    [Fact]
    public async Task A_fetch_that_cannot_leave_the_machine_is_no_release()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("Connection reset."));

        Assert.Null(await Catalogue(handler).FetchAsync(249504));
    }

    [Fact]
    public async Task A_reply_that_is_not_the_release_we_asked_about_is_discarded()
    {
        // Discogs answering 200 with an id of 0 means we have no pressing, whatever the
        // status code said.
        var handler = StubHttpMessageHandler.ReturningJson("""{"id": 0, "title": "Nothing"}""");

        Assert.Null(await Catalogue(handler).FetchAsync(249504));
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"id": "a string where a number belongs"}""")]
    public async Task A_reply_we_cannot_read_is_no_release(string body)
    {
        Assert.Null(await Catalogue(StubHttpMessageHandler.ReturningJson(body)).FetchAsync(249504));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Fetching_a_release_that_could_not_exist_is_a_mistake_worth_shouting_about(int releaseId) =>
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => Catalogue(StubHttpMessageHandler.ReturningJson("{}")).FetchAsync(releaseId));

    [Fact]
    public async Task A_search_with_no_query_at_all_is_a_mistake_worth_shouting_about() =>
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => Catalogue(StubHttpMessageHandler.ReturningJson("{}")).SearchAsync(null!));

    private static DiscogsCatalogue Catalogue(
        StubHttpMessageHandler handler,
        string? token = "a-token") =>
        new(
            handler.Client(),
            Options.Create(new DiscogsOptions { PersonalAccessToken = token }),
            NullLogger<DiscogsCatalogue>.Instance);
}
