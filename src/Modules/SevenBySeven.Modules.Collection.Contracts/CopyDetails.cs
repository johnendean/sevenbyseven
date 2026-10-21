using System.ComponentModel.DataAnnotations;

namespace SevenBySeven.Modules.Collection.Contracts;

/// <summary>
/// What I know about a Copy at the moment I add it. Every field is optional: a record
/// pulled off the shelf years after buying it still belongs in the Collection, and
/// half-remembered facts are worth less than a wrong one. The limits are the ones the
/// database holds, and the API refuses anything past them before it is saved.
/// </summary>
public sealed record CopyDetails
{
    public ConditionGrade? MediaCondition { get; init; }

    public ConditionGrade? SleeveCondition { get; init; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "A price cannot be negative, or more than a million.")]
    public decimal? PricePaid { get; init; }

    /// <summary>ISO 4217, such as "GBP". Kept only when a price is.</summary>
    public string? PricePaidCurrency { get; init; }

    [StringLength(200)]
    public string? PurchasedFrom { get; init; }

    [StringLength(200)]
    public string? Location { get; init; }

    [StringLength(4000)]
    public string? Notes { get; init; }

    /// <summary>A Copy added without a word said about it.</summary>
    public static CopyDetails Unknown { get; } = new();
}
