using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Identification.Discogs;

/// <summary>
/// Turns one Discogs release into a Release the Catalogue can store. Kept apart from
/// the HTTP client so the awkward parts — partial dates, durations, a tracklist that
/// also carries headings — can be tested without a network.
/// </summary>
internal static class DiscogsReleaseMapper
{
    /// <summary>Outside these, a "year" is a typo or a placeholder rather than a date.</summary>
    internal static bool IsPlausibleYear(int year) => year is > 1850 and < 2200;

    public static Release ToRelease(DiscogsReleaseDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var label = detail.Labels?.FirstOrDefault();

        var release = new Release
        {
            DiscogsReleaseId = detail.Id,
            DiscogsMasterId = detail.MasterId is > 0 ? detail.MasterId : null,
            Title = Clip(detail.Title, 500) ?? string.Empty,
            ArtistName = Clip(ArtistOf(detail), 500) ?? string.Empty,
            LabelName = Clip(label?.Name, 300),
            CatalogueNumber = Clip(label?.CatalogueNumber, 100),
            Country = Clip(detail.Country, 100),
            Released = ParseReleaseDate(detail.Released, detail.Year),
            FormatDescription = Clip(FormatOf(detail.Formats), 300),
            Barcode = Clip(BarcodeOf(detail.Identifiers), 64),
            Genres = [.. detail.Genres ?? []],
            Styles = [.. detail.Styles ?? []],
            ThumbnailUrl = ThumbnailOf(detail),
            CoverImageUrl = CoverOf(detail),
        };

        release.ReplaceTracks(TracksOf(detail.Tracklist));

        return release;
    }

    /// <summary>
    /// Discogs disambiguates artists who share a name with a trailing number — the
    /// second Nirvana is "Nirvana (2)". That is a fact about their database, not about
    /// the record, so it does not belong on a sleeve in my collection.
    /// </summary>
    internal static string WithoutDisambiguator(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (!trimmed.EndsWith(')'))
        {
            return trimmed;
        }

        var opened = trimmed.LastIndexOf('(');

        // A name that is nothing but the bracket keeps it: dropping it leaves nothing.
        if (opened <= 0)
        {
            return trimmed;
        }

        var inside = trimmed[(opened + 1)..^1];

        return inside.Length > 0 && inside.All(char.IsAsciiDigit)
            ? trimmed[..opened].TrimEnd()
            : trimmed;
    }

    /// <summary>
    /// Reads whatever precision the date actually has. Pre-1980 pressings are commonly
    /// known only to the year, and Discogs pads the rest with zeroes rather than
    /// omitting it, so "1959-00-00" is a year and not a January date.
    /// </summary>
    internal static ReleaseDate? ParseReleaseDate(string? released, int? year)
    {
        var parts = (released ?? string.Empty).Split('-', StringSplitOptions.TrimEntries);

        if (int.TryParse(parts[0], out var parsedYear) && IsPlausibleYear(parsedYear))
        {
            var month = parts.Length > 1 && int.TryParse(parts[1], out var m) && m is >= 1 and <= 12
                ? m
                : (int?)null;

            // A day without a month is not a date anyone can use.
            var day = month is not null
                && parts.Length > 2
                && int.TryParse(parts[2], out var d)
                && d is >= 1 and <= 31
                    ? d
                    : (int?)null;

            return new ReleaseDate(parsedYear, month, day);
        }

        return year is { } fallback && IsPlausibleYear(fallback) ? ReleaseDate.ForYear(fallback) : null;
    }

    /// <summary>"3:22", or "1:02:33" when a side is one piece. Anything else is no duration.</summary>
    internal static TimeSpan? ParseDuration(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
        {
            return null;
        }

        var parts = duration.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length is < 2 or > 3)
        {
            return null;
        }

        var numbers = new int[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0)
            {
                return null;
            }
        }

        var (hours, minutes, seconds) = parts.Length == 3
            ? (numbers[0], numbers[1], numbers[2])
            : (0, numbers[0], numbers[1]);

        if (minutes > 59 || seconds > 59)
        {
            return null;
        }

        var length = new TimeSpan(hours, minutes, seconds);

        // Discogs writes "0:00" where it means "we don't know".
        return length == TimeSpan.Zero ? null : length;
    }

    /// <summary>
    /// Tracks only, in the order Discogs lists them. Headings and index entries describe
    /// the sleeve rather than the music, and a Position is a Track's natural key within
    /// a Release — so an entry without one, or repeating one already taken, is dropped
    /// rather than allowed to collide in the database.
    /// </summary>
    private static IEnumerable<Track> TracksOf(IReadOnlyList<DiscogsTracklistEntry>? tracklist)
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sequence = 0;

        foreach (var entry in tracklist ?? [])
        {
            if (!string.Equals(entry.Type, "track", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var position = Clip(entry.Position, 20);

            if (position is null || !taken.Add(position))
            {
                continue;
            }

            yield return new Track
            {
                Position = position,
                Title = Clip(entry.Title, 500) ?? string.Empty,
                Duration = ParseDuration(entry.Duration),
                Sequence = sequence++,
            };
        }
    }

    private static string ArtistOf(DiscogsReleaseDetail detail)
    {
        if (WithoutDisambiguator(detail.ArtistsSort) is { Length: > 0 } sorted)
        {
            return sorted;
        }

        var names = (detail.Artists ?? [])
            .Select(artist => WithoutDisambiguator(artist.Name))
            .Where(name => name.Length > 0);

        return string.Join(", ", names);
    }

    /// <summary>Reads as Discogs prints it: "Vinyl, LP, Album, 180 Gram".</summary>
    private static string? FormatOf(IReadOnlyList<DiscogsFormat>? formats)
    {
        if (formats?.FirstOrDefault() is not { } format)
        {
            return null;
        }

        var parts = new[] { format.Name }
            .Concat(format.Descriptions ?? [])
            .Append(format.Text)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Barcodes are transcribed from the sleeve, so they arrive spaced and hyphenated
    /// as printed. Stored as digits when that is what they are, so a scanned barcode
    /// and a typed one are the same string.
    /// </summary>
    private static string? BarcodeOf(IReadOnlyList<DiscogsIdentifier>? identifiers)
    {
        var printed = (identifiers ?? [])
            .Where(identifier => string.Equals(identifier.Type, "Barcode", StringComparison.OrdinalIgnoreCase))
            .Select(identifier => identifier.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?.Trim();

        if (printed is null)
        {
            return null;
        }

        var digits = new string([.. printed.Where(char.IsAsciiDigit)]);

        return digits.Length is >= 8 and <= 14 ? digits : printed;
    }

    private static string? CoverOf(DiscogsReleaseDetail detail) =>
        PrimaryImage(detail)?.Uri ?? detail.Images?.FirstOrDefault()?.Uri;

    private static string? ThumbnailOf(DiscogsReleaseDetail detail) =>
        PrimaryImage(detail)?.Thumbnail
        ?? (string.IsNullOrWhiteSpace(detail.Thumb) ? null : detail.Thumb);

    private static DiscogsImage? PrimaryImage(DiscogsReleaseDetail detail) =>
        detail.Images?.FirstOrDefault(image =>
            string.Equals(image.Type, "primary", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Keeps a value inside the column it is going into. Discogs is user-edited, and a
    /// pathological title should cost a truncated word rather than a failed save.
    /// </summary>
    private static string? Clip(string? value, int maximum)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed.Length <= maximum ? trimmed : trimmed[..maximum];
    }
}
