using System.Net.Http.Json;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Web.Api;

namespace SevenBySeven.Web.Collection;

/// <summary>The Collection, over the API. Every Copy here is one I own.</summary>
public sealed class CollectionClient(HttpClient http)
{
    /// <summary>Every Copy, most recently added first, each with how it stands in my play history.</summary>
    public Task<IReadOnlyList<CopyLine>> ListAsync(CancellationToken cancellationToken = default) =>
        http.GetAsync("collection", cancellationToken).ReadAsync<IReadOnlyList<CopyLine>>(cancellationToken);

    /// <summary>One Copy in full, or null if it is not mine.</summary>
    public Task<CopySheet?> FindAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        http.GetAsync($"collection/{copyId}", cancellationToken).ReadOrNullAsync<CopySheet>(cancellationToken);

    /// <summary>
    /// A Confirmation: holds the Release in the Catalogue and adds a Copy of it. Null when the
    /// pressing could not be fetched, and nothing was added.
    /// </summary>
    public Task<CopySheet?> AddAsync(AddCopyRequest request, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("collection", request, ApiCalls.Json, cancellationToken)
            .ReadOrNullAsync<CopySheet>(cancellationToken);

    /// <summary>False when there is no such Copy.</summary>
    public Task<bool> UpdateAsync(Guid copyId, CopyDetails details, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync($"collection/{copyId}/details", details, ApiCalls.Json, cancellationToken).MadeAsync();

    /// <summary>False when there was no such Copy to remove.</summary>
    public Task<bool> RemoveAsync(Guid copyId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"collection/{copyId}", cancellationToken).MadeAsync();
}
