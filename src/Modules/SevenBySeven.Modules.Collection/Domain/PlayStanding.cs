namespace SevenBySeven.Modules.Collection.Domain;

/// <summary>
/// How a record stands in my play history: how many times it has been played across every
/// Copy of the same Master, the Gig it was last played at, and whether that makes it a
/// Repeat.
/// </summary>
public sealed record PlayStanding(int TimesPlayed, LastPlayed? LastPlayed, Repeat Repeat)
{
    public static PlayStanding NeverPlayed { get; } = new(0, null, Repeat.None);
}

/// <summary>The Gig a record was most recently played at.</summary>
public sealed record LastPlayed(Guid GigId, DateOnly PlayedOn, string? Venue);

public static class PlayStandings
{
    /// <summary>
    /// The record's play history in a sentence: "Played 3 times, last on 26 September 2026
    /// at The Social."
    /// </summary>
    public static string Describe(this PlayStanding standing)
    {
        ArgumentNullException.ThrowIfNull(standing);

        if (standing.LastPlayed is not { } last)
        {
            return "Not played out yet.";
        }

        var times = standing.TimesPlayed == 1 ? "once" : $"{standing.TimesPlayed} times";
        var where = last.Venue is { Length: > 0 } venue ? $" at {venue}" : string.Empty;

        return $"Played {times}, last on {last.PlayedOn:d MMMM yyyy}{where}.";
    }
}
