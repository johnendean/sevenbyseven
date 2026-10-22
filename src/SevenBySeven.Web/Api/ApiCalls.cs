using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SevenBySeven.Web.Api;

/// <summary>
/// How this app reads the API's answers, following docs/adr/0008 across the wire. A 404
/// is something that is not there, and comes back as null or false exactly as the module
/// did in-process, so a page's "it has gone" handling needs no change. Anything else that
/// is not a success means a bug — the forms have validated before sending — and is thrown
/// for the page's catch-all.
/// </summary>
internal static class ApiCalls
{
    /// <summary>The API writes enums by name; reading them needs the same converter.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>The service-discovery address Aspire gives the API.</summary>
    public static readonly Uri BaseAddress = new("https+http://api");

    public static async Task<T> ReadAsync<T>(this Task<HttpResponseMessage> call, CancellationToken cancellationToken)
    {
        using var response = await call;
        response.EnsureSuccessStatusCode();

        return await BodyAsync<T>(response, cancellationToken);
    }

    public static async Task<T?> ReadOrNullAsync<T>(this Task<HttpResponseMessage> call, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await call;

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await BodyAsync<T>(response, cancellationToken);
    }

    /// <summary>A change that answers no content: true when it was made, false when there was nothing to change.</summary>
    public static async Task<bool> MadeAsync(this Task<HttpResponseMessage> call)
    {
        using var response = await call;

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    private static async Task<T> BodyAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) =>
        await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
            ?? throw new InvalidOperationException($"The API answered {(int)response.StatusCode} with no {typeof(T).Name}.");
}
