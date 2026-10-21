using SevenBySeven.Modules.Catalogue.Contracts;

namespace SevenBySeven.Modules.Collection.Contracts;

/// <summary>A Copy as a line in the Collection, with how it stands in my play history.</summary>
public sealed record CopyLine(
    Guid Id,
    DateTimeOffset AddedOn,
    ReleaseLine Release,
    ConditionGrade? MediaCondition,
    ConditionGrade? SleeveCondition,
    string? Location,
    PlayStanding Standing);

/// <summary>One Copy in full: what I know about it, its pressing and that pressing's Tracks.</summary>
public sealed record CopySheet(
    Guid Id,
    DateTimeOffset AddedOn,
    ReleaseSheet Release,
    CopyDetails Details,
    PlayStanding Standing);

/// <summary>
/// A Confirmation: the Release chosen from the Match Candidates, and what I know about my
/// Copy of it. The Release is fetched into the Catalogue first if it is not held already.
/// </summary>
public sealed record AddCopyRequest(int DiscogsReleaseId, CopyDetails Details);
