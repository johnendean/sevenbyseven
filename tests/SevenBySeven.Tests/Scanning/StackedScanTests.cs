using SevenBySeven.Modules.Identification.Domain;
using SevenBySeven.Modules.Scanning.Domain;

namespace SevenBySeven.Tests.Scanning;

public class StackedScanTests
{
    private static Scan AnyScan() =>
        Scan.FromJpeg(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, 10, 10, CaptureSource.Camera);

    private static StackedScan Captured() => new(AnyScan(), 1);

    private static IdentificationResult WithCandidates() =>
        new(
            [new MatchCandidate(1, "Kind of Blue", "Miles Davis", "Columbia", "CS 8163", "US", 1959, "LP", null, null)],
            new SleeveDetails { CatalogueNumber = "CS 8163" },
            IdentificationRoute.CatalogueNumber);

    [Fact]
    public void A_captured_scan_starts_out_being_identified()
    {
        var scan = Captured();

        Assert.Equal(StackedScanStatus.Identifying, scan.Status);
        Assert.True(scan.IsPending);
        Assert.False(scan.IsResolved);
        Assert.False(scan.IsIdentified);
    }

    [Fact]
    public void Candidates_leave_it_waiting_on_a_confirmation()
    {
        var scan = Captured();

        scan.Identified(WithCandidates());

        Assert.Equal(StackedScanStatus.Matched, scan.Status);
        Assert.True(scan.IsIdentified);
        Assert.True(scan.IsPending);
        Assert.NotNull(scan.Result);
    }

    [Fact]
    public void Finding_nothing_is_a_no_match()
    {
        var scan = Captured();

        scan.Identified(IdentificationResult.Nothing());

        Assert.Equal(StackedScanStatus.NoMatch, scan.Status);
        Assert.True(scan.IsPending);
    }

    [Fact]
    public void A_call_that_could_not_run_is_kept_apart_from_a_no_match()
    {
        // Telling me a failed call was an unreadable photograph sends me back to the
        // shelf to re-photograph a blameless record.
        var scan = Captured();

        scan.Failed("No Anthropic API key is configured.");

        Assert.Equal(StackedScanStatus.Unreadable, scan.Status);
        Assert.Equal("No Anthropic API key is configured.", scan.Failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_failure_always_says_something(string reason)
    {
        var scan = Captured();

        scan.Failed(reason);

        Assert.False(string.IsNullOrWhiteSpace(scan.Failure));
    }

    [Fact]
    public void Confirming_ends_the_scan()
    {
        var scan = Captured();
        scan.Identified(WithCandidates());

        scan.Confirm();

        Assert.Equal(StackedScanStatus.Confirmed, scan.Status);
        Assert.True(scan.IsResolved);
        Assert.False(scan.IsPending);
    }

    [Fact]
    public void Abandoning_ends_the_scan()
    {
        var scan = Captured();

        scan.Abandon();

        Assert.Equal(StackedScanStatus.Abandoned, scan.Status);
        Assert.True(scan.IsResolved);
    }

    [Fact]
    public void Identification_landing_late_cannot_reopen_a_resolved_scan()
    {
        // Identification runs behind the capture loop, so a result can arrive after
        // the scan has already been given up on.
        var scan = Captured();
        scan.Abandon();

        scan.Identified(WithCandidates());
        scan.Failed("too late");

        Assert.Equal(StackedScanStatus.Abandoned, scan.Status);
        Assert.Null(scan.Result);
        Assert.Null(scan.Failure);
    }

    [Fact]
    public void A_scan_knows_where_it_came_in_the_stack() =>
        Assert.Equal(4, new StackedScan(AnyScan(), 4).Position);

    [Fact]
    public void A_scan_needs_a_photograph_and_a_place_in_the_stack()
    {
        Assert.Throws<ArgumentNullException>(() => new StackedScan(null!, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StackedScan(AnyScan(), 0));
    }
}
