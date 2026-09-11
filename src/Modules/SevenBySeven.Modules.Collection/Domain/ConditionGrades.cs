namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// How Goldmine grades are written down. Dealers and Discogs both use the
/// abbreviations, so those are what a listing shows; the words are for the form
/// where you are choosing one.
/// </summary>
public static class ConditionGrades
{
    /// <summary>Best first, which is the order a grading dropdown should offer.</summary>
    public static IReadOnlyList<ConditionGrade> BestFirst { get; } =
        [.. Enum.GetValues<ConditionGrade>().OrderDescending()];

    public static string Abbreviation(this ConditionGrade grade) => grade switch
    {
        ConditionGrade.Mint => "M",
        ConditionGrade.NearMint => "NM",
        ConditionGrade.VeryGoodPlus => "VG+",
        ConditionGrade.VeryGood => "VG",
        ConditionGrade.GoodPlus => "G+",
        ConditionGrade.Good => "G",
        ConditionGrade.Fair => "F",
        ConditionGrade.Poor => "P",
        _ => grade.ToString(),
    };

    public static string Describe(this ConditionGrade grade) => grade switch
    {
        ConditionGrade.Mint => "Mint",
        ConditionGrade.NearMint => "Near Mint",
        ConditionGrade.VeryGoodPlus => "Very Good Plus",
        ConditionGrade.VeryGood => "Very Good",
        ConditionGrade.GoodPlus => "Good Plus",
        ConditionGrade.Good => "Good",
        ConditionGrade.Fair => "Fair",
        ConditionGrade.Poor => "Poor",
        _ => grade.ToString(),
    };

    /// <summary>"Very Good Plus (VG+)" — both, for choosing between them.</summary>
    public static string Label(this ConditionGrade grade) => $"{grade.Describe()} ({grade.Abbreviation()})";
}
