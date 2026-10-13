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

    [Fact]
    public async Task The_catalogue_number_is_searched_within_the_chosen_format()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "R 5084", LabelName = "Parlophone" });
        var catalogue = new FakeCatalogue(_ => [Candidate(7)]);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.Equal(Format.SevenInch, catalogue.Queries.Single().Format);
        Assert.False(result.IsOutsideFormat);
    }

    [Fact]
    public async Task Nothing_within_the_format_widens_to_any_vinyl_and_says_so()
    {
        // Discogs entries do not all record their size, so the right Release can sit
        // outside a strict Format search.
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "R 5084" });
        var catalogue = new FakeCatalogue(q => q.Format is Format.Vinyl ? [Candidate(8)] : []);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.Equal(IdentificationRoute.CatalogueNumber, result.Route);
        Assert.True(result.IsOutsideFormat);
        Assert.Equal([Format.SevenInch, Format.Vinyl], catalogue.Queries.Select(q => q.Format));
    }

    [Fact]
    public async Task A_catalogue_number_as_any_vinyl_beats_artist_and_title_within_the_format()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails
        {
            CatalogueNumber = "R 5084",
            ArtistName = "The Beatles",
            Title = "Hey Jude",
        });
        var catalogue = new FakeCatalogue(q =>
            q.CatalogueNumber is not null && q.Format is Format.Vinyl ? [Candidate(9)]
            : q.ArtistName is not null ? [Candidate(10)]
            : []);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.Equal(IdentificationRoute.CatalogueNumber, result.Route);
        Assert.Equal(9, result.Candidates.Single().DiscogsReleaseId);
        Assert.All(catalogue.Queries, q => Assert.Null(q.ArtistName));
    }

    [Fact]
    public async Task A_barcode_is_never_narrowed_to_a_format()
    {
        // A barcode names one Release exactly; narrowing it could only hide that Release.
        var catalogue = new FakeCatalogue(_ => [Candidate(11)]);
        var identifier = Build(new FakeBarcodeScanner("5099749494220"), new FakeSleeveReader(new SleeveDetails()), catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.LP);

        Assert.Equal(Format.Vinyl, catalogue.Queries.Single().Format);
        Assert.False(result.IsOutsideFormat);
    }

    [Fact]
    public async Task A_failed_search_within_the_format_does_not_widen()
    {
        // A failure says nothing about what Discogs holds. Widening after it would mark a
        // successful retry as outside the Format when it may not be.
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "R 5084" });
        var catalogue = new FakeCatalogue(q => q.Format is Format.Vinyl
            ? DiscogsSearch.Found([Candidate(12)])
            : DiscogsSearch.Failure);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.False(result.HasCandidates);
        Assert.False(result.IsOutsideFormat);
        Assert.Equal(Format.SevenInch, catalogue.Queries.Single().Format);
    }

    [Fact]
    public async Task A_failed_search_still_lets_weaker_evidence_be_tried()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails
        {
            CatalogueNumber = "R 5084",
            ArtistName = "The Beatles",
            Title = "Hey Jude",
        });
        var catalogue = new FakeCatalogue(q => q.CatalogueNumber is not null
            ? DiscogsSearch.Failure
            : DiscogsSearch.Found([Candidate(13)]));
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.Equal(IdentificationRoute.ArtistAndTitle, result.Route);
        Assert.False(result.IsOutsideFormat);
    }

    [Fact]
    public async Task Nothing_found_because_Discogs_failed_is_not_nothing_matching()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { ArtistName = "Miles Davis", Title = "Kind Of Blue" });
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, new FakeCatalogue(_ => DiscogsSearch.Failure));

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.False(result.HasCandidates);
        Assert.True(result.SearchFailed);
        Assert.Equal("Miles Davis", result.Details.ArtistName);
    }

    [Fact]
    public async Task Nothing_found_by_searches_that_all_ran_is_nothing_matching()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "UNKNOWN 1", Title = "Nothing" });
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, new FakeCatalogue(_ => []));

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg", Format.SevenInch);

        Assert.False(result.HasCandidates);
        Assert.False(result.SearchFailed);
    }

    [Fact]
    public async Task A_failed_barcode_search_is_remembered_when_the_sleeve_reads_nothing()
    {
        // The barcode was read and could not be looked up; an illegible sleeve after that
        // is not the reason nothing was found.
        var identifier = Build(
            new FakeBarcodeScanner("5099749494220"),
            new FakeSleeveReader(new SleeveDetails()),
            new FakeCatalogue(_ => DiscogsSearch.Failure));

        var result = await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.False(result.HasCandidates);
        Assert.True(result.SearchFailed);
    }

    [Fact]
    public async Task Any_vinyl_has_nothing_wider_to_fall_back_to()
    {
        var sleeves = new FakeSleeveReader(new SleeveDetails { CatalogueNumber = "UNKNOWN 1" });
        var catalogue = new FakeCatalogue(_ => []);
        var identifier = Build(new FakeBarcodeScanner(null), sleeves, catalogue);

        await identifier.IdentifyAsync(AnyImage, "image/jpeg");

        Assert.Single(catalogue.Queries);
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

    private sealed class FakeCatalogue(Func<DiscogsQuery, DiscogsSearch> respond) : IDiscogsCatalogue
    {
        public FakeCatalogue(Func<DiscogsQuery, IReadOnlyList<MatchCandidate>> answer)
            : this(query => DiscogsSearch.Found(answer(query)))
        {
        }

        public List<DiscogsQuery> Queries { get; } = [];

        public Task<DiscogsSearch> SearchAsync(
            DiscogsQuery query, CancellationToken cancellationToken = default)
        {
            Queries.Add(query);
            return Task.FromResult(respond(query));
        }
    }
}
