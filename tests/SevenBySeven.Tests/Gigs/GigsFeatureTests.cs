using SevenBySeven.Modules.Collection.Domain;
using SevenBySeven.Modules.Gigs;
using SevenBySeven.Modules.Gigs.Domain;
using SevenBySeven.Modules.Gigs.Features;

namespace SevenBySeven.Tests.Gigs;

public class GigsFeatureTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-4, 1)]
    [InlineData(5, 5)]
    [InlineData(500, GigsOptions.HighestRepeatWindow)]
    public void The_repeat_window_always_includes_the_last_gig(int configured, int expected)
    {
        Assert.Equal(expected, new GigsOptions { RepeatWindow = configured }.RepeatWindow);
    }

    [Fact]
    public void The_repeat_window_defaults_to_three_gigs()
    {
        Assert.Equal(3, new GigsOptions().RepeatWindow);
    }

    [Theory]
    [InlineData("miles blue", true)]
    [InlineData("BLUE miles", true)]
    [InlineData("columbia cs 8163", true)]
    [InlineData("coltrane", false)]
    [InlineData("miles coltrane", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void A_record_is_found_by_every_word_typed(string? text, bool found)
    {
        Assert.Equal(found, CopySearch.Matches(TestDatabase.AnyRelease(), text));
    }

    [Fact]
    public void A_new_gig_is_dated_the_day_it_is_entered()
    {
        var today = new DateOnly(2026, 9, 30);

        Assert.Equal(today, GigForm.Tonight(today).ToDetails().PlayedOn);
    }

    [Fact]
    public void Editing_a_gig_starts_from_what_is_recorded()
    {
        var sheet = new GigSheet(Guid.CreateVersion7(), new DateOnly(2026, 9, 26), "The Social", "Rammed", []);

        var details = GigForm.For(sheet).ToDetails();

        Assert.Equal(new GigDetails { PlayedOn = sheet.PlayedOn, Venue = "The Social", Notes = "Rammed" }, details);
    }

    [Fact]
    public void A_gig_with_no_date_is_not_details_yet()
    {
        Assert.Throws<InvalidOperationException>(() => new GigForm().ToDetails());
    }

    [Theory]
    [InlineData(Repeat.LastGig, "Played at the last gig", "Last gig")]
    [InlineData(Repeat.Recent, "Played at a recent gig", "Recent")]
    [InlineData(Repeat.None, "", "")]
    public void A_repeat_says_how_recent_it_is(Repeat repeat, string described, string abbreviated)
    {
        Assert.Equal(described, repeat.Describe());
        Assert.Equal(abbreviated, repeat.Abbreviation());
    }

    [Fact]
    public void A_sheet_counts_every_play_across_its_selections()
    {
        var release = TestDatabase.AnyRelease();
        PlayLine Line(int number) => new(Guid.CreateVersion7(), number, Guid.CreateVersion7(), release, false, Repeat.None, false);

        var sheet = new GigSheet(
            Guid.CreateVersion7(),
            new DateOnly(2026, 9, 26),
            null,
            null,
            [new SelectionSheet(Guid.CreateVersion7(), 1, [Line(1), Line(2)]), new SelectionSheet(Guid.CreateVersion7(), 2, [Line(1)])]);

        Assert.Equal(3, sheet.Plays);
    }
}
