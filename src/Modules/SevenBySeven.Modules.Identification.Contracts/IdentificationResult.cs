namespace SevenBySeven.Modules.Identification.Contracts;

/// <summary>Which piece of evidence produced the candidates.</summary>
public enum IdentificationRoute
{
    /// <summary>Nothing usable was read, so no search happened.</summary>
    None = 0,

    /// <summary>A barcode was decoded. The exact path, when a record has one.</summary>
    Barcode = 1,

    /// <summary>The label's own catalogue number. The best evidence for pre-barcode records.</summary>
    CatalogueNumber = 2,

    /// <summary>Artist and title. Broadest, and the most likely to need a human eye.</summary>
    ArtistAndTitle = 3,
}

/// <summary>
/// The outcome of identifying a Scan. Candidates are always shown for Confirmation —
/// identification is never accepted automatically (docs/adr/0002).
/// </summary>
public sealed record IdentificationResult(
    IReadOnlyList<MatchCandidate> Candidates,
    SleeveDetails Details,
    IdentificationRoute Route)
{
    public bool HasCandidates => Candidates.Count > 0;

    /// <summary>
    /// The search limited to the chosen Format found nothing, so these candidates come
    /// from any vinyl. Discogs entries do not all record their size, so the right Release
    /// can be among them; it is worth saying so before one is confirmed.
    /// </summary>
    public bool IsOutsideFormat { get; init; }

    /// <summary>
    /// A catalogue search could not be made — Discogs unreachable, refusing, or no token
    /// configured. With no candidates this is not "nothing matched": the record may well
    /// be on Discogs, and saying otherwise sends me back to the shelf for nothing.
    /// </summary>
    public bool SearchFailed { get; init; }

    /// <summary>
    /// Why the sleeve could not be read at all, in the vision API's own words, or null when
    /// it was read. A failed reading is not an illegible photograph: reporting it as one
    /// sends me back to the shelf to re-photograph a blameless record.
    /// </summary>
    public string? ReadFailure { get; init; }

    public static IdentificationResult Nothing(SleeveDetails? details = null) =>
        new([], details ?? new SleeveDetails(), IdentificationRoute.None);
}
