using SevenBySeven.Modules.Identification.Domain;

namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>
/// One Scan of a Stack, and what became of it. A Scan captured into a Stack is still an
/// ordinary Scan: it ends at Confirmation or abandonment, and the photograph goes with it.
/// The only difference is that it waits its turn alongside the others.
/// </summary>
public sealed class StackedScan
{
    public StackedScan(Scan scan, int position)
    {
        ArgumentNullException.ThrowIfNull(scan);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(position);

        Scan = scan;
        Position = position;
    }

    public Scan Scan { get; }

    /// <summary>Where it came in the Stack, counting from one. Shown, so it can be talked about.</summary>
    public int Position { get; }

    public StackedScanStatus Status { get; private set; } = StackedScanStatus.Identifying;

    /// <summary>The candidates to choose between, once identification has run.</summary>
    public IdentificationResult? Result { get; private set; }

    /// <summary>Why identification could not run, when that is what happened.</summary>
    public string? Failure { get; private set; }

    /// <summary>A resolved Scan is over: it has either become a Copy or been given up on.</summary>
    public bool IsResolved => Status is StackedScanStatus.Confirmed or StackedScanStatus.Abandoned;

    /// <summary>Still wants something from me.</summary>
    public bool IsPending => !IsResolved;

    /// <summary>True once identification has finished, however it finished.</summary>
    public bool IsIdentified => Status is not StackedScanStatus.Identifying;

    /// <summary>Identification finished. Whether it found anything decides where this lands.</summary>
    public void Identified(IdentificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (IsResolved)
        {
            return;
        }

        Result = result;
        Status = result.HasCandidates ? StackedScanStatus.Matched : StackedScanStatus.NoMatch;
    }

    /// <summary>
    /// Identification could not run. Kept apart from a plain no-match: one means this
    /// record is hard to place, the other means nothing was asked of Discogs at all, and
    /// telling me the second is the first sends me back to the shelf for no reason.
    /// </summary>
    public void Failed(string reason)
    {
        if (IsResolved)
        {
            return;
        }

        Failure = string.IsNullOrWhiteSpace(reason) ? "Identification failed." : reason;
        Status = StackedScanStatus.Unreadable;
    }

    /// <summary>Confirmation: a Copy now exists, so this Scan is finished.</summary>
    public void Confirm() => Status = StackedScanStatus.Confirmed;

    /// <summary>Given up on, whether it matched or not.</summary>
    public void Abandon() => Status = StackedScanStatus.Abandoned;
}
