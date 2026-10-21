using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Gigs.Contracts;
using SevenBySeven.Shared.Http;

namespace SevenBySeven.Modules.Gigs;

/// <summary>
/// Every change is made to one Gig at a time and answers 204, or 404 when the Gig, Selection
/// or Play has gone — the client's cue to read the Gig again.
/// </summary>
internal static class GigsEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var gigs = endpoints.MapGroup("/gigs").WithTags("Gigs");

        gigs.MapGet("/", async (IGigLog log, CancellationToken cancellationToken) =>
                TypedResults.Ok(await log.ListAsync(cancellationToken)))
            .WithSummary("Every Gig, most recently played first.");

        gigs.MapGet("/{gigId:guid}", async Task<Results<Ok<GigSheet>, NotFound>> (
                Guid gigId, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.OkOrNotFound(await log.FindAsync(gigId, cancellationToken)))
            .WithSummary("One Gig laid out as it was played, each Play flagged as it stood at the time.");

        gigs.MapGet("/{gigId:guid}/candidates", async (Guid gigId, IGigLog log, CancellationToken cancellationToken) =>
                TypedResults.Ok(await log.CandidatesAsync(gigId, cancellationToken)))
            .WithSummary("The Copies in my Collection that could be played at this Gig, and what playing each would amount to.");

        gigs.MapPost("/", CreateAsync)
            .Validates<GigDetails>()
            .WithSummary("Records a Gig, with one empty Selection to start filling in.");

        gigs.MapPut("/{gigId:guid}", async Task<Results<NoContent, NotFound>> (
                Guid gigId, GigDetails details, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.UpdateAsync(gigId, details, cancellationToken)))
            .Validates<GigDetails>()
            .WithSummary("Replaces what I say about a Gig itself.");

        gigs.MapDelete("/{gigId:guid}", async Task<Results<NoContent, NotFound>> (
                Guid gigId, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.DeleteAsync(gigId, cancellationToken)))
            .WithSummary("Deletes a Gig and everything played at it.");

        gigs.MapPost("/{gigId:guid}/selections", async Task<Results<NoContent, NotFound>> (
                Guid gigId, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.AddSelectionAsync(gigId, cancellationToken)))
            .WithSummary("Starts a new Selection after the last one.");

        gigs.MapDelete("/{gigId:guid}/selections/{selectionId:guid}", async Task<Results<NoContent, NotFound>> (
                Guid gigId, Guid selectionId, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.RemoveSelectionAsync(gigId, selectionId, cancellationToken)))
            .WithSummary("Removes a Selection and every Play in it.");

        gigs.MapPost("/{gigId:guid}/selections/{selectionId:guid}/plays", async Task<Results<NoContent, NotFound>> (
                Guid gigId, Guid selectionId, AddPlayRequest request, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.AddPlayAsync(gigId, selectionId, request.CopyId, cancellationToken)))
            .WithSummary("Plays a Copy at the end of a Selection. 404 as well when the Copy is not in my Collection.");

        gigs.MapDelete("/{gigId:guid}/plays/{playId:guid}", async Task<Results<NoContent, NotFound>> (
                Guid gigId, Guid playId, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.RemovePlayAsync(gigId, playId, cancellationToken)))
            .WithSummary("Removes a Play.");

        gigs.MapPost("/{gigId:guid}/plays/{playId:guid}/move", async Task<Results<NoContent, NotFound>> (
                Guid gigId, Guid playId, MovePlayRequest request, IGigLog log, CancellationToken cancellationToken) =>
                Outcomes.NoContentOrNotFound(await log.MovePlayAsync(gigId, playId, request.Earlier, cancellationToken)))
            .WithSummary("Moves a Play one place within its Selection. 404 as well when it is already at that end.");
    }

    private static async Task<Created<GigSummary>> CreateAsync(
        GigDetails details,
        IGigLog log,
        CancellationToken cancellationToken)
    {
        var gig = await log.CreateAsync(details, cancellationToken);
        var summary = new GigSummary(
            gig.Id,
            gig.PlayedOn,
            gig.Venue,
            gig.Selections.Count,
            gig.Selections.Sum(selection => selection.Plays.Count));

        return TypedResults.Created($"/gigs/{gig.Id}", summary);
    }
}
