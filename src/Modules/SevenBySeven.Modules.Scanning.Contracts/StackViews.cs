using SevenBySeven.Modules.Identification.Contracts;

namespace SevenBySeven.Modules.Scanning.Contracts;

/// <summary>
/// The Stack in progress, as it stands. There is at most one, wherever it is looked at from
/// (docs/adr/0010). The photographs are not here: each is fetched on its own, once.
/// </summary>
public sealed record StackSheet(
    Guid Id,
    int Capacity,
    Format Format,
    StackDefaults Defaults,
    IReadOnlyList<StackedScanLine> Scans)
{
    public int Count => Scans.Count;

    /// <summary>Capture stops here.</summary>
    public bool IsFull => Scans.Count >= Capacity;

    /// <summary>Nothing captured yet, so there is nothing to lose by walking away.</summary>
    public bool IsEmpty => Scans.Count == 0;

    public int PendingCount => Scans.Count(scan => scan.IsPending);

    public int ConfirmedCount => Scans.Count(scan => scan.Status is StackedScanStatus.Confirmed);

    /// <summary>Every Scan has been dealt with. The Stack has nothing left to say.</summary>
    public bool IsComplete => Scans.Count > 0 && Scans.All(scan => !scan.IsPending);

    /// <summary>Worth telling me about before I lose it.</summary>
    public bool HasUnresolvedWork => !IsEmpty && !IsComplete;
}

/// <summary>One Scan of the Stack, and what has become of it so far.</summary>
public sealed record StackedScanLine(
    Guid Id,
    int Position,
    StackedScanStatus Status,
    IdentificationResult? Result,
    string? Failure)
{
    /// <summary>Still wants something from me: it has neither become a Copy nor been given up on.</summary>
    public bool IsPending => Status is not (StackedScanStatus.Confirmed or StackedScanStatus.Abandoned);

    /// <summary>True once identification has finished, however it finished.</summary>
    public bool IsIdentified => Status is not StackedScanStatus.Identifying;
}

/// <summary>Starts a Stack of records of one Format. How many it takes is the API's to say.</summary>
public sealed record BeginStackRequest(Format Format);

/// <summary>A Confirmation from a Stack: one of the Scan's own Match Candidates, by its Discogs id.</summary>
public sealed record ConfirmScanRequest(int DiscogsReleaseId);

/// <summary>How many records a Stack may hold, which is set where the photographs are held.</summary>
public sealed record StackLimits(int MaxStackSize);
