using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;
using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Modules.Scanning;

/// <summary>
/// The one Stack in progress, for the whole application, held in memory and nowhere else
/// (docs/adr/0010). Requests read and change it while identification runs behind them, so
/// every touch goes through one lock and every answer is a snapshot taken inside it.
/// </summary>
public sealed class StackHolder(IOptions<ScanningOptions> options, TimeProvider time)
{
    private readonly Lock _gate = new();
    private readonly List<Channel<long>> _watchers = [];
    private readonly Channel<(StackedScan Scan, Format Format)> _captured =
        Channel.CreateUnbounded<(StackedScan, Format)>(new UnboundedChannelOptions { SingleReader = true });

    private Stack? _current;
    private StackDefaults _defaults = StackDefaults.None;
    private DateTimeOffset _touched;
    private long _version;

    public int MaxStackSize => options.Value.MaxStackSize;

    /// <summary>Scans waiting their turn to be identified, oldest first.</summary>
    internal ChannelReader<(StackedScan Scan, Format Format)> Captured => _captured.Reader;

    public StackSheet? Current
    {
        get
        {
            lock (_gate)
            {
                return Sheet();
            }
        }
    }

    /// <summary>
    /// Starts a Stack, letting go of one that has nothing left to say. Null when the one in
    /// progress still has Scans to deal with: those are a pile on someone's desk, and starting
    /// over would lose them without a word.
    /// </summary>
    public StackSheet? Begin(Format format)
    {
        lock (_gate)
        {
            if (_current is { IsEmpty: false, IsComplete: false })
            {
                return null;
            }

            _current = new Stack(options.Value.MaxStackSize, format);
            _defaults = StackDefaults.None;
            Changed();

            return Sheet();
        }
    }

    /// <summary>The Stack is over: whatever it still held is given up on and let go. False when there was none.</summary>
    public bool Discard()
    {
        lock (_gate)
        {
            if (_current is null)
            {
                return false;
            }

            Let();
            return true;
        }
    }

    /// <summary>False when there is no Stack to say it of.</summary>
    public bool SetDefaults(StackDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        lock (_gate)
        {
            if (_current is null)
            {
                return false;
            }

            _defaults = defaults;
            Changed();

            return true;
        }
    }

    /// <summary>Takes a photograph into the Stack and queues it to be identified.</summary>
    public CaptureOutcome Add(Scan scan)
    {
        ArgumentNullException.ThrowIfNull(scan);

        lock (_gate)
        {
            if (_current is not { } stack)
            {
                return CaptureOutcome.NoStack();
            }

            // Two devices can race for the last place, so a full Stack is an answer, not a bug.
            if (stack.IsFull)
            {
                return CaptureOutcome.Full();
            }

            var stacked = stack.Add(scan);
            _captured.Writer.TryWrite((stacked, stack.Format));
            Changed();

            return CaptureOutcome.Added(stacked.ToLine());
        }
    }

    /// <summary>The photograph of one Scan, or null when the Stack holds no such Scan.</summary>
    public Scan? Photograph(Guid scanId)
    {
        lock (_gate)
        {
            return Find(scanId)?.Scan;
        }
    }

    /// <summary>Gives up on one Scan. False when there is no such Scan, or it is already over.</summary>
    public bool Abandon(Guid scanId)
    {
        lock (_gate)
        {
            if (Find(scanId) is not { IsPending: true, IsConfirming: false } scan)
            {
                return false;
            }

            scan.Abandon();
            Changed();

            return true;
        }
    }

    /// <summary>
    /// Claims a Scan for a Confirmation and hands back what the Copy is to say. The Copy is
    /// added outside the lock, so the claim is what stops two devices adding it twice.
    /// </summary>
    public ConfirmationClaim Claim(Guid scanId, int discogsReleaseId)
    {
        lock (_gate)
        {
            if (Find(scanId) is not { } scan)
            {
                return ConfirmationClaim.NoScan();
            }

            if (!scan.TryBeginConfirmation(discogsReleaseId))
            {
                return scan.IsPending && !scan.IsConfirming
                    ? ConfirmationClaim.NotACandidate()
                    : ConfirmationClaim.AlreadyOver();
            }

            Touch();
            return ConfirmationClaim.Claimed(_defaults);
        }
    }

    /// <summary>The Copy exists: the claimed Scan is finished.</summary>
    public void Confirmed(Guid scanId) => Settle(scanId, scan => scan.Confirm());

    /// <summary>The Copy could not be added: the claimed Scan is untouched, and can be tried again.</summary>
    public void Unclaimed(Guid scanId) => Settle(scanId, scan => scan.ConfirmationFailed());

    internal void Identified(StackedScan scan, IdentificationResult result)
    {
        lock (_gate)
        {
            // Say what actually went wrong: a failed call reported as an unreadable
            // photograph sends you back to the shelf for a blameless record.
            if (result.ReadFailure is { } failure)
            {
                scan.Failed(failure);
            }
            else
            {
                scan.Identified(result);
            }

            Changed(touched: false);
        }
    }

    internal void Failed(StackedScan scan, string reason)
    {
        lock (_gate)
        {
            scan.Failed(reason);
            Changed(touched: false);
        }
    }

    /// <summary>
    /// Abandons the Stack if nothing has been done to it for longer than the idle timeout,
    /// which is the only thing that ever lets go of one nobody comes back to. True when it did.
    /// Identification finishing is not something done to it: a pile left to identify
    /// overnight is still a pile left.
    /// </summary>
    public bool AbandonIfIdle()
    {
        lock (_gate)
        {
            if (_current is null || time.GetUtcNow() - _touched < options.Value.StackIdleTimeout)
            {
                return false;
            }

            Let();
            return true;
        }
    }

    /// <summary>
    /// Every change to the Stack from now on, as a version number, starting with the current
    /// one. Changes that land faster than they are read are run together: a watcher wants to
    /// know that something changed, and then reads what it is.
    /// </summary>
    public async IAsyncEnumerable<long> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var changes = Channel.CreateBounded<long>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });

        lock (_gate)
        {
            _watchers.Add(changes);
            changes.Writer.TryWrite(_version);
        }

        try
        {
            await foreach (var version in changes.Reader.ReadAllAsync(cancellationToken))
            {
                yield return version;
            }
        }
        finally
        {
            lock (_gate)
            {
                _watchers.Remove(changes);
            }
        }
    }

    private void Settle(Guid scanId, Action<StackedScan> settle)
    {
        lock (_gate)
        {
            if (Find(scanId) is { } scan)
            {
                settle(scan);
                Changed();
            }
        }
    }

    private StackedScan? Find(Guid scanId) => _current?.Scans.FirstOrDefault(scan => scan.Id == scanId);

    private StackSheet? Sheet() => _current?.ToSheet(_defaults);

    private void Let()
    {
        _current!.AbandonRemaining();
        _current = null;
        _defaults = StackDefaults.None;
        Changed();
    }

    private void Touch() => _touched = time.GetUtcNow();

    private void Changed(bool touched = true)
    {
        if (touched)
        {
            Touch();
        }

        _version++;

        foreach (var watcher in _watchers)
        {
            watcher.Writer.TryWrite(_version);
        }
    }
}

/// <summary>What became of a photograph offered to the Stack.</summary>
public sealed record CaptureOutcome
{
    private CaptureOutcome()
    {
    }

    /// <summary>The Scan taken in, when it was.</summary>
    public StackedScanLine? Scan { get; private init; }

    public bool WasFull { get; private init; }

    public static CaptureOutcome Added(StackedScanLine scan) => new() { Scan = scan };

    public static CaptureOutcome Full() => new() { WasFull = true };

    public static CaptureOutcome NoStack() => new();
}

/// <summary>Whether a Scan could be claimed for a Confirmation, and if so, what the Copy is to say.</summary>
public sealed record ConfirmationClaim
{
    private ConfirmationClaim()
    {
    }

    /// <summary>The Stack's defaults, for the Copy, when the claim was made.</summary>
    public StackDefaults? Defaults { get; private init; }

    public bool IsMissing { get; private init; }

    public bool IsOver { get; private init; }

    public bool IsNotACandidate { get; private init; }

    public static ConfirmationClaim Claimed(StackDefaults defaults) => new() { Defaults = defaults };

    public static ConfirmationClaim NoScan() => new() { IsMissing = true };

    public static ConfirmationClaim AlreadyOver() => new() { IsOver = true };

    public static ConfirmationClaim NotACandidate() => new() { IsNotACandidate = true };
}
