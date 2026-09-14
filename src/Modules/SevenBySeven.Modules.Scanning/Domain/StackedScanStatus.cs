namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>Where a Scan in a Stack has got to.</summary>
public enum StackedScanStatus
{
    /// <summary>Captured, and the identification is still running.</summary>
    Identifying = 0,

    /// <summary>Match Candidates came back and are waiting on a Confirmation.</summary>
    Matched = 1,

    /// <summary>Identification ran and found nothing to offer.</summary>
    NoMatch = 2,

    /// <summary>Identification could not run at all — an unreadable photograph, or a call that failed.</summary>
    Unreadable = 3,

    /// <summary>A Match Candidate was chosen and a Copy exists. The Scan is over.</summary>
    Confirmed = 4,

    /// <summary>Given up on. The Scan is over and nothing was added.</summary>
    Abandoned = 5,
}
