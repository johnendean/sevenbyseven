using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;

namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>
/// A run of Scans captured back to back. A Stack holds its Scans together until each has
/// been confirmed or abandoned, and is then gone: a Stack is no more kept than the Scans in
/// it. There is at most one, held by <see cref="StackHolder"/>.
/// </summary>
public sealed class Stack
{
    private readonly List<StackedScan> _scans = [];

    public Stack(int capacity, Format format = Format.Vinyl)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        Capacity = capacity;
        Format = format;
    }

    public Guid Id { get; } = Guid.CreateVersion7();

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>The most this Stack will take. Fixed when it starts, so raising the
    /// configured limit mid-Stack cannot move the goalposts underneath it.</summary>
    public int Capacity { get; }

    /// <summary>The kind of record every Scan in this Stack is searched as. A claim about
    /// the whole pile, so it is fixed when the Stack starts, as the capacity is.</summary>
    public Format Format { get; }

    public IReadOnlyList<StackedScan> Scans => _scans;

    public int Count => _scans.Count;

    /// <summary>Capture stops here. The limit is about my attention, so it is a real stop.</summary>
    public bool IsFull => _scans.Count >= Capacity;

    public int Remaining => Capacity - _scans.Count;

    /// <summary>Nothing captured yet, so there is nothing to lose by walking away.</summary>
    public bool IsEmpty => _scans.Count == 0;

    /// <summary>Scans still waiting on a Confirmation or an abandonment.</summary>
    public IEnumerable<StackedScan> Pending => _scans.Where(scan => scan.IsPending);

    public int PendingCount => _scans.Count(scan => scan.IsPending);

    public int ConfirmedCount => _scans.Count(scan => scan.Status is StackedScanStatus.Confirmed);

    /// <summary>Every Scan has been dealt with. The Stack has nothing left to say.</summary>
    public bool IsComplete => _scans.Count > 0 && _scans.All(scan => scan.IsResolved);

    /// <summary>
    /// Takes a captured photograph into the Stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Stack is already full.</exception>
    public StackedScan Add(Scan scan)
    {
        ArgumentNullException.ThrowIfNull(scan);

        if (IsFull)
        {
            throw new InvalidOperationException(
                $"This Stack already holds {Capacity} records, which is as many as it takes.");
        }

        var stacked = new StackedScan(scan, _scans.Count + 1);
        _scans.Add(stacked);

        return stacked;
    }

    /// <summary>Gives up on everything still outstanding. Confirmed Scans stay confirmed —
    /// those are Copies now, and abandoning the Stack does not un-add them.</summary>
    public void AbandonRemaining()
    {
        foreach (var scan in Pending)
        {
            scan.Abandon();
        }
    }
}
