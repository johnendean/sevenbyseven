using System.Net;
using System.Text;

namespace SevenBySeven.Tests;

/// <summary>
/// Stands in for the far end of an <see cref="HttpClient"/>. Discogs is a REST API we
/// call by hand and whose JSON we have modelled ourselves, so asserting on the wire is
/// asserting on our own code — which is why the boundary is stubbed here rather than
/// hidden behind an interface.
/// </summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

    private StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        _respond = respond;

    /// <summary>Every request this handler saw, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    public HttpRequestMessage LastRequest => Requests[^1];

    public static StubHttpMessageHandler Returning(HttpStatusCode status, string? json = null) =>
        Responding(_ => new HttpResponseMessage(status)
        {
            Content = json is null
                ? new StringContent(string.Empty)
                : new StringContent(json, Encoding.UTF8, "application/json"),
        });

    public static StubHttpMessageHandler ReturningJson(string json) =>
        Returning(HttpStatusCode.OK, json);

    /// <summary>The transport itself failing, rather than the server answering badly.</summary>
    public static StubHttpMessageHandler Throwing(Exception failure) =>
        Responding(_ => throw failure);

    public static StubHttpMessageHandler Responding(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new((request, _) => Task.FromResult(respond(request)));

    /// <summary>
    /// A far end that has not answered yet. The task must be returned rather than waited
    /// on, or the request never becomes a pending one and the caller blocks instead.
    /// </summary>
    public static StubHttpMessageHandler RespondingAsync(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) =>
        new(respond);

    public HttpClient Client(string baseAddress = "https://api.discogs.test/") =>
        new(this, disposeHandler: false) { BaseAddress = new Uri(baseAddress) };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        cancellationToken.ThrowIfCancellationRequested();

        return _respond(request, cancellationToken);
    }
}
