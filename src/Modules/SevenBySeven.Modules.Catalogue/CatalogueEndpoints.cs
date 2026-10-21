using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using SevenBySeven.Modules.Catalogue.Contracts;
using SevenBySeven.Shared.Http;

namespace SevenBySeven.Modules.Catalogue;

internal static class CatalogueEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var catalogue = endpoints.MapGroup("/catalogue").WithTags("Catalogue");

        // A read-through cache rather than a plain read: asking for a Release the Catalogue
        // does not hold fetches and keeps it (docs/adr/0001). Asking twice is the same as
        // asking once, so it is still a GET.
        catalogue.MapGet("/releases/{discogsReleaseId:int:min(1)}", GetReleaseAsync)
            .WithSummary("A Release, fetched from Discogs and held if this is the first time it has been asked for.");
    }

    private static async Task<Results<Ok<ReleaseSheet>, NotFound>> GetReleaseAsync(
        int discogsReleaseId,
        IReleaseCatalogue catalogue,
        CancellationToken cancellationToken) =>
        Outcomes.OkOrNotFound((await catalogue.EnsureAsync(discogsReleaseId, cancellationToken))?.ToSheet());
}
