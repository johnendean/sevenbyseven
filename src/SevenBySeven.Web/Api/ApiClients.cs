using SevenBySeven.Web.Catalogue;
using SevenBySeven.Web.Collection;
using SevenBySeven.Web.Gigs;

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

        return services;
    }
}
