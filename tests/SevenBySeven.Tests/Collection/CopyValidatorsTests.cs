using SevenBySeven.Modules.Collection.Contracts;

namespace SevenBySeven.Tests.Collection;

public class CopyValidatorsTests
{
    private static readonly CopyDetailsValidator Details = new();

    [Fact]
    public void Details_with_nothing_said_are_fine()
    {
        Assert.True(Details.Validate(CopyDetails.Unknown).IsValid);
    }

    [Fact]
    public void Details_up_to_the_limits_are_fine()
    {
        var details = new CopyDetails
        {
            PricePaid = 1_000_000m,
            PurchasedFrom = new string('x', 200),
            Location = new string('x', 200),
            Notes = new string('x', 4000),
        };

        Assert.True(Details.Validate(details).IsValid);
        Assert.True(Details.Validate(new CopyDetails { PricePaid = 0m }).IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1_000_000.01)]
    public void A_price_out_of_range_is_refused(double price)
    {
        var result = Details.Validate(new CopyDetails { PricePaid = (decimal)price });

        Assert.Equal("A price cannot be negative, or more than a million.", Assert.Single(result.Errors).ErrorMessage);
    }

    [Fact]
    public void Text_past_the_limits_is_refused_field_by_field()
    {
        var details = new CopyDetails
        {
            PurchasedFrom = new string('x', 201),
            Location = new string('x', 201),
            Notes = new string('x', 4001),
        };

        Assert.Equal(
            [nameof(CopyDetails.Location), nameof(CopyDetails.Notes), nameof(CopyDetails.PurchasedFrom)],
            Details.Validate(details).Errors.Select(error => error.PropertyName).Order());
    }

    [Fact]
    public void A_confirmation_is_held_to_the_limits_of_its_details()
    {
        var request = new AddCopyRequest(249504, new CopyDetails { Location = new string('x', 201) });

        var result = new AddCopyRequestValidator().Validate(request);

        Assert.Equal("Details.Location", Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void A_confirmation_without_details_is_refused()
    {
        var result = new AddCopyRequestValidator().Validate(new AddCopyRequest(249504, null!));

        Assert.Equal(
            "A Confirmation needs the details of the Copy, even if every one of them is blank.",
            Assert.Single(result.Errors).ErrorMessage);
    }
}
