using System.Net;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Discogs;

namespace SevenBySeven.Tests.Identification;

/// <summary>
/// The local limiter that keeps us under the Discogs ceiling. Hand-scanning will never
/// approach it, so the path that matters is the one that fires when something loops:
/// it is meant to fail here, loudly, rather than earn us a 429.
/// </summary>
public class DiscogsRateLimitHandlerTests
{
    [Fact]
    public async Task A_request_under_the_ceiling_goes_straight_through()
    {
        using var inner = StubHttpMessageHandler.Returning(HttpStatusCode.OK, """{"results": []}""");
        using var handler = Limiter(requestsPerMinute: 55, inner);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.discogs.test/") };

        var response = await client.GetAsync("database/search?q=x");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task A_runaway_loop_is_stopped_here_rather_than_at_Discogs()
    {
        // One permit, sixteen queue slots. Holding the permit open and firing seventeen
        // more means one of them must find the queue full, whatever order they arrive in.
        var holding = new TaskCompletionSource();
        var started = new TaskCompletionSource();

        using var inner = StubHttpMessageHandler.RespondingAsync(async (_, _) =>
        {
            started.TrySetResult();
            await holding.Task;

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var handler = Limiter(requestsPerMinute: 1, inner);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.discogs.test/") };

        var first = client.GetAsync("releases/1");
        await started.Task;

        using var abandon = new CancellationTokenSource();
        var queued = Enumerable.Range(0, 17)
            .Select(_ => client.GetAsync("releases/1", abandon.Token))
            .ToArray();

        var rejected = await Task.WhenAny(queued.Select(Rejection));

        var thrown = await rejected;
        Assert.Contains("Something is looping", thrown.Message, StringComparison.Ordinal);

        // Let the held request finish and abandon the rest: the sliding window would not
        // hand out another permit for ten seconds, and the test has its answer.
        holding.SetResult();
        await abandon.CancelAsync();
        await first;
        await Task.WhenAll(queued.Select(Swallow));
    }

    [Fact]
    public async Task Disposing_the_handler_disposes_the_limiter()
    {
        using var inner = StubHttpMessageHandler.Returning(HttpStatusCode.OK);
        var handler = Limiter(requestsPerMinute: 55, inner);

        using (var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.discogs.test/") })
        {
            await client.GetAsync("releases/1");
        }

        // The limiter went with it, so the handler cannot be used again.
        using var reused = new HttpClient(handler) { BaseAddress = new Uri("https://api.discogs.test/") };
        await Assert.ThrowsAnyAsync<ObjectDisposedException>(() => reused.GetAsync("releases/1"));
    }

    /// <summary>Completes with the rejection if this request was refused, and never otherwise.</summary>
    private static async Task<HttpRequestException> Rejection(Task<HttpResponseMessage> request)
    {
        try
        {
            (await request).Dispose();
        }
        catch (HttpRequestException refused)
        {
            return refused;
        }
        catch (OperationCanceledException)
        {
            // Abandoned during teardown, which is not an answer either way.
        }

        return await new TaskCompletionSource<HttpRequestException>().Task;
    }

    private static async Task Swallow(Task<HttpResponseMessage> request)
    {
        try
        {
            (await request).Dispose();
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
        }
    }

    private static DiscogsRateLimitHandler Limiter(int requestsPerMinute, HttpMessageHandler inner) =>
        new(Options.Create(new DiscogsOptions { RequestsPerMinute = requestsPerMinute }))
        {
            InnerHandler = inner,
        };
}
