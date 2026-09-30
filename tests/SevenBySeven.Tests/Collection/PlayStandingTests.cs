using System.Globalization;
using SevenBySeven.Modules.Collection.Domain;

namespace SevenBySeven.Tests.Collection;

public class PlayStandingTests
{
    private static readonly Guid AnyGig = Guid.CreateVersion7();

    [Fact]
    public void A_record_never_played_says_so()
    {
        Assert.Equal("Not played out yet.", PlayStanding.NeverPlayed.Describe());
    }

    [Fact]
    public void A_record_played_says_how_often_and_where_last()
    {
        using var english = new CultureScope("en-GB");
        var standing = new PlayStanding(3, new LastPlayed(AnyGig, new DateOnly(2026, 9, 26), "The Social"), Repeat.LastGig);

        Assert.Equal("Played 3 times, last on 26 September 2026 at The Social.", standing.Describe());
    }

    [Fact]
    public void Once_is_once_and_a_gig_with_no_venue_has_no_at()
    {
        using var english = new CultureScope("en-GB");
        var standing = new PlayStanding(1, new LastPlayed(AnyGig, new DateOnly(2026, 9, 26), null), Repeat.None);

        Assert.Equal("Played once, last on 26 September 2026.", standing.Describe());
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        public CultureScope(string name) => CultureInfo.CurrentCulture = new CultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
