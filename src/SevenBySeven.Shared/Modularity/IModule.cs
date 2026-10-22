using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SevenBySeven.Shared.Modularity;

/// <summary>
/// A vertical slice of the application. Each module owns its own domain types,
/// persistence configuration and endpoints, and is composed into the API at startup.
/// The host discovers implementations itself, so each must be public with a
/// parameterless constructor.
/// </summary>
public interface IModule
{
    string Name { get; }

    /// <summary>Registers the module's services with the host container.</summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Contributes the module's entity configuration to the shared model.
    /// A module configures only the tables in its own schema.
    /// </summary>
    void ConfigureModel(ModelBuilder modelBuilder);

    /// <summary>
    /// Maps the module's HTTP endpoints. They speak only the module's contracts, never its
    /// entities (docs/adr/0009).
    /// </summary>
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
