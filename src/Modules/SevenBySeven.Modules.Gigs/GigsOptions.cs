namespace SevenBySeven.Modules.Gigs;

/// <summary>
/// How Repeats are judged. Bound from configuration, so the window can be widened with
/// <c>Gigs__RepeatWindow</c> without a code change.
/// </summary>
public sealed class GigsOptions
{
    public const string SectionName = "Gigs";

    public const int DefaultRepeatWindow = 3;

    /// <summary>Beyond a year of weekly Gigs, "recent" has stopped meaning anything.</summary>
    public const int HighestRepeatWindow = 52;

    private int _repeatWindow = DefaultRepeatWindow;

    /// <summary>
    /// How many of my most recent Gigs a record counts as a Repeat for having been played
    /// at. Counted in Gigs rather than weeks, because "last time out" is what matters and
    /// a quiet spell should not make the last Gig any less recent. Clamped, so the last
    /// Gig is always inside it.
    /// </summary>
    public int RepeatWindow
    {
        get => _repeatWindow;
        set => _repeatWindow = Math.Clamp(value, 1, HighestRepeatWindow);
    }
}
