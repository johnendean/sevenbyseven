using System.Globalization;

namespace SevenBySeven.Modules.Gigs.Features;

/// <summary>
/// One month laid out as a calendar page: weeks starting on Monday, with the days that
/// belong to the months either side left blank rather than shown greyed out.
/// </summary>
public sealed record CalendarMonth
{
    private CalendarMonth(int year, int month)
    {
        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public static CalendarMonth Of(DateOnly date) => new(date.Year, date.Month);

    public DateOnly First => new(Year, Month, 1);

    /// <summary>"September 2026".</summary>
    public string Title => First.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    public CalendarMonth Previous() => Of(First.AddMonths(-1));

    public CalendarMonth Next() => Of(First.AddMonths(1));

    /// <summary>
    /// The month in rows of seven, Monday first. Four rows for a February that starts on a
    /// Monday, six for a month that starts late in the week; null where a day is not this
    /// month's.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<DateOnly?>> Weeks
    {
        get
        {
            // DayOfWeek counts from Sunday; a Monday-first week counts from Monday.
            var leading = ((int)First.DayOfWeek + 6) % 7;
            var days = DateTime.DaysInMonth(Year, Month);

            var cells = Enumerable.Repeat<DateOnly?>(null, leading)
                .Concat(Enumerable.Range(0, days).Select(offset => (DateOnly?)First.AddDays(offset)))
                .ToList();

            while (cells.Count % 7 != 0)
            {
                cells.Add(null);
            }

            return [.. cells.Chunk(7).Select(week => (IReadOnlyList<DateOnly?>)week)];
        }
    }

    /// <summary>"Mo", "Tu" … "Su", Monday first, to head the columns.</summary>
    public static IReadOnlyList<string> DayHeadings { get; } =
        [.. Enumerable.Range(0, 7)
            .Select(offset => new DateOnly(2021, 2, 1).AddDays(offset))
            .Select(day => day.ToString("ddd", CultureInfo.InvariantCulture)[..2])];
}
