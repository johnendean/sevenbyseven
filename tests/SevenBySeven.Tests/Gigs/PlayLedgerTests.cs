using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs;
using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Tests.Gigs;

public class PlayLedgerTests
{
    private static readonly SameRecord KindOfBlue = SameRecord.Of(5460, Guid.CreateVersion7());
    private static readonly SameRecord Other = SameRecord.Of(1234, Guid.CreateVersion7());

    [Fact]
    public void A_record_never_played_stands_as_never_played()
    {
        var ledger = new PlayLedger([Gig(1)], [], window: 3);

        Assert.Same(PlayStanding.NeverPlayed, ledger.StandingOf(KindOfBlue));
    }

    [Fact]
    public void Played_at_the_last_gig_is_the_pointed_repeat()
    {
        var earlier = Gig(1);
        var last = Gig(8);
        var ledger = new PlayLedger([earlier, last], [PlayAt(last, KindOfBlue)], window: 3);

        var standing = ledger.StandingOf(KindOfBlue);

        Assert.Equal(Repeat.LastGig, standing.Repeat);
        Assert.Equal(1, standing.TimesPlayed);
        Assert.Equal(new LastPlayed(last.Id, last.PlayedOn, last.Venue), standing.LastPlayed);
    }

    [Fact]
    public void Played_within_the_window_but_not_last_time_out_is_a_lesser_repeat()
    {
        var gigs = new[] { Gig(1), Gig(2), Gig(3) };
        var ledger = new PlayLedger(gigs, [PlayAt(gigs[0], KindOfBlue)], window: 3);

        Assert.Equal(Repeat.Recent, ledger.StandingOf(KindOfBlue).Repeat);
    }

    [Fact]
    public void Played_before_the_window_is_no_repeat_but_still_counts()
    {
        var gigs = new[] { Gig(1), Gig(2), Gig(3), Gig(4) };
        var ledger = new PlayLedger(gigs, [PlayAt(gigs[0], KindOfBlue)], window: 3);

        var standing = ledger.StandingOf(KindOfBlue);

        Assert.Equal(Repeat.None, standing.Repeat);
        Assert.Equal(1, standing.TimesPlayed);
        Assert.Equal(gigs[0].Id, standing.LastPlayed?.GigId);
    }

    [Fact]
    public void Gigs_fall_in_the_order_they_were_played_not_entered()
    {
        // The Gig from a fortnight ago, entered after last night's, is still not the last Gig.
        var lastNight = Gig(20);
        var fortnightAgo = Gig(6);
        var ledger = new PlayLedger([lastNight, fortnightAgo], [PlayAt(fortnightAgo, KindOfBlue)], window: 3);

        var standing = ledger.StandingOf(KindOfBlue);

        Assert.Equal(Repeat.Recent, standing.Repeat);
        Assert.Equal(fortnightAgo.Id, standing.LastPlayed?.GigId);
    }

    [Fact]
    public void Every_play_of_the_record_counts_and_the_latest_is_the_last()
    {
        var gigs = new[] { Gig(1), Gig(2), Gig(3) };
        var ledger = new PlayLedger(
            gigs,
            [PlayAt(gigs[0], KindOfBlue), PlayAt(gigs[1], KindOfBlue), PlayAt(gigs[1], KindOfBlue, selection: 1), PlayAt(gigs[2], Other)],
            window: 3);

        var standing = ledger.StandingOf(KindOfBlue);

        Assert.Equal(3, standing.TimesPlayed);
        Assert.Equal(gigs[1].Id, standing.LastPlayed?.GigId);
        Assert.Equal(Repeat.Recent, standing.Repeat);
    }

    [Fact]
    public void A_gig_is_judged_against_the_gigs_before_it_only()
    {
        var gigs = new[] { Gig(1), Gig(2), Gig(3) };
        var ledger = new PlayLedger(gigs, [PlayAt(gigs[1], KindOfBlue), PlayAt(gigs[2], KindOfBlue)], window: 3);

        Assert.Equal(Repeat.None, ledger.RepeatAt(KindOfBlue, gigs[1].Id));
        Assert.Equal(Repeat.LastGig, ledger.RepeatAt(KindOfBlue, gigs[2].Id));
    }

    [Fact]
    public void The_first_gig_ever_has_nothing_to_repeat()
    {
        var first = Gig(1);
        var ledger = new PlayLedger([first], [PlayAt(first, KindOfBlue)], window: 3);

        Assert.Equal(Repeat.None, ledger.RepeatAt(KindOfBlue, first.Id));
    }

    [Fact]
    public void A_gig_not_yet_recorded_is_judged_as_the_next_one()
    {
        var last = Gig(1);
        var ledger = new PlayLedger([last], [PlayAt(last, KindOfBlue)], window: 3);

        Assert.Equal(Repeat.LastGig, ledger.RepeatAt(KindOfBlue, Guid.CreateVersion7()));
    }

    [Fact]
    public void The_window_is_counted_in_gigs()
    {
        var gigs = new[] { Gig(1), Gig(2) };
        var narrow = new PlayLedger(gigs, [PlayAt(gigs[0], KindOfBlue)], window: 1);
        var wide = new PlayLedger(gigs, [PlayAt(gigs[0], KindOfBlue)], window: 2);

        Assert.Equal(Repeat.None, narrow.StandingOf(KindOfBlue).Repeat);
        Assert.Equal(Repeat.Recent, wide.StandingOf(KindOfBlue).Repeat);
    }

    [Fact]
    public void Earlier_in_the_same_gig_means_earlier_in_its_running_order()
    {
        var gig = Gig(1);
        var ledger = new PlayLedger(
            [gig],
            [PlayAt(gig, KindOfBlue, selection: 0, position: 3), PlayAt(gig, KindOfBlue, selection: 2, position: 0)],
            window: 3);

        Assert.False(ledger.PlayedEarlierAt(KindOfBlue, gig.Id, selectionSequence: 0, playSequence: 3));
        Assert.True(ledger.PlayedEarlierAt(KindOfBlue, gig.Id, selectionSequence: 1, playSequence: 0));
        Assert.True(ledger.PlayedEarlierAt(KindOfBlue, gig.Id, selectionSequence: 2, playSequence: 0));
        Assert.True(ledger.PlayedAt(KindOfBlue, gig.Id));
        Assert.False(ledger.PlayedAt(Other, gig.Id));
    }

    [Fact]
    public void A_play_at_a_gig_the_ledger_never_read_is_left_out()
    {
        // Gigs and Plays are read separately; another tab can record a Gig and its first
        // Play in between, leaving a Play whose Gig is missing.
        var known = Gig(1);
        var ledger = new PlayLedger([known], [PlayAt(Gig(2), KindOfBlue)], window: 3);

        Assert.Same(PlayStanding.NeverPlayed, ledger.StandingOf(KindOfBlue));
        Assert.Equal(Repeat.None, ledger.RepeatAt(KindOfBlue, known.Id));
    }

    [Fact]
    public void Two_pressings_of_one_master_are_the_same_record()
    {
        Assert.Equal(SameRecord.Of(5460, Guid.CreateVersion7()), SameRecord.Of(5460, Guid.CreateVersion7()));
    }

    [Fact]
    public void Without_a_master_only_the_same_pressing_is_the_same_record()
    {
        var release = Guid.CreateVersion7();

        Assert.Equal(SameRecord.Of(null, release), SameRecord.Of(null, release));
        Assert.NotEqual(SameRecord.Of(null, release), SameRecord.Of(null, Guid.CreateVersion7()));
    }

    private static GigFact Gig(int day) => new(Guid.CreateVersion7(), new DateOnly(2026, 9, day), $"Venue {day}");

    private static PlayFact PlayAt(GigFact gig, SameRecord record, int selection = 0, int position = 0) =>
        new(gig.Id, selection, position, Guid.CreateVersion7(), record);
}
