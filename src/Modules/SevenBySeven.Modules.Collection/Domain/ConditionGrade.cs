namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// The Goldmine grading standard, as used by Discogs and by record dealers generally.
/// Ordered worst to best so grades can be compared and sorted.
/// </summary>
public enum ConditionGrade
{
    Poor = 1,
    Fair = 2,
    Good = 3,
    GoodPlus = 4,
    VeryGood = 5,
    VeryGoodPlus = 6,
    NearMint = 7,
    Mint = 8,
}
