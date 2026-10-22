using Microsoft.Extensions.Http.Resilience;
using SevenBySeven.Web.Catalogue;
using SevenBySeven.Web.Collection;
using SevenBySeven.Web.Gigs;
using SevenBySeven.Web.Identification;
using SevenBySeven.Web.Scanning;

namespace SevenBySeven.Web.Api;

public static class ApiClients
{
    /// <summary>
    /// The typed clients the pages reach the API through, each over service discovery and
    /// the resilience every client gets from the service defaults.
    /// </summary>
    public static IServiceCollection AddApiClients(this IServiceCollection services)
    {
        services.AddHttpClient<CatalogueClient>(http => http.BaseAddress = ApiCalls.BaseAddress);
        services.AddHttpClient<CollectionClient>(http => http.BaseAddress = ApiCalls.BaseAddress);
        services.AddHttpClient<GigsClient>(http => http.BaseAddress = ApiCalls.BaseAddress);
        services.AddHttpClient<StackClient>(http => http.BaseAddress = ApiCalls.BaseAddress);

        // Identification waits on Claude and then Discogs, well past the ten seconds an
        // attempt is otherwise given. The default handler reads one set of options shared by
        // every client, so this one replaces it rather than configuring it, and keeps its rule
        // that a write is never replayed.
        //
        // RemoveAllResilienceHandlers is marked experimental. It is the documented way to
        // replace the default handler for one client, and the package version is pinned
        // centrally, so a change to it arrives as a deliberate upgrade.
#pragma warning disable EXTEXP0001
        services.AddHttpClient<IdentificationClient>(http => http.BaseAddress = ApiCalls.BaseAddress)
            .RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.DisableForUnsafeHttpMethods();
                options.AttemptTimeout.Timeout = IdentificationClient.Timeout;
                options.TotalRequestTimeout.Timeout = IdentificationClient.Timeout + TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = IdentificationClient.Timeout * 2;
            });

        return services;
    }
}
