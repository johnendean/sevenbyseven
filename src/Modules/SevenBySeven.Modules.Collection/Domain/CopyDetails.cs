namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// What I know about a Copy at the moment I add it. Every field is optional: a record
/// pulled off the shelf years after buying it still belongs in the Collection, and
/// half-remembered facts are worth less than a wrong one.
/// </summary>
public sealed record CopyDetails
{
    public ConditionGrade? MediaCondition { get; init; }

    public ConditionGrade? SleeveCondition { get; init; }

    public decimal? PricePaid { get; init; }

    /// <summary>ISO 4217, such as "GBP". Kept only when a price is.</summary>
    public string? PricePaidCurrency { get; init; }

    public string? PurchasedFrom { get; init; }

    public string? Location { get; init; }

    public string? Notes { get; init; }

    /// <summary>A Copy added without a word said about it.</summary>
    public static CopyDetails Unknown { get; } = new();
}
