using SevenBySeven.Modules.Gigs.Domain;

namespace SevenBySeven.Tests.Gigs;

public class GigTests
{
    private static Gig AnyGig() => new() { PlayedOn = new DateOnly(2026, 9, 26) };

    [Fact]
    public void Selections_are_numbered_in_the_order_they_are_started()
    {
        var gig = AnyGig();

        var first = gig.AddSelection();
        var second = gig.AddSelection();

        Assert.Equal([first.Id, second.Id], gig.Selections.Select(selection => selection.Id));
        Assert.Equal([0, 1], gig.Selections.Select(selection => selection.Sequence));
    }

    [Fact]
    public void Removing_a_selection_closes_up_the_running_order()
    {
        var gig = AnyGig();
        var first = gig.AddSelection();
        var second = gig.AddSelection();
        var third = gig.AddSelection();

        Assert.True(gig.RemoveSelection(second.Id));

        Assert.Equal([first.Id, third.Id], gig.Selections.Select(selection => selection.Id));
        Assert.Equal([0, 1], gig.Selections.Select(selection => selection.Sequence));
    }

    [Fact]
    public void Removing_a_selection_this_gig_does_not_have_changes_nothing()
    {
        var gig = AnyGig();
        gig.AddSelection();

        Assert.False(gig.RemoveSelection(Guid.CreateVersion7()));
        Assert.Single(gig.Selections);
    }

    [Fact]
    public void Plays_go_on_the_end_of_their_selection()
    {
        var gig = AnyGig();
        var selection = gig.AddSelection();
        var copies = new[] { Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7() };

        foreach (var copy in copies)
        {
            gig.AddPlay(selection.Id, copy);
        }

        Assert.Equal(copies, selection.Plays.Select(play => play.CopyId));
        Assert.Equal([0, 1, 2], selection.Plays.Select(play => play.Sequence));
    }

    [Fact]
    public void A_selection_is_not_held_to_seven()
    {
        var gig = AnyGig();
        var selection = gig.AddSelection();

        for (var count = 0; count < 9; count++)
        {
            Assert.NotNull(gig.AddPlay(selection.Id, Guid.CreateVersion7()));
        }

        Assert.Equal(9, selection.Plays.Count);
    }

    [Fact]
    public void A_play_needs_a_selection_of_this_gig_to_go_in()
    {
        var gig = AnyGig();

        Assert.Null(gig.AddPlay(Guid.CreateVersion7(), Guid.CreateVersion7()));
    }

    [Fact]
    public void Removing_a_play_closes_up_its_selection()
    {
        var gig = AnyGig();
        var selection = gig.AddSelection();
        var first = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;
        var second = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;
        var third = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;

        Assert.True(gig.RemovePlay(second.Id));

        Assert.Equal([first.Id, third.Id], selection.Plays.Select(play => play.Id));
        Assert.Equal([0, 1], selection.Plays.Select(play => play.Sequence));
        Assert.False(gig.RemovePlay(second.Id));
    }

    [Fact]
    public void A_play_moves_one_place_at_a_time_within_its_selection()
    {
        var gig = AnyGig();
        var selection = gig.AddSelection();
        var first = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;
        var second = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;
        var third = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;

        Assert.True(gig.MovePlay(third.Id, earlier: true));
        Assert.Equal([first.Id, third.Id, second.Id], selection.Plays.Select(play => play.Id));

        Assert.True(gig.MovePlay(first.Id, earlier: false));
        Assert.Equal([third.Id, first.Id, second.Id], selection.Plays.Select(play => play.Id));
        Assert.Equal([0, 1, 2], selection.Plays.Select(play => play.Sequence));
    }

    [Fact]
    public void A_play_already_at_the_end_does_not_move_past_it()
    {
        var gig = AnyGig();
        var selection = gig.AddSelection();
        var first = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;
        var last = gig.AddPlay(selection.Id, Guid.CreateVersion7())!;

        Assert.False(gig.MovePlay(first.Id, earlier: true));
        Assert.False(gig.MovePlay(last.Id, earlier: false));
        Assert.False(gig.MovePlay(Guid.CreateVersion7(), earlier: true));
        Assert.Equal([first.Id, last.Id], selection.Plays.Select(play => play.Id));
    }
}
