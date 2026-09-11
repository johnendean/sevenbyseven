namespace SevenBySeven.Modules.Catalogue.Domain;

/// <summary>
/// When a Release came out, at whatever precision the catalogue actually knows.
/// Older pressings are frequently known only to the year, so month and day are
/// optional rather than invented.
/// </summary>
public sealed record ReleaseDate(int Year, int? Month, int? Day)
{
    public static ReleaseDate ForYear(int year) => new(year, null, null);

    public override string ToString() => (Month, Day) switch
    {
        (null, _) => Year.ToString(),
        ({ } m, null) => $"{Year}-{m:00}",
        ({ } m, { } d) => $"{Year}-{m:00}-{d:00}",
    };
}
