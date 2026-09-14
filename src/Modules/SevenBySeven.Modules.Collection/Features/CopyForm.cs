using System.ComponentModel.DataAnnotations;
using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Modules.Collection.Features;

/// <summary>
/// The shape a Copy's details take on a page. Bound fields need setters and the lengths
/// the database will accept, neither of which belongs on <see cref="CopyDetails"/>.
/// Shared by adding a Copy and editing one, so the two cannot drift apart.
/// </summary>
public sealed class CopyForm
{
    public ConditionGrade? MediaCondition { get; set; }

    public ConditionGrade? SleeveCondition { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "A price cannot be negative.")]
    public decimal? PricePaid { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "A currency is three letters, such as GBP.")]
    public string? Currency { get; set; } = "GBP";

    [StringLength(200)]
    public string? PurchasedFrom { get; set; }

    [StringLength(200)]
    public string? Location { get; set; }

    [StringLength(4000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Fills the form from a Copy that already exists, for editing. A Copy added by a
    /// Stack arrives here with its condition and price blank — that is the whole reason
    /// this path exists.
    /// </summary>
    public static CopyForm For(Copy copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        return new CopyForm
        {
            MediaCondition = copy.MediaCondition,
            SleeveCondition = copy.SleeveCondition,
            PricePaid = copy.PricePaid,
            // A Copy with no price kept no currency either, so offer the usual one back
            // rather than an empty box that looks like a missing answer.
            Currency = copy.PricePaidCurrency ?? "GBP",
            PurchasedFrom = copy.PurchasedFrom,
            Location = copy.Location,
            Notes = copy.Notes,
        };
    }

    public CopyDetails ToDetails() => new()
    {
        MediaCondition = MediaCondition,
        SleeveCondition = SleeveCondition,
        PricePaid = PricePaid,
        PricePaidCurrency = Currency,
        PurchasedFrom = PurchasedFrom,
        Location = Location,
        Notes = Notes,
    };
}
