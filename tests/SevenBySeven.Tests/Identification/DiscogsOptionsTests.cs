using SevenBySeven.Modules.Identification.Discogs;

namespace SevenBySeven.Tests.Identification;

public class DiscogsOptionsTests
{
    [Fact]
    public void The_default_user_agent_is_a_valid_http_header()
    {
        // A bare "+https://..." is not a legal product token and throws at startup,
        // taking the scan page down with it. The URL has to sit in a comment.
        using var request = new HttpRequestMessage();

        Assert.True(request.Headers.UserAgent.TryParseAdd(new DiscogsOptions().UserAgent));
    }

    [Fact]
    public void The_default_user_agent_identifies_the_application()
    {
        // Discogs rejects generic user agents outright.
        Assert.Contains("SevenBySeven", new DiscogsOptions().UserAgent, StringComparison.Ordinal);
    }

    [Fact]
    public void Options_are_unconfigured_without_a_token() =>
        Assert.False(new DiscogsOptions().IsConfigured);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_tokens_do_not_count_as_configured(string? token) =>
        Assert.False(new DiscogsOptions { PersonalAccessToken = token }.IsConfigured);

    [Fact]
    public void We_stay_under_the_discogs_ceiling_of_sixty_a_minute() =>
        Assert.True(new DiscogsOptions().RequestsPerMinute < 60);
}
