using SevenBySeven.Modules.Identification.Vision;

namespace SevenBySeven.Tests.Identification;

public class ClaudeSleeveReaderTests
{
    [Fact]
    public void Parse_reads_every_field()
    {
        var details = ClaudeSleeveReader.Parse("""
            {
              "barcode": "5099749494220",
              "artistName": "Miles Davis",
              "title": "Kind Of Blue",
              "labelName": "Columbia",
              "catalogueNumber": "CS 8163",
              "year": 1959
            }
            """);

        Assert.Equal("5099749494220", details.Barcode);
        Assert.Equal("Miles Davis", details.ArtistName);
        Assert.Equal("Kind Of Blue", details.Title);
        Assert.Equal("Columbia", details.LabelName);
        Assert.Equal("CS 8163", details.CatalogueNumber);
        Assert.Equal(1959, details.Year);
        Assert.False(details.IsEmpty);
    }

    [Fact]
    public void Parse_treats_nulls_as_unread()
    {
        var details = ClaudeSleeveReader.Parse("""
            {"barcode": null, "artistName": null, "title": null,
             "labelName": null, "catalogueNumber": null, "year": null}
            """);

        Assert.True(details.IsEmpty);
    }

    [Fact]
    public void Parse_ignores_a_year_that_is_not_a_year()
    {
        var details = ClaudeSleeveReader.Parse("""{"title": "Something", "year": 12}""");

        Assert.Null(details.Year);
        Assert.Equal("Something", details.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("{\"unterminated\": ")]
    public void Parse_survives_junk(string json) =>
        Assert.True(ClaudeSleeveReader.Parse(json).IsEmpty);

    [Fact]
    public void Parse_trims_whitespace_the_model_left_behind() =>
        Assert.Equal("BLP 4003", ClaudeSleeveReader.Parse("""{"catalogueNumber": "  BLP 4003  "}""").CatalogueNumber);
}
