using System.Net;
using System.Text.Json;

namespace SevenBySeven.Tests.Api;

public class ApiHostTests
{
    [Fact]
    public async Task Every_module_with_endpoints_is_in_the_published_document()
    {
        await using var host = await ApiHost.StartAsync();
        using var client = host.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToArray();

        Assert.Contains("/catalogue/releases/{discogsReleaseId}", paths);
        Assert.Contains("/collection", paths);
        Assert.Contains("/gigs/{gigId}", paths);
        Assert.Contains("/identifications", paths);
    }
}
