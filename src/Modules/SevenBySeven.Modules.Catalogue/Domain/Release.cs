using System.ComponentModel.DataAnnotations.Schema;

namespace SevenBySeven.Modules.Catalogue.Domain;

/// <summary>
/// A specific pressing as catalogued by Discogs: a particular label, catalogue
/// number, country, year and format. Many Copies may share one Release.
/// This is cached external data — see docs/adr/0001.
/// </summary>
public sealed class Release
{
    private readonly List<Track> _tracks = [];

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    /// <summary>The Discogs release id. The natural key for this pressing.</summary>
    public required int DiscogsReleaseId { get; init; }

    /// <summary>
    /// The Discogs master id, held as a bare identifier. Master is not modelled as an
    /// entity until there is a question that needs one.
    /// </summary>
    public int? DiscogsMasterId { get; init; }

    public required string Title { get; set; }

    public required string ArtistName { get; set; }

    public string? LabelName { get; set; }

    /// <summary>The identifier the label prints on the sleeve and centre label.</summary>
    public string? CatalogueNumber { get; set; }

    public string? Country { get; set; }

    public ReleaseDate? Released { get; set; }

    /// <summary>Format as Discogs describes it, such as "Vinyl, LP, Album, Stereo".</summary>
    public string? FormatDescription { get; set; }

    public string? Barcode { get; set; }

    public List<string> Genres { get; set; } = [];

    public List<string> Styles { get; set; } = [];

    public string? ThumbnailUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    /// <summary>When this was last fetched from Discogs. Refresh is manual only.</summary>
    public DateTimeOffset CachedAt { get; set; } = DateTimeOffset.UtcNow;

    public IReadOnlyList<Track> Tracks => _tracks;

    /// <summary>
    /// The pressing in one line — label, catalogue number, country, year and format.
    /// This is what tells two Releases of the same album apart, so it is what a listing
    /// has to show. Unmapped: it is composed from columns, not stored.
    /// </summary>
    [NotMapped]
    public string Pressing => string.Join(" \u00b7 ", new[]
    {
        LabelName,
        CatalogueNumber,
        Country,
        Released?.ToString(),
        FormatDescription,
    }.Where(part => !string.IsNullOrWhiteSpace(part)));

    public void ReplaceTracks(IEnumerable<Track> tracks)
    {
        _tracks.Clear();
        _tracks.AddRange(tracks);
    }
}
