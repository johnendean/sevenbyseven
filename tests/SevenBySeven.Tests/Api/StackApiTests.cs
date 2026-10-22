using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SevenBySeven.Modules.Collection.Contracts;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;

namespace SevenBySeven.Tests.Api;

/// <summary>
/// The Stack as the API holds it, identification running behind it for real against the
/// stubbed pipeline. The tests wait for identification the way a client would: by reading
/// the Stack again until it has settled.
/// </summary>
public class StackApiTests
{
    internal static readonly byte[] Photograph = [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3];

    internal static readonly MatchCandidate KindOfBlue =
        new(249504, "Kind Of Blue", "Miles Davis", "Columbia", "CS 8163", "US", 1959, "Vinyl, LP", null, 5460);

    [Fact]
    public async Task The_limit_is_the_api_s_to_say()
    {
        await using var host = await ApiHost.StartAsync(("Scanning:MaxStackSize", "4"));
        using var client = host.CreateClient();

        Assert.Equal(new StackLimits(4), await (await client.GetAsync("/stack/limits")).ReadAsync<StackLimits>());
    }

    [Fact]
    public async Task There_is_no_stack_until_one_is_begun()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/stack")).StatusCode);
    }

    [Fact]
    public async Task A_captured_record_is_identified_behind_the_capture()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = Matching(KindOfBlue);
        using var client = host.CreateClient();
        await BeginAsync(client, Format.SevenInch);

        var response = await CaptureAsync(client, Photograph);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var captured = await response.ReadAsync<StackedScanLine>();
        Assert.Equal(1, captured.Position);

        var settled = await SettledAsync(client);
        var line = Assert.Single(settled.Scans);
        Assert.Equal(StackedScanStatus.Matched, line.Status);
        Assert.Equal(249504, Assert.Single(line.Result!.Candidates).DiscogsReleaseId);

        var (image, _, format) = Assert.Single(host.Identifier.Calls);
        Assert.Equal(Photograph, image);
        Assert.Equal(Format.SevenInch, format);
    }

    [Fact]
    public async Task A_sleeve_that_could_not_be_read_says_why()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = IdentificationResult.Nothing() with { ReadFailure = "The sleeve could not be read. Overloaded." };
        using var client = host.CreateClient();
        await BeginAsync(client);
        await CaptureAsync(client, Photograph);

        var line = Assert.Single((await SettledAsync(client)).Scans);

        Assert.Equal(StackedScanStatus.Unreadable, line.Status);
        Assert.Equal("The sleeve could not be read. Overloaded.", line.Failure);
    }

    [Fact]
    public async Task Identification_that_breaks_is_reported_against_the_record()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Throws = new InvalidOperationException("A bug.");
        using var client = host.CreateClient();
        await BeginAsync(client);
        await CaptureAsync(client, Photograph);

        var line = Assert.Single((await SettledAsync(client)).Scans);

        Assert.Equal(StackedScanStatus.Unreadable, line.Status);
        Assert.Contains("Identification failed", line.Failure);
    }

    [Fact]
    public async Task A_confirmation_adds_a_copy_with_the_stack_s_defaults()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = Matching(KindOfBlue);
        using var client = host.CreateClient();
        await BeginAsync(client);
        await client.PutAsJsonAsync("/stack/defaults", new StackDefaults { PurchasedFrom = "Reckless", Location = "Crate 3" }, ApiHost.Json);
        var scan = (await (await CaptureAsync(client, Photograph)).ReadAsync<StackedScanLine>()).Id;
        await SettledAsync(client);

        var response = await ConfirmAsync(client, scan, KindOfBlue.DiscogsReleaseId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(StackedScanStatus.Confirmed, Assert.Single((await CurrentAsync(client)).Scans).Status);

        var copy = Assert.Single(await (await client.GetAsync("/collection")).ReadAsync<CopyLine[]>());
        var sheet = await (await client.GetAsync($"/collection/{copy.Id}")).ReadAsync<CopySheet>();
        Assert.Equal(249504, sheet.Release.DiscogsReleaseId);
        Assert.Equal("Reckless", sheet.Details.PurchasedFrom);
        Assert.Equal("Crate 3", sheet.Details.Location);
    }

    [Fact]
    public async Task A_record_confirmed_once_cannot_be_confirmed_again()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = Matching(KindOfBlue);
        using var client = host.CreateClient();
        var scan = await MatchedAsync(client);
        await ConfirmAsync(client, scan, KindOfBlue.DiscogsReleaseId);

        var again = await ConfirmAsync(client, scan, KindOfBlue.DiscogsReleaseId);

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Single(await (await client.GetAsync("/collection")).ReadAsync<CopyLine[]>());
    }

    [Fact]
    public async Task Only_one_of_the_record_s_own_candidates_can_be_confirmed()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = Matching(KindOfBlue);
        using var client = host.CreateClient();
        var scan = await MatchedAsync(client);

        var response = await ConfirmAsync(client, scan, 1000);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(host.Source.Fetched);
    }

    [Fact]
    public async Task A_pressing_that_cannot_be_fetched_leaves_the_record_to_try_again()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = Matching(KindOfBlue);
        host.Source.Unfetchable.Add(KindOfBlue.DiscogsReleaseId);
        using var client = host.CreateClient();
        var scan = await MatchedAsync(client);

        var refused = await ConfirmAsync(client, scan, KindOfBlue.DiscogsReleaseId);

        Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
        Assert.Equal(StackedScanStatus.Matched, Assert.Single((await CurrentAsync(client)).Scans).Status);

        host.Source.Unfetchable.Clear();
        Assert.Equal(HttpStatusCode.NoContent, (await ConfirmAsync(client, scan, KindOfBlue.DiscogsReleaseId)).StatusCode);
    }

    [Fact]
    public async Task A_full_stack_refuses_another_record()
    {
        await using var host = await ApiHost.StartAsync(("Scanning:MaxStackSize", "1"));
        using var client = host.CreateClient();
        await BeginAsync(client);
        await CaptureAsync(client, Photograph);

        var response = await CaptureAsync(client, Photograph);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Single((await CurrentAsync(client)).Scans);
    }

    [Fact]
    public async Task A_photograph_with_no_stack_to_go_into_is_not_found()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await CaptureAsync(client, Photograph)).StatusCode);
    }

    [Fact]
    public async Task A_stack_with_records_to_deal_with_is_not_started_over()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        var first = await BeginAsync(client);
        await CaptureAsync(client, Photograph);

        var response = await client.PostAsJsonAsync("/stack", new BeginStackRequest(Format.LP), ApiHost.Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(first.Id, (await CurrentAsync(client)).Id);
    }

    [Fact]
    public async Task Only_a_jpeg_of_a_real_size_is_taken()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        await BeginAsync(client);

        var png = new ByteArrayContent(Photograph);
        png.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await client.PostAsync("/stack/scans?width=10&height=10", png)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await CaptureAsync(client, [0x00, 0x01, 0x02])).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/stack/scans?width=0&height=10", Jpeg(Photograph))).StatusCode);
        Assert.Empty((await CurrentAsync(client)).Scans);
    }

    [Fact]
    public async Task The_photograph_is_there_until_the_stack_lets_it_go()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        await BeginAsync(client);
        var scan = (await (await CaptureAsync(client, Photograph)).ReadAsync<StackedScanLine>()).Id;

        var photograph = await client.GetAsync($"/stack/scans/{scan}/photograph");

        Assert.Equal("image/jpeg", photograph.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Photograph, await photograph.Content.ReadAsByteArrayAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/stack")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/stack/scans/{scan}/photograph")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/stack")).StatusCode);
    }

    [Fact]
    public async Task A_record_given_up_on_is_over()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        await BeginAsync(client);
        var scan = (await (await CaptureAsync(client, Photograph)).ReadAsync<StackedScanLine>()).Id;

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/stack/scans/{scan}")).StatusCode);

        var stack = await CurrentAsync(client);
        Assert.Equal(StackedScanStatus.Abandoned, Assert.Single(stack.Scans).Status);
        Assert.True(stack.IsComplete);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/stack/scans/{scan}")).StatusCode);
    }

    [Fact]
    public async Task Defaults_past_the_limits_are_refused()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        await BeginAsync(client);

        var response = await client.PutAsJsonAsync("/stack/defaults", new StackDefaults { Location = new string('x', 201) }, ApiHost.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null((await CurrentAsync(client)).Defaults.Location);
    }

    [Fact]
    public async Task Defaults_with_no_stack_go_nowhere()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.PutAsJsonAsync("/stack/defaults", new StackDefaults { Location = "Crate 3" }, ApiHost.Json);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Watching_the_stack_hears_each_change_as_it_lands()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        using var events = await client.GetAsync("/stack/events", HttpCompletionOption.ResponseHeadersRead, stop.Token);
        Assert.Equal("text/event-stream", events.Content.Headers.ContentType?.MediaType);

        using var reader = new StreamReader(await events.Content.ReadAsStreamAsync(stop.Token));
        var first = await NextVersionAsync(reader, stop.Token);

        await BeginAsync(client);

        Assert.True(await NextVersionAsync(reader, stop.Token) > first);
    }

    internal static IdentificationResult Matching(MatchCandidate candidate) =>
        new([candidate], new SleeveDetails(), IdentificationRoute.Barcode);

    internal static async Task<StackSheet> BeginAsync(HttpClient client, Format format = Format.Vinyl)
    {
        var response = await client.PostAsJsonAsync("/stack", new BeginStackRequest(format), ApiHost.Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        return await response.ReadAsync<StackSheet>();
    }

    private static Task<HttpResponseMessage> CaptureAsync(HttpClient client, byte[] jpeg) =>
        client.PostAsync("/stack/scans?width=1600&height=1600", Jpeg(jpeg));

    private static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid scanId, int discogsReleaseId) =>
        client.PostAsJsonAsync($"/stack/scans/{scanId}/confirmation", new ConfirmScanRequest(discogsReleaseId), ApiHost.Json);

    private static async Task<StackSheet> CurrentAsync(HttpClient client) =>
        await (await client.GetAsync("/stack")).ReadAsync<StackSheet>();

    /// <summary>A Stack holding one Scan, identified as whatever the stub answers.</summary>
    private static async Task<Guid> MatchedAsync(HttpClient client)
    {
        await BeginAsync(client);
        var scan = (await (await CaptureAsync(client, Photograph)).ReadAsync<StackedScanLine>()).Id;
        await SettledAsync(client);

        return scan;
    }

    /// <summary>The Stack once nothing in it is still being identified.</summary>
    private static async Task<StackSheet> SettledAsync(HttpClient client)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (true)
        {
            var stack = await CurrentAsync(client);

            if (stack.Scans.All(scan => scan.IsIdentified) || DateTimeOffset.UtcNow > deadline)
            {
                return stack;
            }

            await Task.Delay(25);
        }
    }

    private static async Task<long> NextVersionAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                return long.Parse(line["data:".Length..].Trim());
            }
        }

        throw new InvalidOperationException("The event stream ended.");
    }

    private static ByteArrayContent Jpeg(byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");

        return content;
    }
}
