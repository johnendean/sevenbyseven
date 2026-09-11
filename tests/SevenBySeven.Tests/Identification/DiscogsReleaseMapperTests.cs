using System.Text.Json;
using SevenBySeven.Modules.Identification.Discogs;

namespace SevenBySeven.Tests.Identification;

public class DiscogsReleaseMapperTests
{
    /// <summary>
    /// A /releases/{id} response, trimmed to the parts we read. The tracklist carries the
    /// awkward entries Discogs really does contain: a side heading, an index entry, a
    /// repeated position and one with no position at all.
    /// </summary>
    private const string SampleRelease =
        """
        {
          "id": 1234567,
          "master_id": 96559,
          "title": "Never Mind The Bollocks Here's The Sex Pistols",
          "year": 1977,
          "released": "1977-10-28",
          "country": "UK",
          "artists_sort": "Sex Pistols (2)",
          "artists": [{ "name": "Sex Pistols (2)" }],
          "labels": [
            { "name": "Virgin", "catno": "V 2086" },
            { "name": "Virgin", "catno": "V2086" }
          ],
          "formats": [{ "name": "Vinyl", "qty": "1", "text": "Gatefold", "descriptions": ["LP", "Album"] }],
          "identifiers": [
            { "type": "Matrix / Runout", "value": "V 2086 A-1U" },
            { "type": "Barcode", "value": "0 7599-23147-1 4" }
          ],
          "images": [
            { "type": "secondary", "uri": "https://img/back.jpg", "uri150": "https://img/back-150.jpg" },
            { "type": "primary", "uri": "https://img/front.jpg", "uri150": "https://img/front-150.jpg" }
          ],
          "genres": ["Rock"],
          "styles": ["Punk"],
          "thumb": "https://img/thumb.jpg",
          "tracklist": [
            { "type_": "heading", "position": "", "title": "Side One", "duration": "" },
            { "type_": "track", "position": "A1", "title": "Holidays In The Sun", "duration": "3:22" },
            { "type_": "track", "position": "A2", "title": "Bodies", "duration": "3:03" },
            { "type_": "index", "position": "", "title": "An index entry", "duration": "" },
            { "type_": "track", "position": "A2", "title": "A repeated position", "duration": "2:00" },
            { "type_": "track", "position": "", "title": "No position at all", "duration": "1:00" },
            { "type_": "track", "position": "B1", "title": "Anarchy In The U.K.", "duration": "3:32" }
          ]
        }
        """;

    [Fact]
    public void A_release_keeps_the_facts_that_identify_the_pressing()
    {
        var release = Mapped();

        Assert.Equal(1234567, release.DiscogsReleaseId);
        Assert.Equal(96559, release.DiscogsMasterId);
        Assert.Equal("Never Mind The Bollocks Here's The Sex Pistols", release.Title);
        Assert.Equal("Sex Pistols", release.ArtistName);
        Assert.Equal("Virgin", release.LabelName);
        Assert.Equal("V 2086", release.CatalogueNumber);
        Assert.Equal("UK", release.Country);
        Assert.Equal("1977-10-28", release.Released?.ToString());
        Assert.Equal("Vinyl, LP, Album, Gatefold", release.FormatDescription);
        Assert.Equal(["Rock"], release.Genres);
        Assert.Equal(["Punk"], release.Styles);
    }

    [Fact]
    public void A_barcode_printed_with_spaces_is_held_as_the_digits_it_is() =>
        Assert.Equal("075992314714", Mapped().Barcode);

    [Fact]
    public void The_front_cover_is_preferred_to_whichever_image_came_first()
    {
        var release = Mapped();

        Assert.Equal("https://img/front.jpg", release.CoverImageUrl);
        Assert.Equal("https://img/front-150.jpg", release.ThumbnailUrl);
    }

    [Fact]
    public void Only_the_music_becomes_a_track_and_it_keeps_its_running_order()
    {
        var tracks = Mapped().Tracks;

        Assert.Equal(["A1", "A2", "B1"], tracks.Select(track => track.Position));
        Assert.Equal([0, 1, 2], tracks.Select(track => track.Sequence));
        Assert.Equal("Holidays In The Sun", tracks[0].Title);
        Assert.Equal(TimeSpan.FromSeconds(202), tracks[0].Duration);
    }

    [Theory]
    [InlineData("1977-10-28", null, "1977-10-28")]
    [InlineData("1959-08", null, "1959-08")]
    [InlineData("1959", null, "1959")]
    // Discogs pads what it does not know, and a zeroed month is not January.
    [InlineData("1959-00-00", null, "1959")]
    // A day with no month is not a date anybody can use.
    [InlineData("1959-00-17", null, "1959")]
    [InlineData("", 1959, "1959")]
    [InlineData("0", 1959, "1959")]
    [InlineData(null, 1959, "1959")]
    public void A_release_date_is_read_at_whatever_precision_it_has(string? released, int? year, string expected) =>
        Assert.Equal(expected, DiscogsReleaseMapper.ParseReleaseDate(released, year)?.ToString());

    [Theory]
    [InlineData("", null)]
    [InlineData("0", 0)]
    [InlineData(null, null)]
    public void A_release_with_no_usable_date_has_none(string? released, int? year) =>
        Assert.Null(DiscogsReleaseMapper.ParseReleaseDate(released, year));

    [Theory]
    [InlineData("3:22", 202)]
    [InlineData("1:02:33", 3753)]
    [InlineData("0:45", 45)]
    public void A_duration_is_read_as_printed(string duration, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), DiscogsReleaseMapper.ParseDuration(duration));

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("3")]
    [InlineData("side long")]
    [InlineData("3:75")]
    [InlineData("1:2:3:4")]
    // "0:00" is how Discogs writes "we don't know".
    [InlineData("0:00")]
    public void Anything_that_is_not_a_duration_is_no_duration(string? duration) =>
        Assert.Null(DiscogsReleaseMapper.ParseDuration(duration));

    [Theory]
    [InlineData("Nirvana (2)", "Nirvana")]
    [InlineData("Sex Pistols (12)", "Sex Pistols")]
    [InlineData("Miles Davis", "Miles Davis")]
    // Brackets that mean something stay.
    [InlineData("Godspeed You! Black Emperor", "Godspeed You! Black Emperor")]
    [InlineData("Album (Remastered)", "Album (Remastered)")]
    // A name that is nothing but the bracket keeps it: there is nothing else left.
    [InlineData("(2)", "(2)")]
    [InlineData(null, "")]
    public void A_discogs_disambiguator_is_not_part_of_the_name(string? name, string expected) =>
        Assert.Equal(expected, DiscogsReleaseMapper.WithoutDisambiguator(name));

    private static SevenBySeven.Modules.Catalogue.Domain.Release Mapped() =>
        DiscogsReleaseMapper.ToRelease(
            JsonSerializer.Deserialize<DiscogsReleaseDetail>(
                SampleRelease, new JsonSerializerOptions(JsonSerializerDefaults.Web))!);
}
