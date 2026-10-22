using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;

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

    public Guid Id => Scan.Id;

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

    /// <summary>
    /// A Confirmation is under way: the pressing is being fetched and the Copy added. A
    /// second one, from another device, must not add the same record twice.
    /// </summary>
    public bool IsConfirming { get; private set; }

    /// <summary>
    /// Claims this Scan for a Confirmation of one of its own Match Candidates. False when it
    /// is already over, already being confirmed, or the release is not one it was offered.
    /// </summary>
    public bool TryBeginConfirmation(int discogsReleaseId)
    {
        if (IsResolved || IsConfirming || Result?.Candidates.Any(candidate => candidate.DiscogsReleaseId == discogsReleaseId) is not true)
        {
            return false;
        }

        IsConfirming = true;
        return true;
    }

    /// <summary>The Confirmation could not be made. The Scan is untouched, so it can be tried again.</summary>
    public void ConfirmationFailed() => IsConfirming = false;

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
    public void Confirm()
    {
        IsConfirming = false;
        Status = StackedScanStatus.Confirmed;
    }

    /// <summary>Given up on, whether it matched or not.</summary>
    public void Abandon() => Status = StackedScanStatus.Abandoned;
}
