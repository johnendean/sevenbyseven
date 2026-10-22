using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SevenBySeven.Shared.Http;

/// <summary>
/// A photograph sent as the raw body of a request. Read in full, but never past the ceiling,
/// whether or not the client said how much it was sending.
/// </summary>
public static class JpegBody
{
    public const string ContentType = "image/jpeg";

    /// <summary>
    /// A downscaled sleeve runs to a few hundred kB; well past that is a fault. The same
    /// ceiling the capture pages hold the browser to.
    /// </summary>
    public const long MaxBytes = 8L * 1024 * 1024;

    /// <summary>
    /// The photograph's bytes, or why it is refused: 415 for anything but a JPEG, 413 past
    /// the ceiling, 400 for an empty body.
    /// </summary>
    public static async Task<(byte[]? Bytes, StatusCodeHttpResult? Refusal)> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.GetTypedHeaders().ContentType?.MediaType.Value, ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return (null, TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType));
        }

        if (request.ContentLength > MaxBytes)
        {
            return (null, TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge));
        }

        using var buffer = new MemoryStream();

        var read = new byte[81920];
        int count;
        while ((count = await request.Body.ReadAsync(read, cancellationToken)) > 0)
        {
            if (buffer.Length + count > MaxBytes)
            {
                return (null, TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge));
            }

            buffer.Write(read, 0, count);
        }

        return buffer.Length == 0
            ? (null, TypedResults.StatusCode(StatusCodes.Status400BadRequest))
            : (buffer.ToArray(), null);
    }
}
