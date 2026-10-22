extern alias web;

using SevenBySeven.Modules.Catalogue;
using SevenBySeven.Modules.Collection.Contracts;
using web::SevenBySeven.Web.Collection;

namespace SevenBySeven.Tests.Collection;

public class CopyFormTests
{
    [Fact]
    public void A_blank_form_offers_the_usual_currency()
    {
        // The field is only meaningful next to a price, but an empty box reads as a
        // missing answer rather than an unasked question.
        Assert.Equal("GBP", new CopyForm().Currency);
    }

    [Fact]
    public void A_form_carries_its_fields_through_to_the_details()
    {
        var details = new CopyForm
        {
            MediaCondition = ConditionGrade.NearMint,
            SleeveCondition = ConditionGrade.VeryGood,
            PricePaid = 18m,
            Currency = "USD",
            PurchasedFrom = "Amoeba",
            Location = "Crate by the door",
            Notes = "Promo copy.",
        }.ToDetails();

        Assert.Equal(ConditionGrade.NearMint, details.MediaCondition);
        Assert.Equal(ConditionGrade.VeryGood, details.SleeveCondition);
        Assert.Equal(18m, details.PricePaid);
        Assert.Equal("USD", details.PricePaidCurrency);
        Assert.Equal("Amoeba", details.PurchasedFrom);
        Assert.Equal("Crate by the door", details.Location);
        Assert.Equal("Promo copy.", details.Notes);
    }

    [Fact]
    public void Editing_a_copy_starts_from_what_is_already_known()
    {
        var copy = Sheet(new CopyDetails
        {
            MediaCondition = ConditionGrade.Good,
            PricePaid = 5m,
            PricePaidCurrency = "EUR",
            Location = "Loft",
        });

        var form = CopyForm.For(copy);

        Assert.Equal(ConditionGrade.Good, form.MediaCondition);
        Assert.Equal(5m, form.PricePaid);
        Assert.Equal("EUR", form.Currency);
        Assert.Equal("Loft", form.Location);
    }

    [Fact]
    public void A_copy_a_stack_added_opens_with_everything_still_to_say()
    {
        // This is the case the edit path exists for: a Stack adds without stopping to
        // ask, so condition and price arrive here blank or not at all.
        var copy = Sheet(new CopyDetails { PurchasedFrom = "Reckless" });

        var form = CopyForm.For(copy);

        Assert.Null(form.MediaCondition);
        Assert.Null(form.SleeveCondition);
        Assert.Null(form.PricePaid);
        Assert.Equal("Reckless", form.PurchasedFrom);
        Assert.Equal("GBP", form.Currency);
    }

    [Fact]
    public void There_is_no_copy_to_edit() =>
        Assert.Throws<ArgumentNullException>(() => CopyForm.For(null!));

    private static CopySheet Sheet(CopyDetails details) =>
        new(Guid.CreateVersion7(), DateTimeOffset.UtcNow, TestDatabase.AnyRelease().ToSheet(), details, PlayStanding.NeverPlayed);
}
