namespace SevenBySeven.Modules.Identification.Domain;

/// <summary>
/// The kind of vinyl record a search is limited to. An LP is its own Format even though
/// it is twelve inches: Discogs lists LPs by that word and gives no size, so
/// <see cref="TwelveInch"/> finds only singles and maxi-singles.
/// </summary>
public enum Format
{
    /// <summary>Any vinyl record. The default, and what every search used before.</summary>
    Vinyl = 0,

    SevenInch = 1,

    LP = 2,

    TwelveInch = 3,

    TenInch = 4,
}
