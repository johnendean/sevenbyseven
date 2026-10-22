using System.Net.Http.Json;
using SevenBySeven.Modules.Gigs.Contracts;
using SevenBySeven.Web.Api;

namespace SevenBySeven.Web.Gigs;

/// <summary>
/// The Gigs I have played, over the API. Every change is made to one Gig at a time, and each
/// answers false when the Gig, Selection or Play has gone — the page's cue to read it again.
/// </summary>
public sealed class GigsClient(HttpClient http)
{
    /// <summary>Every Gig, most recently played first.</summary>
    public Task<IReadOnlyList<GigSummary>> ListAsync(CancellationToken cancellationToken = default) =>
        http.GetAsync("gigs", cancellationToken).ReadAsync<IReadOnlyList<GigSummary>>(cancellationToken);

    public Task<GigSheet?> FindAsync(Guid gigId, CancellationToken cancellationToken = default) =>
        http.GetAsync($"gigs/{gigId}", cancellationToken).ReadOrNullAsync<GigSheet>(cancellationToken);

    /// <summary>The Copies in my Collection that could be played at this Gig, and what playing each would amount to.</summary>
    public Task<IReadOnlyList<Candidate>> CandidatesAsync(Guid gigId, CancellationToken cancellationToken = default) =>
        http.GetAsync($"gigs/{gigId}/candidates", cancellationToken).ReadAsync<IReadOnlyList<Candidate>>(cancellationToken);

    /// <summary>Records a Gig, with one empty Selection to start filling in.</summary>
    public Task<GigSummary> CreateAsync(GigDetails details, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync("gigs", details, ApiCalls.Json, cancellationToken).ReadAsync<GigSummary>(cancellationToken);

    public Task<bool> UpdateAsync(Guid gigId, GigDetails details, CancellationToken cancellationToken = default) =>
        http.PutAsJsonAsync($"gigs/{gigId}", details, ApiCalls.Json, cancellationToken).MadeAsync();

    /// <summary>Deletes a Gig and everything played at it.</summary>
    public Task<bool> DeleteAsync(Guid gigId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"gigs/{gigId}", cancellationToken).MadeAsync();

    public Task<bool> AddSelectionAsync(Guid gigId, CancellationToken cancellationToken = default) =>
        http.PostAsync($"gigs/{gigId}/selections", content: null, cancellationToken).MadeAsync();

    public Task<bool> RemoveSelectionAsync(Guid gigId, Guid selectionId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"gigs/{gigId}/selections/{selectionId}", cancellationToken).MadeAsync();

    /// <summary>False as well when the Copy is not in my Collection: a record I do not own cannot be newly played.</summary>
    public Task<bool> AddPlayAsync(Guid gigId, Guid selectionId, Guid copyId, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync($"gigs/{gigId}/selections/{selectionId}/plays", new AddPlayRequest(copyId), ApiCalls.Json, cancellationToken)
            .MadeAsync();

    public Task<bool> RemovePlayAsync(Guid gigId, Guid playId, CancellationToken cancellationToken = default) =>
        http.DeleteAsync($"gigs/{gigId}/plays/{playId}", cancellationToken).MadeAsync();

    /// <summary>False as well when the Play is already at that end of its Selection.</summary>
    public Task<bool> MovePlayAsync(Guid gigId, Guid playId, bool earlier, CancellationToken cancellationToken = default) =>
        http.PostAsJsonAsync($"gigs/{gigId}/plays/{playId}/move", new MovePlayRequest(earlier), ApiCalls.Json, cancellationToken)
            .MadeAsync();
}
