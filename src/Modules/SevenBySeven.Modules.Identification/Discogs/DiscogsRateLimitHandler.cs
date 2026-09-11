using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// Keeps us under the Discogs 60-requests-a-minute ceiling. Hand-scanning will never
/// approach it; this exists so a runaway loop fails locally instead of earning a 429.
/// Retries and circuit-breaking come from the standard resilience handler that
/// ServiceDefaults applies to every HttpClient.
/// </summary>
internal sealed class DiscogsRateLimitHandler : DelegatingHandler
{
    private readonly RateLimiter _limiter;

    public DiscogsRateLimitHandler(IOptions<DiscogsOptions> options) =>
        _limiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
        {
            PermitLimit = options.Value.RequestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 16,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        });

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var lease = await _limiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            throw new HttpRequestException(
                "Too many Discogs requests are already queued locally. Something is looping.");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _limiter.Dispose();
        }

        base.Dispose(disposing);
    }
}
