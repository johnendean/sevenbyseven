using System.Globalization;
using SevenBySeven.Modules.Gigs.Features;

namespace SevenBySeven.Tests.Gigs;

public class CalendarMonthTests
{
    [Fact]
    public void A_month_starting_on_a_tuesday_leaves_monday_blank()
    {
        // 1 September 2026 is a Tuesday.
        var first = CalendarMonth.Of(new DateOnly(2026, 9, 26)).Weeks[0];

        Assert.Null(first[0]);
        Assert.Equal(new DateOnly(2026, 9, 1), first[1]);
        Assert.Equal(new DateOnly(2026, 9, 6), first[6]);
    }

    [Fact]
    public void Every_day_of_the_month_appears_once_in_order()
    {
        var days = CalendarMonth.Of(new DateOnly(2026, 9, 1)).Weeks
            .SelectMany(week => week)
            .OfType<DateOnly>()
            .ToList();

        Assert.Equal(30, days.Count);
        Assert.Equal(Enumerable.Range(1, 30), days.Select(day => day.Day));
    }

    [Fact]
    public void A_february_starting_on_a_monday_fills_exactly_four_weeks()
    {
        var weeks = CalendarMonth.Of(new DateOnly(2021, 2, 1)).Weeks;

        Assert.Equal(4, weeks.Count);
        Assert.All(weeks, week => Assert.DoesNotContain(null, week));
    }

    [Fact]
    public void A_month_starting_on_a_sunday_runs_to_six_weeks()
    {
        // 1 November 2026 is a Sunday, so it sits alone in the first row.
        var weeks = CalendarMonth.Of(new DateOnly(2026, 11, 1)).Weeks;

        Assert.Equal(6, weeks.Count);
        Assert.Equal(new DateOnly(2026, 11, 1), weeks[0][6]);
        Assert.Equal(new DateOnly(2026, 11, 30), weeks[5][0]);
        Assert.All(weeks, week => Assert.Equal(7, week.Count));
    }

    [Fact]
    public void Stepping_between_months_crosses_the_year()
    {
        var december = CalendarMonth.Of(new DateOnly(2026, 12, 25));

        Assert.Equal(CalendarMonth.Of(new DateOnly(2027, 1, 1)), december.Next());
        Assert.Equal(CalendarMonth.Of(new DateOnly(2026, 11, 1)), december.Previous());
        Assert.Equal(december, december.Next().Previous());
    }

    [Fact]
    public void A_month_is_titled_by_name_and_year()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-GB");

        try
        {
            Assert.Equal("September 2026", CalendarMonth.Of(new DateOnly(2026, 9, 26)).Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void The_week_is_headed_monday_first()
    {
        Assert.Equal(["Mo", "Tu", "We", "Th", "Fr", "Sa", "Su"], CalendarMonth.DayHeadings);
    }
}
