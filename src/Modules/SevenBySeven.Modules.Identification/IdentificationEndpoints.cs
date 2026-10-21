using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Identification.Contracts;

namespace SevenBySeven.Modules.Identification;

internal static class IdentificationEndpoints
{
    /// <summary>
    /// A downscaled sleeve runs to a few hundred kB; well past that is a fault. The same
    /// ceiling the capture page holds the browser to.
    /// </summary>
    internal const long MaxImageBytes = 8L * 1024 * 1024;

    private const string Jpeg = "image/jpeg";

    public static void Map(IEndpointRouteBuilder endpoints)
    {
        // Stateless: the photograph is read, searched on and let go within the request. A
        // Scan that stands alone is held by whoever is looking at it (docs/adr/0010).
        endpoints.MapPost("/identifications", IdentifyAsync)
            .WithTags("Identification")
            .Accepts<byte[]>(Jpeg)
            .WithSummary("Match Candidates for a photographed sleeve or centre label, sent as the raw JPEG body.");
    }

    private static async Task<Results<Ok<IdentificationResult>, BadRequest, StatusCodeHttpResult>> IdentifyAsync(
        HttpRequest request,
        ISleeveIdentifier identifier,
        CancellationToken cancellationToken,
        Format format = Format.Vinyl)
    {
        if (!string.Equals(request.GetTypedHeaders().ContentType?.MediaType.Value, Jpeg, StringComparison.OrdinalIgnoreCase))
        {
            return TypedResults.StatusCode(StatusCodes.Status415UnsupportedMediaType);
        }

        if (request.ContentLength > MaxImageBytes)
        {
            return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        using var buffer = new MemoryStream();

        // Bounded whether or not the client said how much it was sending.
        var read = new byte[81920];
        int count;
        while ((count = await request.Body.ReadAsync(read, cancellationToken)) > 0)
        {
            if (buffer.Length + count > MaxImageBytes)
            {
                return TypedResults.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            buffer.Write(read, 0, count);
        }

        if (buffer.Length == 0)
        {
            return TypedResults.BadRequest();
        }

        var result = await identifier.IdentifyAsync(buffer.ToArray(), Jpeg, format, cancellationToken);

        return TypedResults.Ok(result);
    }
}
