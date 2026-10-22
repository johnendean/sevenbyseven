using SevenBySeven.Modules.Scanning.Contracts;

namespace SevenBySeven.Web.Scanning;

/// <summary>
/// The Stack in progress as one circuit sees it, kept current by the API's event stream. It
/// can change from anywhere — another device capturing, identification finishing in the API —
/// so everything that shows it watches this rather than asking once. Scoped: one per circuit,
/// shared by the Stack page and the header that says a Stack is under way.
/// </summary>
public sealed class StackWatch(StackClient stacks, ILogger<StackWatch> logger) : IAsyncDisposable
{
    /// <summary>How long to wait before listening again after the stream drops.</summary>
    private static readonly TimeSpan Reconnect = TimeSpan.FromSeconds(3);

    private readonly CancellationTokenSource _stopping = new();

    // One read at a time. The listener and the page both read after a change, and a read
    // that started earlier must not land later and put back what was there before.
    private readonly SemaphoreSlim _reading = new(1, 1);
    private Task? _listening;

    /// <summary>The Stack as it was last read. Null when there is none, or before the first read.</summary>
    public StackSheet? Current { get; private set; }

    /// <summary>The Stack has been read at least once, so a null Current means there is none.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>Raised off the render thread; a component re-renders through InvokeAsync.</summary>
    public event Action? Changed;

    /// <summary>Starts following the Stack, if this circuit is not already.</summary>
    public void Start() => _listening ??= ListenAsync(_stopping.Token);

    /// <summary>Reads the Stack now, after a change made from here, rather than waiting for its event.</summary>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _reading.WaitAsync(cancellationToken);

        try
        {
            Current = await stacks.CurrentAsync(cancellationToken);
            IsLoaded = true;
        }
        finally
        {
            _reading.Release();
        }

        Changed?.Invoke();
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // The first event is the Stack as it is now, so each connection starts from a
                // fresh read, and every change after it is read as it lands.
                await foreach (var _ in stacks.WatchAsync(cancellationToken))
                {
                    await RefreshAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lost the stack's event stream; listening again shortly.");
            }

            try
            {
                await Task.Delay(Reconnect, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();

        if (_listening is not null)
        {
            await _listening;
        }

        _stopping.Dispose();
        _reading.Dispose();
    }
}
