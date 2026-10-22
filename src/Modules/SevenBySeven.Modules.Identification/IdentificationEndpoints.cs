using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Shared.Http;

namespace SevenBySeven.Modules.Identification;

internal static class IdentificationEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        // Stateless: the photograph is read, searched on and let go within the request. A
        // Scan that stands alone is held by whoever is looking at it (docs/adr/0010).
        endpoints.MapPost("/identifications", IdentifyAsync)
            .WithTags("Identification")
            .Accepts<byte[]>(JpegBody.ContentType)
            .WithSummary("Match Candidates for a photographed sleeve or centre label, sent as the raw JPEG body.");
    }

    private static async Task<Results<Ok<IdentificationResult>, StatusCodeHttpResult>> IdentifyAsync(
        HttpRequest request,
        ISleeveIdentifier identifier,
        CancellationToken cancellationToken,
        Format format = Format.Vinyl)
    {
        var (photograph, refusal) = await JpegBody.ReadAsync(request, cancellationToken);

        if (refusal is not null)
        {
            return refusal;
        }

        return TypedResults.Ok(await identifier.IdentifyAsync(photograph!, JpegBody.ContentType, format, cancellationToken));
    }
}
