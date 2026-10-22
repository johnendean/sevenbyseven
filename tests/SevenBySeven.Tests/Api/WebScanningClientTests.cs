extern alias web;

using Microsoft.Extensions.Logging.Abstractions;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning.Contracts;
using web::SevenBySeven.Web.Identification;
using web::SevenBySeven.Web.Scanning;

namespace SevenBySeven.Tests.Api;

/// <summary>
/// The web app's scanning clients against the real API, in memory: what a photograph, a
/// Stack and its event stream look like from the far side of the wire.
/// </summary>
public class WebScanningClientTests
{
    [Fact]
    public async Task A_photograph_comes_back_identified()
    {
        await using var host = await ApiHost.StartAsync();
        host.Identifier.Answer = StackApiTests.Matching(StackApiTests.KindOfBlue);
        var identification = new IdentificationClient(host.CreateClient());

        var result = await identification.IdentifyAsync(StackApiTests.Photograph, Format.LP);

        Assert.Equal(249504, Assert.Single(result.Candidates).DiscogsReleaseId);
        Assert.Equal(Format.LP, Assert.Single(host.Identifier.Calls).Format);
    }

    [Fact]
    public async Task A_stack_is_begun_filled_confirmed_and_let_go()
    {
        await using var host = await ApiHost.StartAsync(("Scanning:MaxStackSize", "2"));
        host.Identifier.Answer = StackApiTests.Matching(StackApiTests.KindOfBlue);
        var stacks = new StackClient(host.CreateClient());

        Assert.Equal(2, (await stacks.LimitsAsync()).MaxStackSize);
        Assert.Null(await stacks.CurrentAsync());

        var stack = await stacks.BeginAsync(Format.SevenInch);
        Assert.Equal(Format.SevenInch, stack?.Format);
        Assert.True(await stacks.SetDefaultsAsync(new StackDefaults { Location = "Crate 3" }));

        var first = await stacks.CaptureAsync(StackApiTests.Photograph, 1600, 1600);
        var second = await stacks.CaptureAsync(StackApiTests.Photograph, 1600, 1600);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Null(await stacks.CaptureAsync(StackApiTests.Photograph, 1600, 1600));
        Assert.Equal(StackApiTests.Photograph, await stacks.PhotographAsync(first.Id));

        Assert.Null(await stacks.BeginAsync(Format.LP));

        await SettledAsync(stacks);
        Assert.True(await stacks.ConfirmAsync(first.Id, StackApiTests.KindOfBlue.DiscogsReleaseId));
        Assert.False(await stacks.ConfirmAsync(first.Id, StackApiTests.KindOfBlue.DiscogsReleaseId));
        Assert.True(await stacks.AbandonAsync(second.Id));
        Assert.True((await stacks.CurrentAsync())?.IsComplete);

        Assert.True(await stacks.DiscardAsync());
        Assert.False(await stacks.DiscardAsync());
        Assert.Null(await stacks.PhotographAsync(first.Id));
    }

    [Fact]
    public async Task The_stack_is_followed_as_it_changes_elsewhere()
    {
        await using var host = await ApiHost.StartAsync();
        var stacks = new StackClient(host.CreateClient());
        await using var watch = new StackWatch(stacks, NullLogger<StackWatch>.Instance);

        var changed = new SemaphoreSlim(0);
        watch.Changed += () => changed.Release();
        watch.Start();

        // The first event is the Stack as it is: none.
        Assert.True(await changed.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(watch.IsLoaded);
        Assert.Null(watch.Current);

        // Begun from somewhere else entirely.
        var begun = await new StackClient(host.CreateClient()).BeginAsync(Format.Vinyl);

        await EventuallyAsync(() => watch.Current?.Id == begun!.Id, changed);
    }

    private static async Task SettledAsync(StackClient stacks)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while ((await stacks.CurrentAsync())?.Scans.Any(scan => !scan.IsIdentified) is true && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(25);
        }
    }

    private static async Task EventuallyAsync(Func<bool> condition, SemaphoreSlim changed)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (!condition())
        {
            Assert.True(DateTimeOffset.UtcNow < deadline, "The watch never caught up with the change.");
            await changed.WaitAsync(TimeSpan.FromMilliseconds(250));
        }
    }
}
