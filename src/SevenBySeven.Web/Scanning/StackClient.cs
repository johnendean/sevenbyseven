using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;
using SevenBySeven.Web.Api;

namespace SevenBySeven.Web.Scanning;

/// <summary>
/// The Stack in progress, over the API. There is one, shared by every device, so another may
/// have got there first: that comes back as null or false alongside "not there", and the
/// page reads the Stack again to see what happened (docs/adr/0010).
/// </summary>
public sealed class StackClient(HttpClient http)
{
    public Task<StackLimits> LimitsAsync(CancellationToken cancellationToken = default) =>
        http.GetAsync("stack/limits", cancellationToken).ReadAsync<StackLimits>(cancellationToken);

    /// <summary>The Stack in progress, or null when there is none.</summary>
    public Task<StackSheet?> CurrentAsync(CancellationToken cancellationToken = default) =>
        http.GetAsync("stack", cancellationToken).ReadOrNullAsync<StackSheet>(cancellationToken);

    /// <summary>Null when the Stack in progress still has Scans to deal with.</summary>
    public async Task<StackSheet?> BeginAsync(Format format, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("stack", new BeginStackRequest(format), ApiCalls.Json, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StackSheet>(ApiCalls.Json, cancellationToken);
    }

    public Task<bool> DiscardAsync(CancellationToken cancellationToken = default) =>
        http.DeleteAsync("stack", cancellationToken).MadeAsync();

    public Task<bool> SetDefaultsAsync(StackDefaults defaults, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync("stack/defaults", defaults, ApiCalls.Json, cancellationToken).MadeAsync();

    /// <summary>
    /// Takes a photograph into the Stack, to be identified in the background. Null when there
    /// is no Stack or it is already full.
    /// </summary>
    public async Task<StackedScanLine?> CaptureAsync(
        ReadOnlyMemory<byte> jpeg,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        var photograph = new ReadOnlyMemoryContent(jpeg);
        photograph.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        using var response = await http.PostAsync($"stack/scans?width={width}&height={height}", photograph, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StackedScanLine>(ApiCalls.Json, cancellationToken);
    }

    /// <summary>The JPEG of one Scan, or null once the Stack no longer holds it.</summary>
    public async Task<byte[]?> PhotographAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"stack/scans/{scanId}/photograph", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// A Confirmation. False when nothing was added: the pressing could not be fetched, or the
    /// Scan has gone or been dealt with elsewhere.
    /// </summary>
    public async Task<bool> ConfirmAsync(Guid scanId, int discogsReleaseId, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            $"stack/scans/{scanId}/confirmation", new ConfirmScanRequest(discogsReleaseId), ApiCalls.Json, cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();

        return true;
    }

    public Task<bool> AbandonAsync(Guid scanId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"stack/scans/{scanId}", cancellationToken).MadeAsync();

    /// <summary>A version number each time the Stack changes, starting with the current one, until cancelled.</summary>
    public async IAsyncEnumerable<long> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "stack/events");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            if (long.TryParse(item.Data, out var version))
            {
                yield return version;
            }
        }
    }
}
