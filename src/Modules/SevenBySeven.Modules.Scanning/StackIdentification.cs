using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SevenBySeven.Modules.Identification;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// Identifies each Scan of the Stack behind the capture, so the pile can be worked through at
/// the shelf and the confirmations done at the desk. One at a time: a record is up to six
/// Discogs calls when its Format search widens, and the rate limiter queues sixteen before
/// it decides something is looping. Running them in order also settles the list top-down,
/// which is how it is read.
/// </summary>
internal sealed class StackIdentification(
    StackHolder holder,
    IServiceScopeFactory scopes,
    ILogger<StackIdentification> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (scan, format) in holder.Captured.ReadAllAsync(stoppingToken))
        {
            // Given up on while it waited its turn: nobody wants the answer.
            if (scan.IsResolved)
            {
                continue;
            }

            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var identifier = scope.ServiceProvider.GetRequiredService<ISleeveIdentifier>();

                holder.Identified(scan, await identifier.IdentifyAsync(scan.Scan.Image, scan.Scan.ContentType, format, stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Identifying record {Position} of the stack failed.", scan.Position);
                holder.Failed(scan, "Identification failed. The Discogs or Anthropic call may not be configured.");
            }
        }
    }
}
