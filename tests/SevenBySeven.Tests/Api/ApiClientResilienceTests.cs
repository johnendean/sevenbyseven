extern alias web;

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly.Timeout;
using SevenBySeven.Modules.Identification.Contracts;
using SevenBySeven.Modules.Collection.Contracts;
using web::SevenBySeven.Web.Api;
using web::SevenBySeven.Web.Collection;
using web::SevenBySeven.Web.Identification;

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

    [Fact]
    public void Identification_is_given_longer_than_an_ordinary_request()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();
        builder.Services.AddApiClients();
        using var services = builder.Services.BuildServiceProvider();

        var options = services.GetRequiredService<IOptionsMonitor<HttpStandardResilienceOptions>>()
            .Get($"{nameof(IdentificationClient)}-standard");

        Assert.Equal(IdentificationClient.Timeout, options.AttemptTimeout.Timeout);
        Assert.True(options.TotalRequestTimeout.Timeout > IdentificationClient.Timeout);
    }

    [Fact]
    public async Task Those_are_the_options_identification_s_pipeline_reads()
    {
        // Proves the name, not the value: a short limit under it must cut a slow call off.
        // Under any other name, the ordinary ten seconds would let this one finish.
        var network = StubHttpMessageHandler.RespondingAsync(async (_, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(IdentificationResult.Nothing(), options: ApiHost.Json),
            };
        });

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["services:api:http:0"] = "http://localhost:5130";
        builder.AddServiceDefaults();
        builder.Services.AddApiClients();
        builder.Services.AddHttpClient<IdentificationClient>().ConfigurePrimaryHttpMessageHandler(() => network);
        builder.Services.PostConfigure<HttpStandardResilienceOptions>($"{nameof(IdentificationClient)}-standard", options =>
            options.AttemptTimeout.Timeout = TimeSpan.FromMilliseconds(200));
        await using var services = builder.Services.BuildServiceProvider();

        var identification = services.GetRequiredService<IdentificationClient>();

        await Assert.ThrowsAsync<TimeoutRejectedException>(() => identification.IdentifyAsync(new byte[] { 0xFF, 0xD8, 0xFF }));
    }

    [Fact]
    public async Task Identification_keeps_the_rule_that_a_write_is_sent_once()
    {
        var network = StubHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable);
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["services:api:http:0"] = "http://localhost:5130";
        builder.AddServiceDefaults();
        builder.Services.AddApiClients();
        builder.Services.AddHttpClient<IdentificationClient>().ConfigurePrimaryHttpMessageHandler(() => network);
        await using var services = builder.Services.BuildServiceProvider();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            services.GetRequiredService<IdentificationClient>().IdentifyAsync(new byte[] { 0xFF, 0xD8, 0xFF }));

        Assert.Single(network.Requests);
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
