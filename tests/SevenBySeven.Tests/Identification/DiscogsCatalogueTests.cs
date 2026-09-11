using SevenBySeven.Modules.Identification.Discogs;

namespace SevenBySeven.Tests.Identification;

public class DiscogsCatalogueTests
{
    [Theory]
    [InlineData("Miles Davis - Kind Of Blue", "Miles Davis", "Kind Of Blue")]
    [InlineData("Godspeed You! Black Emperor - F♯A♯∞", "Godspeed You! Black Emperor", "F♯A♯∞")]
    // A title containing its own " - " must not be split twice.
    [InlineData("Pink Floyd - Wish You Were Here - Remastered", "Pink Floyd", "Wish You Were Here - Remastered")]
    // Hyphens without surrounding spaces are part of the name, not a separator.
    [InlineData("Jay-Z - The Blueprint", "Jay-Z", "The Blueprint")]
    public void SplitTitle_separates_artist_from_title(string combined, string artist, string title) =>
        Assert.Equal((artist, title), DiscogsCatalogue.SplitTitle(combined));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SplitTitle_survives_nothing(string? combined) =>
        Assert.Equal((string.Empty, string.Empty), DiscogsCatalogue.SplitTitle(combined));

    [Fact]
    public void SplitTitle_with_no_separator_is_all_title() =>
        Assert.Equal((string.Empty, "Untitled"), DiscogsCatalogue.SplitTitle("Untitled"));

    [Theory]
    [InlineData("1959", 1959)]
    [InlineData("", null)]
    [InlineData(null, null)]
    [InlineData("0", null)]
    [InlineData("not a year", null)]
    [InlineData("3000", null)]
    public void ParseYear_rejects_anything_implausible(string? input, int? expected) =>
        Assert.Equal(expected, DiscogsCatalogue.ParseYear(input));

    [Fact]
    public void BuildSearchUri_always_restricts_to_vinyl_releases()
    {
        var uri = DiscogsCatalogue.BuildSearchUri(new DiscogsQuery { Barcode = "5099749494220" }, 5);

        Assert.Contains("type=release", uri, StringComparison.Ordinal);
        Assert.Contains("format=Vinyl", uri, StringComparison.Ordinal);
        Assert.Contains("barcode=5099749494220", uri, StringComparison.Ordinal);
        Assert.Contains("per_page=5", uri, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchUri_omits_fields_that_were_not_read()
    {
        var uri = DiscogsCatalogue.BuildSearchUri(new DiscogsQuery { CatalogueNumber = "BLP 4003" }, 5);

        Assert.DoesNotContain("artist=", uri, StringComparison.Ordinal);
        Assert.DoesNotContain("barcode=", uri, StringComparison.Ordinal);
        Assert.Contains("catno=BLP%204003", uri, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSearchUri_escapes_values()
    {
        var uri = DiscogsCatalogue.BuildSearchUri(new DiscogsQuery { ArtistName = "Simon & Garfunkel" }, 5);

        Assert.Contains("artist=Simon%20%26%20Garfunkel", uri, StringComparison.Ordinal);
    }
}
