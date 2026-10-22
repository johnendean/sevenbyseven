using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Collection;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;
using SevenBySeven.Shared.Http;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// The Stack in progress, at <c>/stack</c>, whichever device is looking at it. A 404 means
/// there is no such Stack or Scan; a 409 means another device got there first, which with
/// one Stack shared between devices is an answer rather than a bug (docs/adr/0010).
/// </summary>
internal static class ScanningEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var stack = endpoints.MapGroup("/stack").WithTags("Stack");

        stack.MapGet("/limits", (StackHolder holder) => TypedResults.Ok(new StackLimits(holder.MaxStackSize)))
            .WithSummary("How many records a Stack may hold.");

        stack.MapGet("/", (StackHolder holder) => Outcomes.OkOrNotFound(holder.Current))
            .WithSummary("The Stack in progress, without its photographs.");

        stack.MapPost("/", Begin)
            .WithSummary("Starts a Stack. Refused while the one in progress still has Scans to deal with.");

        stack.MapDelete("/", (StackHolder holder) => Outcomes.NoContentOrNotFound(holder.Discard()))
            .WithSummary("Gives up on whatever the Stack still holds, and lets it go.");

        stack.MapPut("/defaults", (StackDefaults defaults, StackHolder holder) =>
                Outcomes.NoContentOrNotFound(holder.SetDefaults(defaults)))
            .Validates<StackDefaults>()
            .WithSummary("What is true of every record the Stack adds: where they were bought and where they are kept.");

        stack.MapPost("/scans", CaptureAsync)
            .Accepts<byte[]>(JpegBody.ContentType)
            .WithSummary("Takes a photograph, sent as the raw JPEG body, into the Stack and identifies it in the background.");

        stack.MapGet("/scans/{scanId:guid}/photograph", Photograph)
            .WithSummary("The photograph of one Scan, for as long as the Stack holds it.");

        stack.MapPost("/scans/{scanId:guid}/confirmation", ConfirmAsync)
            .WithSummary("A Confirmation: adds a Copy of one of the Scan's Match Candidates, with the Stack's defaults.");

        stack.MapDelete("/scans/{scanId:guid}", (Guid scanId, StackHolder holder) =>
                Outcomes.NoContentOrNotFound(holder.Abandon(scanId)))
            .WithSummary("Gives up on one Scan.");

        stack.MapGet("/events", (StackHolder holder, CancellationToken cancellationToken) =>
                TypedResults.ServerSentEvents(holder.WatchAsync(cancellationToken), eventType: "changed"))
            .WithSummary("A 'changed' event, carrying a version number, every time the Stack changes, starting with now.");
    }

    private static Results<Created<StackSheet>, Conflict<ProblemDetails>> Begin(BeginStackRequest request, StackHolder holder)
    {
        if (holder.Begin(request.Format) is not { } sheet)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "A stack is already in progress.",
                Detail = "It still has records to confirm or give up on. Finish it, or throw it away, before starting another.",
                Status = StatusCodes.Status409Conflict,
            });
        }

        return TypedResults.Created("/stack", sheet);
    }

    private static async Task<Results<Accepted<StackedScanLine>, NotFound, Conflict, StatusCodeHttpResult>> CaptureAsync(
        HttpRequest request,
        StackHolder holder,
        CancellationToken cancellationToken,
        int width,
        int height)
    {
        var (photograph, refusal) = await JpegBody.ReadAsync(request, cancellationToken);

        if (refusal is not null)
        {
            return refusal;
        }

        Contracts.Scan scan;

        try
        {
            scan = Contracts.Scan.FromJpeg(photograph!, width, height, CaptureSource.Camera);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentOutOfRangeException)
        {
            return TypedResults.StatusCode(StatusCodes.Status400BadRequest);
        }

        var outcome = holder.Add(scan);

        return outcome switch
        {
            { Scan: { } line } => TypedResults.Accepted($"/stack/scans/{line.Id}/photograph", line),
            { WasFull: true } => TypedResults.Conflict(),
            _ => TypedResults.NotFound(),
        };
    }

    private static Results<FileContentHttpResult, NotFound> Photograph(Guid scanId, StackHolder holder) =>
        holder.Photograph(scanId) is { } scan
            ? TypedResults.File(scan.Image.ToArray(), scan.ContentType)
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound, Conflict, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> ConfirmAsync(
        Guid scanId,
        ConfirmScanRequest request,
        StackHolder holder,
        IReleaseCatalogue catalogue,
        IVinylCollection collection,
        CancellationToken cancellationToken)
    {
        var claim = holder.Claim(scanId, request.DiscogsReleaseId);

        if (claim.IsMissing)
        {
            return TypedResults.NotFound();
        }

        if (claim.IsOver)
        {
            return TypedResults.Conflict();
        }

        if (claim.IsNotACandidate)
        {
            // A Confirmation chooses one of the Match Candidates (docs/adr/0002). Anything
            // else would add a record nobody was shown.
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "That is not one of this record's Match Candidates.",
                Status = StatusCodes.Status400BadRequest,
            });
        }

        try
        {
            // First time this pressing is confirmed it is fetched from Discogs and cached;
            // every time after, this is a read of our own catalogue.
            if (await catalogue.EnsureAsync(request.DiscogsReleaseId, cancellationToken) is not { } release)
            {
                holder.Unclaimed(scanId);

                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "That pressing could not be fetched.",
                    Detail = "Discogs may be unreachable, or the release may have been merged into another since the search. Nothing has been added, and the record can be tried again.",
                    Status = StatusCodes.Status404NotFound,
                });
            }

            var defaults = claim.Defaults!;
            await collection.AddAsync(
                release.Id,
                new CopyDetails { PurchasedFrom = defaults.PurchasedFrom, Location = defaults.Location },
                cancellationToken);
        }
        catch
        {
            holder.Unclaimed(scanId);
            throw;
        }

        holder.Confirmed(scanId);

        return TypedResults.NoContent();
    }
}
