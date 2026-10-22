using System.ComponentModel.DataAnnotations;
using SevenBySeven.Modules.Collection.Contracts;

namespace SevenBySeven.Web.Collection;

/// <summary>
/// The shape a Copy's details take on a page. Bound fields need setters, which
/// <see cref="CopyDetails"/> does not have. What the details may hold is checked by
/// <see cref="CopyDetailsValidator"/>, the same rules the API enforces, so the page can say
/// what is wrong beside the field. Only the currency is checked here, because the API drops
/// one it cannot read rather than refusing it. Shared by adding a Copy and editing one, so
/// the two cannot drift apart. Its fields are named as the details' are, which is how a
/// failed rule finds its field.
/// </summary>
public sealed class CopyForm
{
    public ConditionGrade? MediaCondition { get; set; }

    public ConditionGrade? SleeveCondition { get; set; }

    public decimal? PricePaid { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "A currency is three letters, such as GBP.")]
    public string? Currency { get; set; } = "GBP";

    public string? PurchasedFrom { get; set; }

    public string? Location { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Fills the form from a Copy that already exists, for editing. A Copy added by a
    /// Stack arrives here with its condition and price blank — that is the whole reason
    /// this path exists.
    /// </summary>
    public static CopyForm For(CopySheet copy)
    {
        ArgumentNullException.ThrowIfNull(copy);

        var details = copy.Details;

        return new CopyForm
        {
            MediaCondition = details.MediaCondition,
            SleeveCondition = details.SleeveCondition,
            PricePaid = details.PricePaid,
            // A Copy with no price kept no currency either, so offer the usual one back
            // rather than an empty box that looks like a missing answer.
            Currency = details.PricePaidCurrency ?? "GBP",
            PurchasedFrom = details.PurchasedFrom,
            Location = details.Location,
            Notes = details.Notes,
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
