using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// Lets go of a Stack nobody has come back to. Closing a tab used to do this; with the Stack
/// in the API, nothing else would ever free its photographs (docs/adr/0010).
/// </summary>
internal sealed class StackSweep(StackHolder holder, TimeProvider time, ILogger<StackSweep> logger) : BackgroundService
{
    /// <summary>Often enough that an idle Stack goes within a minute of its timeout.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval, time);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (holder.AbandonIfIdle())
                {
                    logger.LogInformation("The stack was left untouched past its idle timeout and has been abandoned.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
    }
}
