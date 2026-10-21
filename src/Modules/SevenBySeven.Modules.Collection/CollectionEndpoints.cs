using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Shared.Http;

namespace SevenBySeven.Modules.Collection;

internal static class CollectionEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var collection = endpoints.MapGroup("/collection").WithTags("Collection");

        collection.MapGet("/", ListAsync)
            .WithSummary("Every Copy I own, most recently added first, each with how it stands in my play history.");

        collection.MapGet("/{copyId:guid}", FindAsync)
            .WithSummary("One Copy with its pressing and that pressing's Tracks.");

        collection.MapPost("/", AddAsync)
            .WithSummary("A Confirmation: holds the chosen Release in the Catalogue if it is not already, and adds a Copy of it.");

        collection.MapPut("/{copyId:guid}/details", UpdateAsync)
            .WithSummary("Replaces what I know about a Copy.");

        collection.MapDelete("/{copyId:guid}", RemoveAsync)
            .WithSummary("Removes a Copy from the Collection. One that has been played is kept as a Former Copy.");
    }

    private static async Task<Ok<CopyLine[]>> ListAsync(
        IVinylCollection collection,
        IPlayHistory plays,
        CancellationToken cancellationToken)
    {
        var copies = await collection.ListAsync(cancellationToken);
        var standings = await plays.StandingsAsync([.. copies.Select(copy => copy.Id)], cancellationToken);

        return TypedResults.Ok(copies.Select(copy => copy.ToLine(standings[copy.Id])).ToArray());
    }

    private static async Task<Results<Ok<CopySheet>, NotFound>> FindAsync(
        Guid copyId,
        IVinylCollection collection,
        IPlayHistory plays,
        CancellationToken cancellationToken) =>
        Outcomes.OkOrNotFound(await SheetAsync(copyId, collection, plays, cancellationToken));

    private static async Task<Results<Created<CopySheet>, NotFound<ProblemDetails>, ValidationProblem>> AddAsync(
        AddCopyRequest request,
        IReleaseCatalogue catalogue,
        IVinylCollection collection,
        IPlayHistory plays,
        CancellationToken cancellationToken)
    {
        // Before the Release is fetched: a Confirmation refused afterwards would still have
        // filled the Catalogue for nothing.
        if (Validation.Problem((string.Empty, request), ("details", request.Details)) is { } problem)
        {
            return problem;
        }

        if (request.DiscogsReleaseId <= 0
            || await catalogue.EnsureAsync(request.DiscogsReleaseId, cancellationToken) is not { } release)
        {
            // Discogs being unreachable and the release having been merged away both come
            // back as null from the Catalogue, and neither leaves anything to add a Copy of.
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "That pressing could not be fetched.",
                Detail = "Discogs may be unreachable, or the release may have been merged into another since the search. Nothing has been added.",
                Status = StatusCodes.Status404NotFound,
            });
        }

        var copy = await collection.AddAsync(release.Id, request.Details, cancellationToken);

        // Read back rather than built from what was saved: a new Copy of a Master I have
        // played before is already a Repeat, so its standing has to be judged like any other.
        var sheet = await SheetAsync(copy.Id, collection, plays, cancellationToken)
            ?? throw new InvalidOperationException($"Copy {copy.Id} was added but could not be read back.");

        return TypedResults.Created($"/collection/{copy.Id}", sheet);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> UpdateAsync(
        Guid copyId,
        CopyDetails details,
        IVinylCollection collection,
        CancellationToken cancellationToken)
    {
        if (Validation.Problem((string.Empty, details)) is { } problem)
        {
            return problem;
        }

        return await collection.UpdateAsync(copyId, details, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveAsync(
        Guid copyId,
        IVinylCollection collection,
        CancellationToken cancellationToken) =>
        Outcomes.NoContentOrNotFound(await collection.RemoveAsync(copyId, cancellationToken));

    private static async Task<CopySheet?> SheetAsync(
        Guid copyId,
        IVinylCollection collection,
        IPlayHistory plays,
        CancellationToken cancellationToken)
    {
        if (await collection.FindAsync(copyId, cancellationToken) is not { } copy)
        {
            return null;
        }

        var standings = await plays.StandingsAsync([copy.Id], cancellationToken);

        return new CopySheet(copy.Id, copy.AddedOn, copy.Release!.ToSheet(), copy.Details(), standings[copy.Id]);
    }

    private static CopyLine ToLine(this Copy copy, PlayStanding standing) =>
        new(
            copy.Id,
            copy.AddedOn,
            copy.Release!.ToLine(),
            copy.MediaCondition,
            copy.SleeveCondition,
            copy.Location,
            standing);

    private static CopyDetails Details(this Copy copy) =>
        new()
        {
            MediaCondition = copy.MediaCondition,
            SleeveCondition = copy.SleeveCondition,
            PricePaid = copy.PricePaid,
            PricePaidCurrency = copy.PricePaidCurrency,
            PurchasedFrom = copy.PurchasedFrom,
            Location = copy.Location,
            Notes = copy.Notes,
        };
}
