namespace SevenBySeven.Modules.Catalogue.Domain;

/// <summary>
/// One piece of music at a given position on a Release. Tracks belong to the
/// Release, not to any Copy: every Copy of a pressing has the same tracks, so a
/// BPM is entered once and shared.
/// </summary>
public sealed class Track
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid ReleaseId { get; private set; }

    /// <summary>Position as printed on the record, such as "A1" or "B2".</summary>
    public required string Position { get; init; }

    /// <summary>
    /// Where the track sits in the source's tracklist. Position is what is printed and
    /// does not reliably sort — "A10" comes before "A2" — so running order is kept
    /// as the source gave it rather than derived.
    /// </summary>
    public int Sequence { get; init; }

    public required string Title { get; init; }

    public TimeSpan? Duration { get; init; }

    /// <summary>Tempo, absent until someone supplies it. Fractional values are normal.</summary>
    public decimal? Bpm { get; private set; }

    public BpmSource? BpmSource { get; private set; }

    /// <summary>
    /// Sets the tempo and records where it came from. A catalogue refresh must never
    /// overwrite this: it is the one piece of hand-owned data inside the Catalogue.
    /// </summary>
    public void SetBpm(decimal bpm, BpmSource source)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bpm);
        Bpm = bpm;
        BpmSource = source;
    }

    public void ClearBpm()
    {
        Bpm = null;
        BpmSource = null;
    }
}
