using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SevenBySeven.Shared.Modularity;

/// <summary>
/// A vertical slice of the application. Each module owns its own domain types,
/// persistence configuration and UI, and is composed into the host at startup.
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
}
