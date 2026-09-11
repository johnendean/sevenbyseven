using Microsoft.Extensions.Logging.Abstractions;
using SevenBySeven.Modules.Identification;
using SevenBySeven.Modules.Identification.Barcodes;
using SevenBySeven.Modules.Identification.Discogs;
using SevenBySeven.Modules.Identification.Domain;
using SevenBySeven.Modules.Identification.Vision;

namespace SevenBySeven.Tests.Identification;

public class SleeveIdentifierTests
{
    private static readonly ReadOnlyMemory<byte> AnyImage = new byte[] { 1, 2, 3, 4 };

    [Fact]
    public async Task A_decoded_barcode_short_circuits_the_sleeve_reader()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { Title = "should not be reached" });
        var catalogue = new FakeCatalogue(_ => [Candidate(1)]);
        var identifier = Build(new FakeBarcodeScanner("5099749494220"), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.Barcode, result.Route);
        Assert.Equal(0, sleeves.Calls);
        Assert.Equal("5099749494220", catalogue.Queries.Single().Barcode);
    }

    [Fact]
    public async Task A_barcode_that_matches_nothing_falls_through_to_the_sleeve_reader()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "BLP 4003", LabelName = "Blue Note" });
        // Nothing for the barcode, a hit for the catalogue number.
        var catalogue = new FakeCatalogue(q => q.CatalogueNumber is null ? [] : [Candidate(2)]);
        var identifier = Build(new FakeBarcodeScanner("0000000000000"), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.CatalogueNumber, result.Route);
        Assert.Equal(1, sleeves.Calls);
        Assert.Equal("Blue Note", catalogue.Queries.Last().LabelName);
    }

    [Fact]
    public async Task The_catalogue_number_is_tried_before_artist_and_title()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails
        {
            CatalogueNumber = "SHVL 804",
            ArtistName = "Pink Floyd",
            Title = "The Dark Side Of The Moon",
        });
        var catalogue = new FakeCatalogue(_ => [Candidate(3)]);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.CatalogueNumber, result.Route);
        Assert.Single(catalogue.Queries);
    }

    [Fact]
    public async Task Artist_and_title_are_the_last_resort()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails
        {
            CatalogueNumber = "UNKNOWN 1",
            ArtistName = "Pink Floyd",
            Title = "The Dark Side Of The Moon",
        });
        var catalogue = new FakeCatalogue(q => q.ArtistName is null ? [] : [Candidate(4)]);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.ArtistAndTitle, result.Route);
        Assert.Equal(2, catalogue.Queries.Count);
    }

    [Fact]
    public async Task An_unreadable_sleeve_searches_for_nothing()
    {
        var catalogue = new FakeCatalogue(_ => [Candidate(5)]);
        var identifier = Build(new FakeBarcodeScanner(null), new FakeSleeveReader(new SleeveDetails()), catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.None, result.Route);
        Assert.False(result.HasCandidates);
        Assert.Empty(catalogue.Queries);
    }

    [Fact]
    public async Task An_empty_image_does_no_work_at_all()
    {
        var catalogue = new FakeCatalogue(_ => [Candidate(6)]);
        var sleeves = new FakeSleeveReader(new SleeveDetails { Title = "x" });
        var identifier = Build(new FakeBarcodeScanner("123"), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(ReadOnlyMemory<byte>.Empty, "image/jpeg");

        Assert.Equal(IdentificationRoute.None, result.Route);
        Assert.Empty(catalogue.Queries);
        Assert.Equal(0, sleeves.Calls);
    }

    [Fact]
    public async Task Exhausting_every_route_still_returns_what_was_read()
    {
        var details = new SleeveDetails { ArtistName = "Nobody", Title = "Nothing" };
        var identifier = Build(new FakeBarcodeScanner(null), new FakeSleeveReader(details), new FakeCatalogue(_ => []));

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Equal(IdentificationRoute.None, result.Route);
        Assert.Equal("Nobody", result.Details.ArtistName);
    }

    private static SleeveIdentifier Build(
        IBarcodeScanner barcodes,
        ISleeveReader sleeves,
        IDiscogsCatalogue catalogue) =>
        new(barcodes, sleeves, catalogue, NullLogger<SleeveIdentifier>.Instance);

    private static MatchCandidate Candidate(int id) =>
        new(id, "Title", "Artist", null, null, null, null, null, null, null);

    private sealed class FakeBarcodeScanner(string? barcode) : IBarcodeScanner
    {
        public string? TryRead(ReadOnlyMemory<byte> image) => barcode;
    }

    private sealed class FakeSleeveReader(SleeveDetails details) : ISleeveReader
    {
        public int Calls { get; private set; }

        public Task<SleeveDetails> ReadAsync(
            ReadOnlyMemory<byte> image, string contentType, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(details);
        }
    }

    private sealed class FakeCatalogue(Func<DiscogsQuery, IReadOnlyList<MatchCandidate>> respond) : IDiscogsCatalogue
    {
        public List<DiscogsQuery> Queries { get; } = [];

        public Task<IReadOnlyList<MatchCandidate>> SearchAsync(
            DiscogsQuery query, CancellationToken cancellationToken = default)
        {
            Queries.Add(query);
            return Task.FromResult(respond(query));
        }
    }
}
