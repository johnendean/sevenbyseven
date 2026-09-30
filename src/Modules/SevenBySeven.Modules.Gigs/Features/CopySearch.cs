using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Modules.Gigs.Features;

/// <summary>
/// Finding a record in the Collection by what is written on it. Every word typed has to
/// appear somewhere among the artist, title, label and catalogue number, in any order, so
/// "miles blue" finds Kind Of Blue.
/// </summary>
public static class CopySearch
{
    public static bool Matches(Release release, string? text)
    {
        ArgumentNullException.ThrowIfNull(release);

        var words = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length == 0)
        {
            return false;
        }

        var haystack = string.Join(
            ' ',
            release.ArtistName,
            release.Title,
            release.LabelName,
            release.CatalogueNumber);

        return words.All(word => haystack.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
}
