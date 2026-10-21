using SevenBySeven.Modules.Gigs.Contracts;

namespace SevenBySeven.Tests.Gigs;

public class GigDetailsValidatorTests
{
    private static readonly GigDetailsValidator Validator = new();
    private static readonly DateOnly September26 = new(2026, 9, 26);

    [Fact]
    public void A_gig_up_to_the_limits_is_fine()
    {
        var details = new GigDetails { PlayedOn = September26, Venue = new string('x', 200), Notes = new string('x', 4000) };

        Assert.True(Validator.Validate(details).IsValid);
    }

    [Fact]
    public void A_venue_past_the_limit_is_refused()
    {
        var result = Validator.Validate(new GigDetails { PlayedOn = September26, Venue = new string('x', 201) });

        Assert.Equal("A venue can be at most 200 characters.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public void Notes_past_the_limit_are_refused()
    {
        var result = Validator.Validate(new GigDetails { PlayedOn = September26, Notes = new string('x', 4001) });

        Assert.Equal("Notes can be at most 4,000 characters.", Assert.Single(result.Errors).ErrorMessage);
    }
}
