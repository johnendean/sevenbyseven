using SevenBySeven.Modules.Catalogue.Contracts;
using SevenBySeven.Web.Api;

namespace SevenBySeven.Web.Catalogue;

/// <summary>The Catalogue, over the API.</summary>
public sealed class CatalogueClient(HttpClient http)
{
    /// <summary>
    /// A held Release with its Tracks, fetched from Discogs and kept if this is the first time
    /// it has been asked for. Null when Discogs cannot supply it.
    /// </summary>
    public Task<ReleaseSheet?> EnsureAsync(int discogsReleaseId, CancellationToken cancellationToken = default) =>
        http.GetAsync($"catalogue/releases/{discogsReleaseId}", cancellationToken)
            .ReadOrNullAsync<ReleaseSheet>(cancellationToken);
}
