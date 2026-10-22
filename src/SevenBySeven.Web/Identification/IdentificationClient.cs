using System.Net.Http.Headers;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Web.Api;

namespace SevenBySeven.Web.Identification;

/// <summary>Identification of a Scan that stands alone, over the API. Nothing is held between calls.</summary>
public sealed class IdentificationClient(HttpClient http)
{
    /// <summary>
    /// How long one identification may take. Reading a sleeve is a call to Claude and then up
    /// to six to Discogs, which runs well past the ten seconds a request is otherwise given.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

    public Task<IdentificationResult> IdentifyAsync(
        ReadOnlyMemory<byte> jpeg,
        Format format = Format.Vinyl,
        CancellationToken cancellationToken = default)
    {
        var photograph = new ReadOnlyMemoryContent(jpeg);
        photograph.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return http.PostAsync($"identifications?format={format}", photograph, cancellationToken)
            .ReadAsync<IdentificationResult>(cancellationToken);
    }
}
