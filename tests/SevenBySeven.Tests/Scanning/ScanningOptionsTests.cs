extern alias web;

using SevenBySeven.Modules.Scanning;
using web::SevenBySeven.Web.Scanning;

namespace SevenBySeven.Tests.Scanning;

public class ScanningOptionsTests
{
    [Fact]
    public void A_stack_holds_ten_records_unless_told_otherwise() =>
        Assert.Equal(10, new ScanningOptions().MaxStackSize);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(10, 10)]
    [InlineData(25, 25)]
    public void A_sensible_limit_is_taken_as_given(int configured, int expected) =>
        Assert.Equal(expected, new ScanningOptions { MaxStackSize = configured }.MaxStackSize);

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_stack_that_could_hold_nothing_is_raised_to_one(int configured) =>
        Assert.Equal(1, new ScanningOptions { MaxStackSize = configured }.MaxStackSize);

    [Fact]
    public void An_extravagant_limit_is_clamped_rather_than_refused()
    {
        // Photographs are held in the API's memory until they are confirmed or abandoned,
        // so an unchecked value here is tens of megabytes. A stack that silently caps is
        // better than an API that will not start.
        Assert.Equal(
            ScanningOptions.HighestMaxStackSize,
            new ScanningOptions { MaxStackSize = 500 }.MaxStackSize);
    }

    [Fact]
    public void The_detector_has_thresholds_that_worked_against_real_records()
    {
        var detection = new DetectionOptions();

        Assert.True(detection.MinCoverage > detection.EmptyCoverage);
        Assert.True(detection.ClearFraction is > 0 and < 1);
        Assert.True(detection.StillFrames > 0);
        Assert.True(detection.SettleOutFrames > detection.StillFrames);
    }

    [Fact]
    public void A_stack_is_kept_for_an_afternoon_and_an_evening_unless_told_otherwise() =>
        Assert.Equal(TimeSpan.FromHours(4), new ScanningOptions().StackIdleTimeout);

    [Fact]
    public void An_idle_timeout_too_short_to_capture_a_pile_is_raised_to_a_minute() =>
        Assert.Equal(TimeSpan.FromMinutes(1), new ScanningOptions { StackIdleTimeout = TimeSpan.FromSeconds(5) }.StackIdleTimeout);

    [Fact]
    public void An_idle_timeout_past_a_day_is_brought_back_to_one() =>
        Assert.Equal(TimeSpan.FromDays(1), new ScanningOptions { StackIdleTimeout = TimeSpan.FromDays(30) }.StackIdleTimeout);
}
