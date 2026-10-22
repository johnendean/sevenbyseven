extern alias web;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using SevenBySeven.Modules.Collection.Contracts;
using web::SevenBySeven.Web.Api;
using web::SevenBySeven.Web.Collection;

namespace SevenBySeven.Tests.Api;

/// <summary>
/// The web app's clients as it registers them, service defaults and all, with only the
/// network stood in for. A write that fails after it may have committed must not be sent
/// again: nothing on the far side would notice the duplicate.
/// </summary>
public class ApiClientResilienceTests
{
    [Fact]
    public async Task A_write_that_fails_is_sent_once()
    {
        var network = StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable);
        await using var services = Build(network);

        var collection = services.GetRequiredService<CollectionClient>();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            collection.AddAsync(new AddCopyRequest(249504, CopyDetails.Unknown)));

        Assert.Single(network.Requests);
    }

    [Fact]
    public async Task A_read_that_fails_is_tried_again()
    {
        var network = StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable);
        await using var services = Build(network);

        var collection = services.GetRequiredService<CollectionClient>();

        await Assert.ThrowsAsync<HttpRequestException>(() => collection.ListAsync());

        Assert.True(network.Requests.Count > 1, $"Expected the read to be retried, but it was sent {network.Requests.Count} time(s).");
    }

    private static ServiceProvider Build(StubHttpMessageHandler network)
    {
        var builder = Host.CreateApplicationBuilder();

        // Where service discovery would find the API under Aspire.
        builder.Configuration["services:api:http:0"] = "http://localhost:5130";

        builder.AddServiceDefaults();
        builder.Services.AddApiClients();
        builder.Services.AddHttpClient<CollectionClient>().ConfigurePrimaryHttpMessageHandler(() => network);

        // The retries are the point, not the backoff between them.
        builder.Services.ConfigureAll<HttpStandardResilienceOptions>(options =>
        {
            options.Retry.Delay = TimeSpan.Zero;
            options.Retry.UseJitter = false;
        });

        return builder.Services.BuildServiceProvider();
    }
}
