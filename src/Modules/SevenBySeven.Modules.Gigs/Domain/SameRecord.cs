namespace SevenBySeven.Modules.Gigs.Domain;

/// <summary>
/// What counts as "the same record" when judging Repeats and play counts: every Copy of
/// one Master, so an original and its reissue are one record to the people listening.
/// Discogs does not give every Release a Master, and without one the Release has to do.
/// </summary>
internal readonly record struct SameRecord
{
    private SameRecord(int? discogsMasterId, Guid releaseId)
    {
        DiscogsMasterId = discogsMasterId;
        ReleaseId = releaseId;
    }

    public int? DiscogsMasterId { get; }

    /// <summary>Empty whenever there is a Master, so that two pressings of it are equal.</summary>
    public Guid ReleaseId { get; }

    public static SameRecord Of(int? discogsMasterId, Guid releaseId) =>
        discogsMasterId is { } master ? new(master, Guid.Empty) : new(null, releaseId);
}
