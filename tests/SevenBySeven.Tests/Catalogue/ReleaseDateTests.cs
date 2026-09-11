using SevenBySeven.Modules.Catalogue.Domain;

namespace SevenBySeven.Tests.Catalogue;

public class ReleaseDateTests
{
    [Fact]
    public void A_year_only_date_renders_as_the_year() =>
        Assert.Equal("1959", ReleaseDate.ForYear(1959).ToString());

    [Theory]
    [InlineData(1959, 8, null, "1959-08")]
    [InlineData(1959, 8, 17, "1959-08-17")]
    [InlineData(1971, 11, 8, "1971-11-08")]
    public void Known_parts_render_zero_padded(int year, int? month, int? day, string expected) =>
        Assert.Equal(expected, new ReleaseDate(year, month, day).ToString());

    [Fact]
    public void A_day_without_a_month_is_not_rendered_as_a_full_date() =>
        Assert.Equal("1959", new ReleaseDate(1959, null, 17).ToString());
}
