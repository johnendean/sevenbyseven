using SevenBySeven.Modules.Scanning.Domain;
using Stack = SevenBySeven.Modules.Scanning.Domain.Stack;

namespace SevenBySeven.Tests.Scanning;

public class StackTests
{
    private static Scan AnyScan() =>
        Scan.FromJpeg(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 10, 10, CaptureSource.Camera);

    [Fact]
    public void A_new_stack_is_empty_and_takes_its_capacity()
    {
        var stack = new Stack(10);

        Assert.True(stack.IsEmpty);
        Assert.False(stack.IsFull);
        Assert.False(stack.IsComplete);
        Assert.Equal(10, stack.Capacity);
        Assert.Equal(10, stack.Remaining);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_stack_must_be_able_to_hold_something(int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Stack(capacity));

    [Fact]
    public void Scans_are_numbered_from_one_in_the_order_they_were_captured()
    {
        var stack = new Stack(3);

        var first = stack.Add(AnyScan());
        var second = stack.Add(AnyScan());

        Assert.Equal(1, first.Position);
        Assert.Equal(2, second.Position);
        Assert.Equal(2, stack.Count);
        Assert.Equal(1, stack.Remaining);
    }

    [Fact]
    public void Capture_stops_at_the_limit()
    {
        var stack = new Stack(2);
        stack.Add(AnyScan());
        stack.Add(AnyScan());

        Assert.True(stack.IsFull);
        Assert.Equal(0, stack.Remaining);
        Assert.Throws<InvalidOperationException>(() => stack.Add(AnyScan()));
    }

    [Fact]
    public void The_limit_is_fixed_when_the_stack_starts()
    {
        // Raising the configured ceiling mid-stack must not move the goalposts
        // underneath a stack already being captured.
        var stack = new Stack(1);
        stack.Add(AnyScan());

        Assert.True(stack.IsFull);
    }

    [Fact]
    public void A_stack_is_complete_only_once_every_scan_is_resolved()
    {
        var stack = new Stack(3);
        var first = stack.Add(AnyScan());
        var second = stack.Add(AnyScan());

        Assert.False(stack.IsComplete);
        Assert.Equal(2, stack.PendingCount);

        first.Confirm();
        Assert.False(stack.IsComplete);

        second.Abandon();
        Assert.True(stack.IsComplete);
        Assert.Equal(0, stack.PendingCount);
        Assert.Equal(1, stack.ConfirmedCount);
    }

    [Fact]
    public void An_empty_stack_is_never_complete()
    {
        // Nothing captured is not the same as everything dealt with, and treating it
        // as complete would offer to take you to a collection nothing was added to.
        var stack = new Stack(5);

        Assert.False(stack.IsComplete);
    }

    [Fact]
    public void Abandoning_what_is_left_spares_the_scans_already_confirmed()
    {
        var stack = new Stack(3);
        var confirmed = stack.Add(AnyScan());
        var pending = stack.Add(AnyScan());
        confirmed.Confirm();

        stack.AbandonRemaining();

        // Those are Copies now. Giving up on the stack does not un-add them.
        Assert.Equal(StackedScanStatus.Confirmed, confirmed.Status);
        Assert.Equal(StackedScanStatus.Abandoned, pending.Status);
        Assert.True(stack.IsComplete);
    }

    [Fact]
    public void Adding_nothing_is_refused() =>
        Assert.Throws<ArgumentNullException>(() => new Stack(3).Add(null!));
}
