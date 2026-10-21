using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using SevenBySeven.Modules.Identification.Contracts;

namespace SevenBySeven.Tests.Api;

public class IdentificationApiTests
{
    private static readonly byte[] Photograph = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    [Fact]
    public async Task A_photograph_comes_back_as_match_candidates()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = new IdentificationResult(
            [new MatchCandidate(249504, "Kind Of Blue", "Miles Davis", "Columbia", "CS 8163", "US", 1959, "Vinyl, LP", null, 5460)],
            new SleeveDetails { CatalogueNumber = "CS 8163" },
            IdentificationRoute.CatalogueNumber);
        using var client = host.CreateClient();

        var response = await client.PostAsync("/identifications", Jpeg(Photograph));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.ReadAsync<IdentificationResult>();
        Assert.Equal(249504, Assert.Single(result.Candidates).DiscogsReleaseId);
        Assert.Equal(IdentificationRoute.CatalogueNumber, result.Route);

        var (image, contentType, format) = Assert.Single(host.Identifier.Calls);
        Assert.Equal(Photograph, image);
        Assert.Equal("image/jpeg", contentType);
        Assert.Equal(Format.Vinyl, format);
    }

    [Fact]
    public async Task The_format_narrows_the_search()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        await client.PostAsync("/identifications?format=SevenInch", Jpeg(Photograph));

        Assert.Equal(Format.SevenInch, Assert.Single(host.Identifier.Calls).Format);
    }

    [Fact]
    public async Task A_sleeve_that_could_not_be_read_says_why()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = IdentificationResult.Nothing() with { ReadFailure = "The sleeve could not be read. Overloaded." };
        using var client = host.CreateClient();

        var result = await (await client.PostAsync("/identifications", Jpeg(Photograph))).ReadAsync<IdentificationResult>();

        Assert.Equal("The sleeve could not be read. Overloaded.", result.ReadFailure);
    }

    [Fact]
    public async Task Only_a_jpeg_is_read()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var png = new ByteArrayContent(Photograph);
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var response = await client.PostAsync("/identifications", png);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Empty(host.Identifier.Calls);
    }

    [Fact]
    public async Task An_empty_photograph_is_refused()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsync("/identifications", Jpeg([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(host.Identifier.Calls);
    }

    [Fact]
    public async Task A_photograph_past_the_ceiling_is_refused()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PostAsync("/identifications", Jpeg(new byte[(8 * 1024 * 1024) + 1]));

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(host.Identifier.Calls);
    }

    [Fact]
    public async Task A_photograph_of_unstated_length_is_still_held_to_the_ceiling()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var unstated = new StreamContent(new UnseekableStream(new byte[(8 * 1024 * 1024) + 1]));
        unstated.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        var response = await client.PostAsync("/identifications", unstated);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(host.Identifier.Calls);
    }

    [Fact]
    public async Task A_bug_comes_back_as_a_problem_rather_than_a_page()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Throws = new InvalidOperationException("A bug.");
        using var client = host.CreateClient();

        var response = await client.PostAsync("/identifications", Jpeg(Photograph));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, (await response.ReadAsync<ProblemDetails>()).Status);
    }

    private static ByteArrayContent Jpeg(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return content;
    }

    /// <summary>A body the client cannot measure, so it goes without a Content-Length.</summary>
    private sealed class UnseekableStream(byte[] bytes) : MemoryStream(bytes)
    {
        public override bool CanSeek => false;
    }
}
