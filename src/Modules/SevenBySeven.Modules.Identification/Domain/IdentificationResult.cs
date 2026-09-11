namespace SevenBySeven.Modules.Identification.Domain;

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

    public static IdentificationResult Nothing(SleeveDetails? details = null) =>
        new([], details ?? new SleeveDetails(), IdentificationRoute.None);
}
