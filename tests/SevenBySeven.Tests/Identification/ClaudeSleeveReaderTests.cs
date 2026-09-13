using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification;
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

    [Fact]
    public async Task An_unconfigured_key_is_reported_rather_than_read_as_an_illegible_sleeve()
    {
        var service = FakeMessageService.Saying("{}");

        var thrown = await Assert.ThrowsAsync<SleeveReadException>(
            () => Reader(service, key: null).ReadAsync(Jpeg, "image/jpeg"));

        Assert.Contains("API key", thrown.Message);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task A_sleeve_is_read_into_the_details_it_printed()
    {
        var service = FakeMessageService.Saying("""
            {"barcode": null, "artistName": "Miles Davis", "title": "Kind Of Blue",
             "labelName": "Columbia", "catalogueNumber": "CS 8163", "year": 1959}
            """);

        var details = await Reader(service).ReadAsync(Jpeg, "image/jpeg");

        Assert.Equal("Miles Davis", details.ArtistName);
        Assert.Equal("CS 8163", details.CatalogueNumber);
        Assert.Equal(1959, details.Year);
        Assert.Equal(1, service.Calls);
    }

    [Fact]
    public async Task An_empty_capture_is_not_worth_asking_about()
    {
        var service = FakeMessageService.Saying("{}");

        var details = await Reader(service).ReadAsync(ReadOnlyMemory<byte>.Empty, "image/jpeg");

        Assert.True(details.IsEmpty);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task A_declined_reading_is_an_illegible_sleeve_rather_than_a_failure()
    {
        // Nothing the user can act on: the scan simply finds nothing, and they can retake
        // the photograph or type the catalogue number themselves.
        var details = await Reader(FakeMessageService.Refusing("cyber")).ReadAsync(Jpeg, "image/jpeg");

        Assert.True(details.IsEmpty);
    }

    [Fact]
    public async Task A_reading_that_fails_says_so_rather_than_looking_like_a_blank_sleeve()
    {
        var service = FakeMessageService.Failing(new HttpRequestException("Anthropic is unreachable."));

        var thrown = await Assert.ThrowsAsync<SleeveReadException>(
            () => Reader(service).ReadAsync(Jpeg, "image/jpeg"));

        // The API's own words carry the part that says what to change.
        Assert.Contains("Anthropic is unreachable.", thrown.Message);
        Assert.IsType<HttpRequestException>(thrown.InnerException);
    }

    [Fact]
    public async Task Abandoning_a_scan_is_not_a_failure_to_report()
    {
        var service = FakeMessageService.Failing(new OperationCanceledException());

        // The user navigated away. Wrapping this would log a fault that never happened.
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => Reader(service).ReadAsync(Jpeg, "image/jpeg"));
    }

    [Fact]
    public async Task An_answer_that_is_not_the_json_we_asked_for_is_an_illegible_sleeve()
    {
        var details = await Reader(FakeMessageService.Saying("I am afraid I cannot read that."))
            .ReadAsync(Jpeg, "image/jpeg");

        Assert.True(details.IsEmpty);
    }

    private static readonly ReadOnlyMemory<byte> Jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

    private static ClaudeSleeveReader Reader(FakeMessageService service, string? key = "a-key") =>
        new(
            service,
            Options.Create(new IdentificationOptions { AnthropicApiKey = key }),
            NullLogger<ClaudeSleeveReader>.Instance);
}
