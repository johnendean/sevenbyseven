using SevenBySeven.Modules.Scanning;
using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Tests.Scanning;

public class StackSessionTests
{
    private static Scan AnyScan() =>
        Scan.FromJpeg(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 10, 10, CaptureSource.Camera);

    [Fact]
    public void A_new_session_holds_no_stack()
    {
        var session = new StackSession();

        Assert.False(session.HasStack);
        Assert.Null(session.Current);
        Assert.False(session.HasUnresolvedWork);
    }

    [Fact]
    public void Beginning_a_stack_raises_Changed()
    {
        var session = new StackSession();
        var raised = 0;
        session.Changed += () => raised++;

        var stack = session.Begin(10);

        Assert.True(session.HasStack);
        Assert.Equal(10, stack.Capacity);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void A_stack_with_nothing_captured_is_not_work_worth_warning_about()
    {
        var session = new StackSession();
        session.Begin(10);

        Assert.False(session.HasUnresolvedWork);
    }

    [Fact]
    public void A_stack_with_scans_left_to_deal_with_is()
    {
        var session = new StackSession();
        var stack = session.Begin(10);
        stack.Add(AnyScan());

        Assert.True(session.HasUnresolvedWork);

        stack.Scans[0].Confirm();

        Assert.False(session.HasUnresolvedWork);
    }

    [Fact]
    public void Discarding_gives_up_on_what_was_left_and_lets_the_stack_go()
    {
        var session = new StackSession();
        var stack = session.Begin(10);
        var scan = stack.Add(AnyScan());

        session.Discard();

        Assert.Equal(StackedScanStatus.Abandoned, scan.Status);
        Assert.Null(session.Current);
        Assert.False(session.HasStack);
    }

    [Fact]
    public void Discarding_when_there_is_no_stack_raises_nothing()
    {
        var session = new StackSession();
        var raised = 0;
        session.Changed += () => raised++;

        session.Discard();

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Defaults_are_remembered_for_the_stack_and_cleared_with_it()
    {
        var session = new StackSession();
        session.Begin(10);

        session.SetDefaults(new StackDefaults { PurchasedFrom = "Reckless", Location = "Shelf A" });

        Assert.Equal("Reckless", session.Defaults.PurchasedFrom);
        Assert.Equal("Shelf A", session.Defaults.Location);

        session.Discard();

        Assert.Same(StackDefaults.None, session.Defaults);
    }

    [Fact]
    public void Starting_a_second_stack_forgets_the_first_ones_defaults()
    {
        var session = new StackSession();
        session.Begin(10);
        session.SetDefaults(new StackDefaults { Location = "Shelf A" });

        session.Begin(10);

        Assert.Null(session.Defaults.Location);
    }

    [Fact]
    public void Defaults_must_actually_be_something() =>
        Assert.Throws<ArgumentNullException>(() => new StackSession().SetDefaults(null!));

    [Fact]
    public void Touching_the_session_re_renders_what_is_watching()
    {
        // Identification finishing is not a user's action, so nothing else would.
        var session = new StackSession();
        var raised = 0;
        session.Changed += () => raised++;

        session.Touch();

        Assert.Equal(1, raised);
    }
}
