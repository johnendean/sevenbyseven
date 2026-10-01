using System.ComponentModel.DataAnnotations.Schema;
using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// A single physical record I own. Two identical pressings on the shelf are two
/// Copies of one Release. Everything here is mine — nothing on a Copy is ever
/// touched by a catalogue refresh.
/// </summary>
public sealed class Copy
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>The pressing this is a copy of.</summary>
    public required Guid ReleaseId { get; init; }

    public Release? Release { get; private set; }

    public DateTimeOffset AddedOn { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>Condition of the vinyl itself.</summary>
    public ConditionGrade? MediaCondition { get; set; }

    /// <summary>Condition of the sleeve, which is often graded differently.</summary>
    public ConditionGrade? SleeveCondition { get; set; }

    public decimal? PricePaid { get; set; }

    /// <summary>ISO 4217 code. Only meaningful when a price is set.</summary>
    public string? PricePaidCurrency { get; set; }

    public string? PurchasedFrom { get; set; }

    /// <summary>Where it physically lives — a shelf, a crate, a box in the loft.</summary>
    public string? Location { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// When I parted with this Copy, if I have. A Copy that has been played is kept as a
    /// Former Copy rather than deleted, so the Gigs it was played at still read correctly
    /// (docs/adr/0006). A Former Copy is not part of the Collection.
    /// </summary>
    public DateTimeOffset? PartedWithOn { get; private set; }

    [NotMapped]
    public bool IsFormer => PartedWithOn is not null;

    internal void PartWith(DateTimeOffset when) => PartedWithOn = when;
}
