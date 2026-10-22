using Microsoft.Extensions.Options;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Scanning;
using SevenBySeven.Modules.Scanning.Contracts;

namespace SevenBySeven.Tests.Scanning;

public class StackHolderTests
{
    private static readonly MatchCandidate KindOfBlue =
        new(249504, "Kind Of Blue", "Miles Davis", "Columbia", "CS 8163", "US", 1959, "Vinyl, LP", null, 5460);

    private readonly Clock _clock = new();

    [Fact]
    public void There_is_no_stack_until_one_is_begun()
    {
        Assert.Null(Holder().Current);
    }

    [Fact]
    public void A_stack_takes_its_size_from_configuration_and_its_format_from_the_request()
    {
        var stack = Holder(maxStackSize: 3).Begin(Format.SevenInch);

        Assert.NotNull(stack);
        Assert.Equal(3, stack.Capacity);
        Assert.Equal(Format.SevenInch, stack.Format);
    }

    [Fact]
    public void A_stack_with_records_still_to_deal_with_is_not_replaced()
    {
        var holder = Holder();
        var first = holder.Begin(Format.Vinyl)!;
        holder.Add(AnyScan());

        Assert.Null(holder.Begin(Format.LP));
        Assert.Equal(first.Id, holder.Current!.Id);
    }

    [Fact]
    public void A_stack_with_nothing_left_to_say_is_replaced()
    {
        var holder = Holder();
        var first = holder.Begin(Format.Vinyl)!;
        holder.Abandon(holder.Add(AnyScan()).Scan!.Id);

        var second = holder.Begin(Format.LP);

        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void A_full_stack_says_so_rather_than_throwing()
    {
        var holder = Holder(maxStackSize: 1);
        holder.Begin(Format.Vinyl);
        holder.Add(AnyScan());

        var outcome = holder.Add(AnyScan());

        Assert.True(outcome.WasFull);
        Assert.Null(outcome.Scan);
    }

    [Fact]
    public void A_photograph_with_no_stack_to_go_into_says_so()
    {
        var outcome = Holder().Add(AnyScan());

        Assert.False(outcome.WasFull);
        Assert.Null(outcome.Scan);
    }

    [Fact]
    public void A_captured_scan_waits_its_turn_to_be_identified()
    {
        var holder = Holder();
        holder.Begin(Format.LP);

        var added = holder.Add(AnyScan()).Scan!;

        Assert.True(holder.Captured.TryRead(out var queued));
        Assert.Equal(added.Id, queued.Scan.Id);
        Assert.Equal(Format.LP, queued.Format);
        Assert.Equal(StackedScanStatus.Identifying, added.Status);
    }

    [Fact]
    public void A_sleeve_that_could_not_be_read_is_unreadable_rather_than_unmatched()
    {
        var holder = Holder();
        holder.Begin(Format.Vinyl);
        holder.Add(AnyScan());
        holder.Captured.TryRead(out var queued);

        holder.Identified(queued.Scan, IdentificationResult.Nothing() with { ReadFailure = "Overloaded." });

        var line = holder.Current!.Scans.Single();
        Assert.Equal(StackedScanStatus.Unreadable, line.Status);
        Assert.Equal("Overloaded.", line.Failure);
    }

    [Fact]
    public void A_confirmation_claims_the_scan_so_no_other_device_can()
    {
        var (holder, scanId) = Matched();

        var first = holder.Claim(scanId, KindOfBlue.DiscogsReleaseId);
        var second = holder.Claim(scanId, KindOfBlue.DiscogsReleaseId);

        Assert.NotNull(first.Defaults);
        Assert.True(second.IsOver);
    }

    [Fact]
    public void Only_a_scan_s_own_candidates_can_be_confirmed()
    {
        var (holder, scanId) = Matched();

        Assert.True(holder.Claim(scanId, 1).IsNotACandidate);
    }

    [Fact]
    public void A_confirmation_that_fails_leaves_the_scan_to_try_again()
    {
        var (holder, scanId) = Matched();
        holder.Claim(scanId, KindOfBlue.DiscogsReleaseId);

        holder.Unclaimed(scanId);

        Assert.NotNull(holder.Claim(scanId, KindOfBlue.DiscogsReleaseId).Defaults);
    }

    [Fact]
    public void A_confirmed_scan_is_over()
    {
        var (holder, scanId) = Matched();
        holder.Claim(scanId, KindOfBlue.DiscogsReleaseId);

        holder.Confirmed(scanId);

        Assert.Equal(StackedScanStatus.Confirmed, holder.Current!.Scans.Single().Status);
        Assert.True(holder.Claim(scanId, KindOfBlue.DiscogsReleaseId).IsOver);
        Assert.False(holder.Abandon(scanId));
    }

    [Fact]
    public void A_scan_the_stack_does_not_hold_cannot_be_claimed()
    {
        var (holder, _) = Matched();

        Assert.True(holder.Claim(Guid.CreateVersion7(), KindOfBlue.DiscogsReleaseId).IsMissing);
    }

    [Fact]
    public void Defaults_are_what_every_confirmation_adds_with()
    {
        var (holder, scanId) = Matched();
        var defaults = new StackDefaults { PurchasedFrom = "Reckless", Location = "Shelf A" };

        Assert.True(holder.SetDefaults(defaults));

        Assert.Equal(defaults, holder.Claim(scanId, KindOfBlue.DiscogsReleaseId).Defaults);
    }

    [Fact]
    public void Defaults_with_no_stack_to_say_them_of_go_nowhere()
    {
        Assert.False(Holder().SetDefaults(new StackDefaults { Location = "Shelf A" }));
    }

    [Fact]
    public void The_photograph_is_held_until_the_stack_lets_it_go()
    {
        var holder = Holder();
        holder.Begin(Format.Vinyl);
        var scan = AnyScan();
        var id = holder.Add(scan).Scan!.Id;

        Assert.Same(scan, holder.Photograph(id));

        holder.Discard();

        Assert.Null(holder.Photograph(id));
        Assert.False(holder.Discard());
    }

    [Fact]
    public void A_stack_left_untouched_past_its_timeout_is_abandoned()
    {
        var holder = Holder(idle: TimeSpan.FromHours(4));
        holder.Begin(Format.Vinyl);
        holder.Add(AnyScan());

        _clock.Advance(TimeSpan.FromHours(3));
        Assert.False(holder.AbandonIfIdle());

        _clock.Advance(TimeSpan.FromHours(1));
        Assert.True(holder.AbandonIfIdle());
        Assert.Null(holder.Current);
    }

    [Fact]
    public void Doing_something_to_a_stack_keeps_it()
    {
        var holder = Holder(idle: TimeSpan.FromHours(4));
        holder.Begin(Format.Vinyl);

        _clock.Advance(TimeSpan.FromHours(3));
        holder.SetDefaults(new StackDefaults { Location = "Shelf A" });
        _clock.Advance(TimeSpan.FromHours(3));

        Assert.False(holder.AbandonIfIdle());
    }

    [Fact]
    public void Identification_finishing_is_not_something_done_to_the_stack()
    {
        var holder = Holder(idle: TimeSpan.FromHours(4));
        holder.Begin(Format.Vinyl);
        holder.Add(AnyScan());
        holder.Captured.TryRead(out var queued);

        _clock.Advance(TimeSpan.FromHours(3));
        holder.Identified(queued.Scan, new IdentificationResult([KindOfBlue], new SleeveDetails(), IdentificationRoute.Barcode));
        _clock.Advance(TimeSpan.FromHours(1));

        Assert.True(holder.AbandonIfIdle());
    }

    [Fact]
    public async Task A_watcher_hears_the_current_version_and_then_each_change()
    {
        var holder = Holder();
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var watch = holder.WatchAsync(stop.Token).GetAsyncEnumerator(stop.Token);

        Assert.True(await watch.MoveNextAsync());
        var start = watch.Current;

        holder.Begin(Format.Vinyl);

        Assert.True(await watch.MoveNextAsync());
        Assert.True(watch.Current > start);
    }

    private StackHolder Holder(int maxStackSize = 10, TimeSpan? idle = null) =>
        new(
            Options.Create(new ScanningOptions
            {
                MaxStackSize = maxStackSize,
                StackIdleTimeout = idle ?? ScanningOptions.DefaultStackIdleTimeout,
            }),
            _clock);

    /// <summary>A Stack holding one Scan that matched Kind Of Blue.</summary>
    private (StackHolder Holder, Guid ScanId) Matched()
    {
        var holder = Holder();
        holder.Begin(Format.Vinyl);
        var id = holder.Add(AnyScan()).Scan!.Id;
        holder.Captured.TryRead(out var queued);
        holder.Identified(queued.Scan, new IdentificationResult([KindOfBlue], new SleeveDetails(), IdentificationRoute.Barcode));

        return (holder, id);
    }

    private static Scan AnyScan() =>
        Scan.FromJpeg(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 10, 10, CaptureSource.Camera);

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 10, 22, 14, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }
}
