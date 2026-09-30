namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// Whether playing a record now would be a Repeat, and how pointedly. A Repeat is
/// flagged, never refused: bringing a record back can be deliberate.
/// </summary>
public enum Repeat
{
    /// <summary>Not played at any of my recent Gigs.</summary>
    None,

    /// <summary>Played at one of my recent Gigs, but not the last one.</summary>
    Recent,

    /// <summary>Played at my last Gig — the one to avoid most.</summary>
    LastGig,
}

public static class Repeats
{
    public static string Describe(this Repeat repeat) => repeat switch
    {
        Repeat.LastGig => "Played at the last gig",
        Repeat.Recent => "Played at a recent gig",
        _ => string.Empty,
    };

    /// <summary>A short badge for a listing, where the full description will not fit.</summary>
    public static string Abbreviation(this Repeat repeat) => repeat switch
    {
        Repeat.LastGig => "Last gig",
        Repeat.Recent => "Recent",
        _ => string.Empty,
    };
}
