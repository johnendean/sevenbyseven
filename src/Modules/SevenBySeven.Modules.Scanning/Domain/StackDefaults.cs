namespace SevenBySeven.Modules.Scanning.Domain;

/// <summary>
/// The facts that are usually true of a whole Stack at once. A pile of records brought
/// home together came from one shop and goes on one shelf, so asking ten times is nine
/// times too many. Everything that genuinely varies record by record — condition, price —
/// is deliberately absent, and so is currency: a Copy added without a price cannot
/// carry one, so offering the field would be offering nothing.
/// </summary>
public sealed record StackDefaults
{
    public string? PurchasedFrom { get; init; }

    public string? Location { get; init; }

    /// <summary>A Stack with nothing said about it.</summary>
    public static StackDefaults None { get; } = new();
}
